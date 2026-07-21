using System.ComponentModel;
using System.Diagnostics;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Melsec.Protocols;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Melsec.Drivers;

/// <summary>三菱PLC MC协议驱动（以太网 3E 帧，二进制模式）</summary>
/// <remarks>
/// 通过 TCP 长连接与三菱 Q 系列、iQ-R 系列及带网口的 FX3U/FX5U PLC 通信。
/// 实现 IDriver 接口，可无缝接入 NewLife.IoT 平台与 ZeroIoT/IoTEdge 网关。
/// </remarks>
[Driver("MelsecMC")]
[DisplayName("三菱MC以太网")]
public class MCDriver : DriverBase
{
    #region 属性

    /// <summary>MC协议链路</summary>
    public MCProtocol Link { get; set; }

    private Int32 _nodes;

    #endregion

    #region IDriver 接口

    /// <summary>创建驱动参数对象</summary>
    protected override IDriverParameter OnCreateParameter() => new MCParameter
    {
        Address = "192.168.1.10:6000",
        Timeout = 5000,
    };

    /// <summary>打开通道。一个物理 PLC 可以挂载多个逻辑设备（节点），共享同一 TCP 连接</summary>
    /// <param name="device">逻辑设备</param>
    /// <param name="parameter">参数</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>节点对象</returns>
    public override Task<INode> OpenAsync(IDevice device, IDriverParameter? parameter, CancellationToken cancellationToken = default)
    {
        using var span = Tracer?.NewSpan("mc:Open", parameter.ToJson());

        var p = parameter as MCParameter;
        if (p == null) throw new ArgumentException($"参数不合法：{parameter.ToJson()}");

        var node = new MelsecNode
        {
            Address = p.Address,
            Host = p.NetworkNo,

            Driver = this,
            Device = device,
            Parameter = p,
        };

        if (Link == null)
        {
            lock (this)
            {
                if (Link == null)
                {
                    var link = new MCProtocol
                    {
                        Address = p.Address,
                        FrameType = p.FrameType,
                        NetworkNo = p.NetworkNo,
                        DataFormat = p.DataFormat,
                        Log = Log,
                        Tracer = Tracer,
                    };

                    if (p.Timeout > 0) link.Timeout = p.Timeout;

                    if (device != null) link.Open();

                    Link = link;
                }
            }
        }

        Interlocked.Increment(ref _nodes);

        return TaskEx.FromResult(node as INode);
    }

    /// <summary>关闭通道</summary>
    /// <param name="node">节点对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    public override Task CloseAsync(INode node, CancellationToken cancellationToken = default)
    {
        if (Interlocked.Decrement(ref _nodes) <= 0)
        {
            Link.TryDispose();
            Link = null;
        }
        return TaskEx.CompletedTask;
    }

    /// <summary>读取数据</summary>
    /// <param name="node">节点对象</param>
    /// <param name="points">点位集合。地址格式示例：D100、M200、X1F、Y2A、B100</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>读取结果</returns>
    public override Task<ReadResult> ReadAsync(INode node, IPoint[] points, CancellationToken cancellationToken = default)
    {
        if (points == null || points.Length == 0)
            return TaskEx.FromResult(new ReadResult { IsSuccess = true, Points = [], Values = [] });

        var n = node as MelsecNode;
        var p = node.Parameter as MCParameter;

        var list = BuildSegments(points, p);

        lock (Link)
        {
            for (var i = 0; i < list.Count; i++)
            {
                var seg = list[i];
                try
                {
                    if (DeviceCodeHelper.IsBitDevice(seg.Code))
                        seg.Bits = Link.ReadBits(seg.Code, seg.StartAddress, seg.Count);
                    else
                        seg.Words = Link.ReadWords(seg.Code, seg.StartAddress, seg.Count);
                }
                catch (Exception ex)
                {
                    Log?.Error(ex.ToString());
                }

                if (i < list.Count - 1 && p.BatchDelay > 0) Thread.Sleep(p.BatchDelay);
            }
        }

        var dic = Dispatch(points, list);

        var resultPoints = new IPoint[dic.Count];
        var resultValues = new Object?[dic.Count];
        var idx = 0;
        foreach (var kv in dic)
        {
            var pt = points.FirstOrDefault(e => e.Name == kv.Key);
            resultPoints[idx] = pt;
            resultValues[idx] = kv.Value;
            idx++;
        }

        return TaskEx.FromResult(new ReadResult
        {
            IsSuccess = true,
            Points = resultPoints,
            Values = resultValues,
        });
    }

    /// <summary>写入数据</summary>
    /// <param name="node">节点对象</param>
    /// <param name="requests">写入请求数组，每项含目标点位和值</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>批量写入结果</returns>
    public override Task<WriteResult> WriteAsync(INode node, WriteRequest[] requests, CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        foreach (var request in requests)
        {
            var point = request.Point;
            var value = request.Value;

            if (value == null || point == null || point.Address.IsNullOrEmpty()) continue;

            var n = node as MelsecNode;
            var (devCode, addr) = DeviceCodeHelper.ParseAddress(point.Address);

            lock (Link)
            {
                if (DeviceCodeHelper.IsBitDevice(devCode))
                {
                    var bitVal = value.ToBoolean();
                    Link.WriteBits(devCode, addr, [bitVal]);
                }
                else
                {
                    var words = ConvertToWords(value, point, n.Device?.Specification);
                    if (words == null) throw new NotSupportedException($"点位[{point.Name}]不支持数据[{value}]");
                    Link.WriteWords(devCode, addr, words);
                }
            }

            successCount++;
        }

        return TaskEx.FromResult(WriteResult.SuccessBatch(successCount));
    }

    #endregion

    #region 批量读取优化

    /// <summary>构建批量读取分段（按软元件代码分组 + 地址排序 + 区间合并）</summary>
    /// <param name="points">点位集合</param>
    /// <param name="p">驱动参数</param>
    internal IList<MCSegment> BuildSegments(IList<IPoint> points, MCParameter p)
    {
        var list = new List<MCSegment>(points.Count);
        foreach (var point in points)
        {
            var (code, address) = DeviceCodeHelper.ParseAddress(point.Address);
            list.Add(new MCSegment { Code = code, StartAddress = address, Count = 1 });
        }

        // 按软元件代码 + 地址排序
        list = [.. list.OrderBy(e => e.Code).ThenBy(e => e.StartAddress)];

        var step = p.BatchStep > 1 ? p.BatchStep : 1;
        var k = 1;
        var rs = new List<MCSegment>();
        var prv = list[0];
        rs.Add(prv);

        for (var i = 1; i < list.Count; i++)
        {
            var cur = list[i];
            var canMerge = prv.Code == cur.Code &&
                           prv.StartAddress + prv.Count + step > cur.StartAddress;

            if (canMerge)
            {
                if (p.BatchSize <= 0 || k < p.BatchSize)
                {
                    var newSize = cur.StartAddress + cur.Count - prv.StartAddress;
                    if (newSize > prv.Count) prv.Count = newSize;
                    k++;
                }
                else
                {
                    rs.Add(cur);
                    prv = cur;
                    k = 1;
                }
            }
            else
            {
                rs.Add(cur);
                prv = cur;
                k = 1;
            }
        }

        return rs;
    }

    /// <summary>将汇聚批量读取结果分发到各点位</summary>
    /// <param name="points">点位集合</param>
    /// <param name="segments">分段结果</param>
    internal IDictionary<String, Object> Dispatch(IPoint[] points, IList<MCSegment> segments)
    {
        var dic = new Dictionary<String, Object>();
        if (segments == null || segments.Count == 0) return dic;

        foreach (var point in points)
        {
            var (code, address) = DeviceCodeHelper.ParseAddress(point.Address);
            var seg = segments.FirstOrDefault(e =>
                e.Code == code &&
                e.StartAddress <= address && address < e.StartAddress + e.Count);

            if (seg == null) continue;

            var offset = address - seg.StartAddress;
            if (seg.Words != null && seg.Words.Length > offset)
                dic[point.Name] = seg.Words[offset];
            else if (seg.Bits != null && seg.Bits.Length > offset)
                dic[point.Name] = seg.Bits[offset];
        }

        return dic;
    }

    #endregion

    #region 数据转换

    /// <summary>将点位值转换为字软元件数组（Little-Endian）</summary>
    protected virtual UInt16[] ConvertToWords(Object data, IPoint point, ThingSpec spec)
    {
        var type = TypeHelper.GetNetType(point);
        if (type == null)
        {
            var pi = spec?.Properties?.FirstOrDefault(e => e.Id.EqualIgnoreCase(point.Name));
            type = TypeHelper.GetNetType(pi?.DataType?.Type);
        }
        if (type == null) return null;

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
                return data.ToBoolean() ? [(UInt16)1] : [(UInt16)0];
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
                return [(UInt16)(data.ToInt() & 0xFFFF)];
            case TypeCode.Int32:
            case TypeCode.UInt32:
            {
                var v = (UInt32)data.ToInt();
                return [(UInt16)(v & 0xFFFF), (UInt16)(v >> 16)];
            }
            case TypeCode.Int64:
            case TypeCode.UInt64:
            {
                var v = (UInt64)data.ToLong();
                return [(UInt16)(v & 0xFFFF), (UInt16)((v >> 16) & 0xFFFF),
                        (UInt16)((v >> 32) & 0xFFFF), (UInt16)(v >> 48)];
            }
            case TypeCode.Single:
            {
                var bytes = BitConverter.GetBytes((Single)Convert.ChangeType(data, typeof(Single)));
                return [(UInt16)(bytes[0] | (bytes[1] << 8)), (UInt16)(bytes[2] | (bytes[3] << 8))];
            }
            case TypeCode.Double:
            {
                var bytes = BitConverter.GetBytes((Double)Convert.ChangeType(data, typeof(Double)));
                return [
                    (UInt16)(bytes[0] | (bytes[1] << 8)), (UInt16)(bytes[2] | (bytes[3] << 8)),
                    (UInt16)(bytes[4] | (bytes[5] << 8)), (UInt16)(bytes[6] | (bytes[7] << 8))];
            }
            default:
                return [(UInt16)(data.ToInt() & 0xFFFF)];
        }
    }

    #endregion

    #region 内部类型

    /// <summary>MC批量读取分段</summary>
    [DebuggerDisplay("{Code}(start={StartAddress}, count={Count})")]
    public class MCSegment
    {
        /// <summary>软元件代码</summary>
        public DeviceCode Code { get; set; }

        /// <summary>起始地址</summary>
        public Int32 StartAddress { get; set; }

        /// <summary>点数</summary>
        public Int32 Count { get; set; }

        /// <summary>字读取结果</summary>
        public UInt16[] Words { get; set; }

        /// <summary>位读取结果</summary>
        public Boolean[] Bits { get; set; }
    }

    #endregion
}
