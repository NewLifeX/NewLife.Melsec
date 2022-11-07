using System.ComponentModel;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Melsec.Protocols;
using NewLife.Serialization;

namespace NewLife.Melsec.Drivers;

/// <summary>
/// 三菱PLC驱动
/// </summary>
[Driver("MelsecFxLinks")]
[DisplayName("三菱FxLinks")]
public class FxLinksDriver : DriverBase
{
    private FxLinks _link;

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
    public virtual String GetAddress(IPoint point)
    {
        if (point == null) throw new ArgumentException("点位信息不能为空！");

        // 去掉冒号后面的位域
        var addr = point.Address;
        var p = addr.IndexOf(':');
        if (p > 0) addr = addr.Substring(0, p);

        return addr;
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

            Driver = this,
            Device = device,
            Parameter = p,
        };

        // 实例化
        if (_link == null)
        {
            lock (this)
            {
                if (_link == null)
                {
                    var link = new FxLinks();
                    if (p.Timeout > 0) link.Timeout = p.Timeout;

                    // 外部已指定通道时，打开连接
                    if (device != null) link.Open();

                    _link = link;
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
            _link.TryDispose();
            _link = null;
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

        foreach (var point in points)
        {
            var name = point.Name;
            var addr = GetAddress(point);
            var length = point.Length;
            var data = _link.Read(addr, (UInt16)(length / 2));

            dic[name] = data;
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
        var addr = GetAddress(point);
        var res = value switch
        {
            Int32 v1 => _link.Write(addr, v1),
            String v2 => _link.Write(addr, v2),
            Boolean v3 => _link.Write(addr, v3),
            Byte[] v4 => _link.Write(addr, v4),
            Byte v5 => _link.Write(addr, v5),
            _ => throw new ArgumentException("暂不支持写入该类型数据！"),
        };
        return res;
    }
}