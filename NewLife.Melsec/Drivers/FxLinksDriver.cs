using System.ComponentModel;
using System.Diagnostics;
using System.Xml;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;
using NewLife.Melsec.Protocols;
using NewLife.Reflection;
using NewLife.Serialization;
using NewLife.Xml;

namespace NewLife.Melsec.Drivers;

/// <summary>
/// 三菱PLC驱动
/// </summary>
[Driver("MelsecFxLinks")]
[DisplayName("三菱FxLinks")]
public class FxLinksDriver : DriverBase
{
    /// <summary>链接</summary>
    public FxLinks Link { get; set; }

    /// <summary>
    /// 打开通道数量
    /// </summary>
    private Int32 _nodes;

    /// <summary>
    /// 创建驱动参数对象，可序列化成Xml/Json作为该协议的参数模板
    /// </summary>
    /// <returns></returns>
    public override IDriverParameter CreateParameter(String parameter)
    {
        if (parameter.IsNullOrEmpty()) return new FxLinksParameter
        {
            PortName = "COM1",
            Baudrate = 9600,
            Host = 1,
        };

        return parameter.ToXmlEntity<FxLinksParameter>();
    }

    /// <summary>
    /// 从点位中解析地址
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual UInt16 GetAddress(IPoint point)
    {
        if (point == null) throw new ArgumentException("点位信息不能为空！");

        // 去掉冒号后面的位域
        var addr = point.Address;
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr[..p];

        return (UInt16)addr.ToInt();
    }

    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="device">通道</param>
    /// <param name="parameter">参数</param>
    /// <returns></returns>
    public override INode Open(IDevice device, IDriverParameter parameter)
    {
        using var span = Tracer?.NewSpan("fxlinks:parameter", parameter.ToJson());

        var p = parameter as FxLinksParameter;
        if (p == null) throw new ArgumentException($"参数不合法：{parameter.ToJson()}");

        if (p.Baudrate <= 0) p.Baudrate = 9600;

        var node = new MelsecNode
        {
            Address = p.PortName,
            Host = p.Host,

            Driver = this,
            Device = device,
            Parameter = p,
        };

        // 实例化
        if (Link == null)
        {
            lock (this)
            {
                if (Link == null)
                {
                    var link = new FxLinks
                    {
                        PortName = p.PortName,
                        Baudrate = p.Baudrate,
                        DataBits = p.DataBits,
                        Parity = p.Parity,
                        StopBits = p.StopBits,

                        Log = Log,
                        Tracer = Tracer,
                    };

                    if (p.Timeout > 0) link.Timeout = p.Timeout;

                    //if (Log != null && Log.Level <= LogLevel.Debug) link.Log = Log;

                    // 外部已指定通道时，打开连接
                    if (device != null) link.Open();

                    Link = link;
                }
            }
        }

        Interlocked.Increment(ref _nodes);

        return node;
    }

    /// <summary>
    /// 关闭设备驱动
    /// </summary>
    /// <param name="node"></param>
    public override void Close(INode node)
    {
        if (Interlocked.Decrement(ref _nodes) <= 0)
        {
            Link.TryDispose();
            Link = null;
        }
    }

    /// <summary>
    /// 读取数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="points">点位集合，Address属性地址示例：D100、C100、W100、H100</param>
    /// <returns></returns>
    public override IDictionary<String, Object> Read(INode node, IPoint[] points)
    {
        var dic = new Dictionary<String, Object>();

        if (points == null || points.Length == 0) return dic;

        var n = node as MelsecNode;
        var p = node.Parameter as FxLinksParameter;

        // 组合多个片段，减少读取次数
        var list = BuildSegments(points, p);

        // 加锁，避免冲突
        lock (Link)
        {
            // 分段整体读取
            for (var i = 0; i < list.Count; i++)
            {
                var seg = list[i];

                // 其中一项读取报错时，直接跳过，不要影响其它批次
                try
                {
                    // 根据点位地址类型，使用不同操作，尽管字读取也可以用来读取位存储区，但很容易混淆，并且不好解析，因此不支持
                    if (seg.Code.EqualIgnoreCase("X", "Y", "M"))
                        seg.Bits = Link.ReadBit(n.Host, seg.Code + seg.Address, (Byte)seg.Count);
                    else if (seg.Code.EqualIgnoreCase("D"))
                        seg.Values = Link.ReadWord(n.Host, seg.Code + seg.Address, (Byte)seg.Count);
                    else
                        throw new NotSupportedException($"{seg.Code}{seg.Address} is unkown address");
                }
                catch (Exception ex)
                {
                    Log?.Error(ex.ToString());
                }

                // 读取时延迟一点时间
                if (i < list.Count - 1 && p.BatchDelay > 0) Thread.Sleep(p.BatchDelay);
            }
        }

        // 分割数据
        return Dispatch(points, list);
    }

    internal IList<Segment> BuildSegments(IList<IPoint> points, FxLinksParameter p)
    {
        // 组合多个片段，减少读取次数
        var list = new List<Segment>();
        foreach (var point in points)
        {
            var cmd = point.Address[..1];
            var addr = point.Address[1..].ToInt();

            list.Add(new Segment
            {
                Code = cmd,
                Address = addr,
                Count = 1
            });
        }
        list = list.OrderBy(e => e.Code).ThenBy(e => e.Address).ThenByDescending(e => e.Count).ToList();

        var step = p.BatchStep > 1 ? p.BatchStep : 1;
        var k = 1;
        var rs = new List<Segment>();
        var prv = list[0];
        rs.Add(prv);
        for (var i = 1; i < list.Count; i++)
        {
            var cur = list[i];

            // 前一段末尾碰到了当前段开始，可以合并
            var flag = prv.Address + prv.Count + step > cur.Address;
            //// 如果是读取位存储区，间隔小于8都可以合并
            //if (!flag && cur.Code.EqualIgnoreCase("X", "Y", "M"))
            //{
            //    flag = prv.Address + prv.Count + 8 > cur.Address;
            //}

            // 前一段末尾碰到了当前段开始，可以合并
            if (flag && prv.Code == cur.Code)
            {
                if (p.BatchSize <= 0 || k < p.BatchSize)
                {
                    // 要注意，可能前后重叠，也可能前面区域比后面还大
                    var size = cur.Address + cur.Count - prv.Address;
                    if (size > prv.Count) prv.Count = size;

                    // 连续合并数累加
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

    internal IDictionary<String, Object> Dispatch(IPoint[] points, IList<Segment> segments)
    {
        var dic = new Dictionary<String, Object>();
        if (segments == null || segments.Count == 0) return dic;

        foreach (var point in points)
        {
            var cmd = point.Address[..1];
            var addr = point.Address[1..].ToInt();

            // 找到片段
            var seg = segments.FirstOrDefault(e => e.Code == cmd && e.Address <= addr && addr < e.Address + e.Count);
            if (seg != null)
            {
                var code = seg.Code;
                if (seg.Values != null)
                {
                    // 校验数据完整性
                    var offset = addr - seg.Address;
                    if (seg.Values.Length > offset)
                        dic[point.Name] = seg.Values[offset];
                }
                else if (seg.Bits != null)
                {
                    // 校验数据完整性
                    var offset = addr - seg.Address;
                    if (seg.Bits.Length > offset)
                        dic[point.Name] = seg.Bits[offset];
                }
                //else
                //    throw new NotSupportedException($"无法拆分{code}");
            }
        }
        return dic;
    }

    [DebuggerDisplay("{Code}({Address}, {Count})")]
    internal class Segment
    {
        public String Code { get; set; }
        public Int32 Address { get; set; }
        public Int32 Count { get; set; }
        public Byte[] Bits { get; set; }
        public UInt16[] Values { get; set; }
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="point">点位，Address属性地址示例：D100、C100、W100、H100</param>
    /// <param name="value">数据</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public override Object Write(INode node, IPoint point, Object value)
    {
        if (value == null) return null;
        if (point.Address.IsNullOrEmpty()) return null;

        var n = node as MelsecNode;

        UInt16[] vs;
        if (value is Byte[] buf)
        {
            vs = new UInt16[(Int32)Math.Ceiling(buf.Length / 2d)];
            for (var i = 0; i < vs.Length; i++)
            {
                vs[i] = buf.ToUInt16(i * 2, false);
            }
        }
        else
        {
            if (point.Address.StartsWithIgnoreCase("X", "Y", "M"))
                vs = ConvertToBit(value, point, n.Device?.Specification);
            else
                vs = ConvertToWord(value, point, n.Device?.Specification);

            if (vs == null) throw new NotSupportedException($"点位[{point.Name}]不支持数据[{value}]");
        }

        // 加锁，避免冲突
        lock (Link)
        {
            // 按照点位地址前缀决定使用哪一种写入方法，要求物模型必须配置对点位
            if (point.Address.StartsWithIgnoreCase("X", "Y", "M"))
                return Link.WriteBit(n.Host, point.Address, vs);
            else if (point.Address.StartsWithIgnoreCase("D"))
                return Link.WriteWord(n.Host, point.Address, vs);
            else
                return Link.Write(point.Address[..1], n.Host, point.Address, vs);
        }
    }

    /// <summary>原始数据转为线圈</summary>
    /// <param name="data"></param>
    /// <param name="point"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    protected virtual UInt16[] ConvertToBit(Object data, IPoint point, ThingSpec spec)
    {
        var type = TypeHelper.GetNetType(point);
        if (type == null)
        {
            // 找到物属性定义
            var pi = spec?.Properties?.FirstOrDefault(e => e.Id.EqualIgnoreCase(point.Name));
            type = TypeHelper.GetNetType(pi?.DataType?.Type);
        }
        if (type == null) return null;

        DefaultSpan.Current?.AppendTag("ConvertToBit->" + type.FullName);

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.SByte:
                return data.ToBoolean() ? new[] { (UInt16)0x01 } : new[] { (UInt16)0x00 };
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
                return data.ToInt() > 0 ? new[] { (UInt16)0x01 } : new[] { (UInt16)0x00 };
            case TypeCode.Int64:
            case TypeCode.UInt64:
                return data.ToLong() > 0 ? new[] { (UInt16)0x01 } : new[] { (UInt16)0x00 };
            default:
                return data.ToBoolean() ? new[] { (UInt16)0x01 } : new[] { (UInt16)0x00 };
        }
    }

    /// <summary>原始数据转寄存器数组</summary>
    /// <param name="data"></param>
    /// <param name="point"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    protected virtual UInt16[] ConvertToWord(Object data, IPoint point, ThingSpec spec)
    {
        var type = TypeHelper.GetNetType(point);
        if (type == null)
        {
            // 找到物属性定义
            var pi = spec?.Properties?.FirstOrDefault(e => e.Id.EqualIgnoreCase(point.Name));
            type = TypeHelper.GetNetType(pi?.DataType?.Type);
        }
        if (type == null) return null;

        DefaultSpan.Current?.AppendTag("ConvertToWord->" + type.FullName);

        switch (type.GetTypeCode())
        {
            case TypeCode.Boolean:
            case TypeCode.Byte:
            case TypeCode.SByte:
                return data.ToBoolean() ? new[] { (UInt16)0x01 } : new[] { (UInt16)0x00 };
            case TypeCode.Int16:
            case TypeCode.UInt16:
                return new[] { (UInt16)data.ToInt() };
            case TypeCode.Int32:
            case TypeCode.UInt32:
                {
                    var n = data.ToInt();
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case TypeCode.Int64:
            case TypeCode.UInt64:
                {
                    var n = data.ToLong();
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case TypeCode.Single:
                {
                    var d = (Single)data.ToDouble();
                    //var n = BitConverter.SingleToInt32Bits(d);
                    var n = (UInt32)d;
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case TypeCode.Double:
                {
                    var d = (Double)data.ToDouble();
                    //var n = BitConverter.DoubleToInt64Bits(d);
                    var n = (UInt64)d;
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case TypeCode.Decimal:
                {
                    var d = data.ToDecimal();
                    var n = (UInt64)d;
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            //case TypeCode.String:
            //    break;
            default:
                return null;
        }
    }
}