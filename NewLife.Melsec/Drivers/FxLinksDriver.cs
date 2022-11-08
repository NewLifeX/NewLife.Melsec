using System.ComponentModel;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Log;
using NewLife.Melsec.Protocols;
using NewLife.Reflection;
using NewLife.Serialization;

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
    public override IDriverParameter GetDefaultParameter() => new FxLinksParameter
    {
        PortName = "COM1",
        Baudrate = 9600,
        Host = 1,
    };

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
    /// <param name="parameters">参数</param>
    /// <returns></returns>
    public override INode Open(IDevice device, IDictionary<String, Object> parameters)
    {
        var p = JsonHelper.Convert<FxLinksParameter>(parameters);
        if (p == null) throw new ArgumentException($"参数不合法：{parameters.ToJson()}");

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

        foreach (var point in points)
        {
            var name = point.Name;
            var type = point.GetNetType();
            if (type == typeof(Boolean) || type == typeof(Byte))
                dic[name] = Link.ReadBit(n.Host, point.Address, 1)?.ReadBytes();
            else
                dic[name] = Link.ReadWord(n.Host, point.Address, 1)?.ReadBytes();
        }

        return dic;
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
            vs = ConvertToRegister(value, point, n.Device?.Specification);

            if (vs == null) throw new NotSupportedException($"点位[{point.Name}]不支持数据[{value}]");
        }

        // 加锁，避免冲突
        lock (Link)
        {
            var type = point.GetNetType();
            if (type == typeof(Boolean) || type == typeof(Byte))
                return Link.WriteBit(n.Host, point.Address, vs);
            else
                return Link.WriteWord(n.Host, point.Address, vs);
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
        //if (type.IsNullOrEmpty()) return null;

        var type2 = TypeHelper.GetNetType(point);
        //var value = data.ChangeType(type2);
        switch (type2.GetTypeCode())
        {
            case TypeCode.Boolean:
                return data.ToBoolean() ? new[] { (UInt16)0xFF00 } : new[] { (UInt16)0x00 };
            //case TypeCode.Byte:
            //    break;
            //case TypeCode.Char:
            //    break;
            //case TypeCode.DateTime:
            //    break;
            //case TypeCode.DBNull:
            //    break;
            //case TypeCode.Empty:
            //    break;
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
            //case TypeCode.Object:
            //    break;
            //case TypeCode.SByte:
            //    break;
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
                    var d = (Decimal)data.ToDecimal();
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