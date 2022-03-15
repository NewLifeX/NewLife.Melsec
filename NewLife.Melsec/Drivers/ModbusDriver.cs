using NewLife.IoT.Protocols;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;

namespace NewLife.IoT.Drivers;

/// <summary>
/// Modbus协议封装
/// </summary>
public abstract class ModbusDriver : DisposeBase
{
    /// <summary>
    /// Modbus通道
    /// </summary>
    protected Modbus _modbus;

    private Int32 _nodes;

    #region 构造
    /// <summary>
    /// 销毁时，关闭连接
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _modbus.TryDispose();
        _modbus = null;
    }
    #endregion

    #region 方法
    /// <summary>
    /// 创建Modbus通道
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="parameters"></param>
    /// <returns></returns>
    protected abstract Modbus CreateModbus(IChannel channel, IDictionary<String, Object> parameters);

    /// <summary>
    /// 打开通道。一个ModbusTcp设备可能分为多个通道读取，需要共用Tcp连接，以不同节点区分
    /// </summary>
    /// <param name="channel">通道</param>
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    public virtual INode Open(IChannel channel, IDictionary<String, Object> parameters)
    {
        var node = new ModbusNode
        {
            Host = (Byte)parameters["host"],
            ReadCode = (FunctionCodes)parameters["ReadFunctionCode"],
            WriteCode = (FunctionCodes)parameters["WriteFunctionCode"],
            Channel = channel
        };

        // 实例化一次Tcp连接
        if (_modbus == null)
        {
            lock (this)
            {
                if (_modbus == null)
                {
                    var modbus = CreateModbus(channel, parameters);

                    modbus.Open();

                    _modbus = modbus;
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
    public virtual void Close(INode node)
    {
        if (Interlocked.Decrement(ref _nodes) <= 0)
        {
            _modbus.TryDispose();
            _modbus = null;
        }
    }

    /// <summary>
    /// 读取数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="points">点位集合</param>
    /// <returns></returns>
    public virtual IDictionary<String, Object> Read(INode node, IPoint[] points)
    {
        if (points == null || points.Length == 0) return null;

        // 加锁，避免冲突
        lock (_modbus)
        {
            var n = node as ModbusNode;
            var dic = new Dictionary<String, Object>();
            //foreach (var p in points)
            //{
            //    var addr = GetAddress(p);
            //    var count = GetCount(p);
            //    if (addr != UInt16.MaxValue)
            //    {
            //        var buf = _modbus.Read(n.ReadCode, n.Host, addr, (UInt16)count);

            //        // 这里可以点位信息解析数据，如果直接返回字节数组，设备通道将使用表达式解析

            //        dic[p.Name] = buf;
            //    }
            //}

            // 组合多个片段，减少读取次数
            var list = new List<Segment>();
            foreach (var p in points)
            {
                var addr = GetAddress(p);
                var count = GetCount(p);
                if (addr != UInt16.MaxValue)
                    list.Add(new Segment { Address = addr, Count = count });
            }
            list = list.OrderBy(e => e.Address).ThenByDescending(e => e.Count).ToList();

            // 逆向合并，减少拷贝
            for (var i = list.Count - 1; i > 0; i--)
            {
                var prv = list[i - 1];
                var cur = list[i];

                // 前一段末尾碰到了当前段开始，可以合并
                if (prv.Address + prv.Count >= cur.Address)
                {
                    // 要注意，可能前后重叠，也可能前面区域比后面还大
                    var size = cur.Address + cur.Count - prv.Address;
                    if (size > prv.Count) prv.Count = size;

                    list.RemoveAt(i);
                }
            }

            // 整体读取
            foreach (var seg in list)
            {
                seg.Data = _modbus.Read(n.ReadCode, n.Host, (UInt16)seg.Address, (UInt16)seg.Count);
            }

            // 分赃
            foreach (var p in points)
            {
                var addr = GetAddress(p);
                var count = GetCount(p);
                if (addr != UInt16.MaxValue)
                {
                    // 找到片段
                    var seg = list.FirstOrDefault(e => e.Address <= addr && addr + count <= e.Address + e.Count);
                    if (seg != null && seg.Data != null)
                    {
                        // 校验数据完整性
                        var offset = addr - seg.Address;
                        var size = count * 2;
                        if (seg.Data.Length >= offset + size)
                            dic[p.Name] = seg.Data.ReadBytes(offset, size);
                    }
                }
            }

            return dic;
        }
    }

    private class Segment
    {
        public Int32 Address { get; set; }
        public Int32 Count { get; set; }
        public Byte[] Data { get; set; }
    }

    /// <summary>
    /// 从点位中解析地址
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual UInt16 GetAddress(IPoint point)
    {
        if (point == null) return UInt16.MaxValue;

        // 去掉冒号后面的位域
        var addr = point.Address;
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr[..p];

        // 按十六进制解析返回
        if (addr.StartsWithIgnoreCase("0x")) return addr[2..].ToHex().ToUInt16(0, false);

        // 直接转数字范围
        return (UInt16)addr.ToInt(UInt16.MaxValue);
    }

    /// <summary>
    /// 从点位中计算寄存器个数
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public virtual Int32 GetCount(IPoint point)
    {
        // 字节数转寄存器数，要除以2
        var count = point.Length / 2;
        if (count > 0) return count;

        if (point.Type.IsNullOrEmpty()) return 1;

        return point.Type.ToLower() switch
        {
            "byte" or "sbyte" or "bool" => 1,
            "short" or "ushort" or "int16" or "uint16" => 2 / 2,
            "int" or "uint" or "int32" or "uint32" => 4 / 2,
            "long" or "ulong" or "int64" or "uint64" => 8 / 2,
            "float" or "single" => 4 / 2,
            "double" or "decimal" => 8 / 2,
            _ => 1,
        };
    }

    /// <summary>
    /// 写入数据
    /// </summary>
    /// <param name="node">节点对象，可存储站号等信息，仅驱动自己识别</param>
    /// <param name="point">点位</param>
    /// <param name="value">数值</param>
    public virtual Object Write(INode node, IPoint point, Object value)
    {
        var addr = GetAddress(point);
        if (addr == UInt16.MaxValue) return null;

        if (value == null) return null;

        var n = node as ModbusNode;
        UInt16[] vs;
        if (value is Byte[] buf)
        {
            vs = new UInt16[(Int32)(Math.Ceiling(buf.Length / 2d))];
            for (var i = 0; i < vs.Length; i++)
            {
                vs[i] = buf.ToUInt16(i * 2, false);
            }
        }
        else
        {
            vs = ConvertToRegister(value, point, n.Channel?.Specification);

            if (vs == null) throw new NotSupportedException($"点位[{point.Name}]不支持数据[{value}]");
        }

        // 加锁，避免冲突
        lock (_modbus)
        {
            return _modbus.Write(n.WriteCode, n.Host, addr, vs);
        }
    }

    /// <summary>原始数据转寄存器数组</summary>
    /// <param name="data"></param>
    /// <param name="point"></param>
    /// <param name="spec"></param>
    /// <returns></returns>
    private UInt16[] ConvertToRegister(Object data, IPoint point, ThingSpec spec)
    {
        // 找到物属性定义
        var pi = spec?.Properties?.FirstOrDefault(e => e.Id.EqualIgnoreCase(point.Name));
        var type = pi?.DataType?.Type;
        if (type.IsNullOrEmpty()) type = point.Type;
        if (type.IsNullOrEmpty()) return null;

        switch (type.ToLower())
        {
            case "short":
            case "int16":
            case "ushort":
            case "uint16":
                {
                    var n = data.ToInt();
                    return new[] { (UInt16)n };
                }
            case "int":
            case "int32":
            case "uint":
            case "uint32":
                {
                    var n = data.ToInt();
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "long":
            case "int64":
            case "ulong":
            case "uint64":
                {
                    var n = data.ToLong();
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "float":
            case "single":
                {
                    var d = (Single)data.ToDouble();
                    //var n = BitConverter.SingleToInt32Bits(d);
                    var n = (UInt32)d;
                    return new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "double":
            case "decimal":
                {
                    var d = data.ToDouble();
                    //var n = BitConverter.DoubleToInt64Bits(d);
                    var n = (UInt64)d;
                    return new[] { (UInt16)(n >> 48), (UInt16)(n >> 32), (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) };
                }
            case "bool":
            case "boolean":
                {
                    return data.ToBoolean() ? new[] { (UInt16)0xFF00 } : new[] { (UInt16)0x00 };
                }
            default:
                return null;
        }
        //return type.ToLower() switch
        //{
        //    "short" or "int16" or "ushort" or "uint16" => new[] { (UInt16)n },
        //    "int" or "int32" or "uint" or "uint32" => new[] { (UInt16)(n >> 16), (UInt16)(n & 0xFFFF) },
        //    "long" or "int64" or "uint64" => { },
        //    "float" or "single" => BitConverter.SingleToInt32Bits((Single)data.ToDouble()).GetBytes(false),
        //    "double" or "decimal" => BitConverter.DoubleToInt64Bits(data.ToDouble()).GetBytes(false),
        //    "bool" or "boolean" => data.ToBoolean() ? new[] { (UInt16)0xFF00 } : new[] { (UInt16)0x00 },
        //    _ => null,
        //};
    }

    /// <summary>
    /// 控制设备，特殊功能使用
    /// </summary>
    /// <param name="node"></param>
    /// <param name="parameters"></param>
    /// <exception cref="NotImplementedException"></exception>
    public virtual void Control(INode node, IDictionary<String, Object> parameters) => throw new NotImplementedException();
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }
    #endregion
}