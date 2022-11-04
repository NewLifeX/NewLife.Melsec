using System.ComponentModel;
using System.IO.Ports;
using HslCommunication.Core;
using HslCommunication.Profinet.Melsec;
using NewLife.IoT;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Log;
using NewLife.Serialization;

namespace NewLife.Melsec.Drivers;

/// <summary>
/// 三菱PLC驱动
/// </summary>
[Driver("MelsecPLC")]
[DisplayName("三菱PLC")]
public class MelsecDriver : DriverBase
{
    private IReadWriteNet _plcNet;

    /// <summary>
    /// 打开通道数量
    /// </summary>
    private Int32 _nodes;

    /// <summary>
    /// 创建驱动参数对象，可序列化成Xml/Json作为该协议的参数模板
    /// </summary>
    /// <returns></returns>
    public override IDriverParameter GetDefaultParameter() => new MelsecParameter
    {
        Address = "127.0.0.1:6000",
        DataFormat = "CDAB",
        Protocol = Protocol.MCQna3E
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
        var pm = JsonHelper.Convert<MelsecParameter>(parameters);

        if (pm == null) throw new ArgumentException($"参数不合法：{parameters.ToJson()}");

        MelsecNode node;
        String ipAddress;
        Int32 port;

        if (pm.Protocol == Protocol.MCQna3E)
        {
            var address = pm.Address;
            if (address.IsNullOrEmpty()) throw new ArgumentException("参数中未指定地址address");

            var p = address.IndexOf(':');
            if (p < 0) throw new ArgumentException($"参数中地址address格式错误:{address}");



            node = new MelsecNode
            {
                Address = address,

                Driver = this,
                Device = device,
                Parameter = pm,
            };
        }
        else
        {
            if (pm.PortName.IsNullOrEmpty()) throw new ArgumentException("参数中未指定串口名称PortName");

            node = new MelsecNode
            {
                Address = pm.PortName,

                Driver = this,
                Device = device,
                Parameter = pm,
            };
        }



        if (_plcNet == null)
        {
            lock (this)
            {
                if (_plcNet == null)
                {
                    if (pm.Protocol == Protocol.MCQna3E)
                    {
                        var address = pm.Address;
                        var p = address.IndexOf(':');

                        ipAddress = address.Substring(0, p);
                        port = address.Substring(p + 1).ToInt();

                        //MelsecA3CNet
                        var plcNet = new MelsecMcNet
                        {
                            ConnectTimeOut = 3000,
                            IpAddress = ipAddress,
                            Port = port,
                        };

                        _plcNet = plcNet;

                        if (!pm.DataFormat.IsNullOrEmpty() && Enum.TryParse<DataFormat>(pm.DataFormat, out var format))
                        {
                            plcNet.ByteTransform.DataFormat = format;
                        }

                        var connect = plcNet.ConnectServer();

                        if (!connect.IsSuccess) throw new Exception($"连接失败：{connect.Message}");
                    }
                    else
                    {
                        var melsecSerial = new MelsecFxLinks();
                        _plcNet = melsecSerial;

                        var baudRate = 9600;
                        var dataBits = 7;
                        var stopBits = 1;
                        var parity = 2;
                        byte station = 0;

                        melsecSerial.SerialPortInni(sp =>
                        {
                            sp.PortName = pm.PortName;
                            sp.BaudRate = pm.Baudrate;
                            sp.DataBits = 7;// dataBits;
                            sp.StopBits = StopBits.One;
                            sp.Parity = Parity.Even;
                        });
                        melsecSerial.Station = station;
                        melsecSerial.WaittingTime = 0;
                        melsecSerial.SumCheck = true;
                        melsecSerial.Format = 1;

                        var connect = melsecSerial.Open();
                        if (!connect.IsSuccess) throw new Exception($"连接失败：{connect.Message}");
                    }

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
            if (_plcNet is MelsecMcNet plcNet) plcNet?.ConnectClose();
            _plcNet.TryDispose();
            _plcNet = null;
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
            var data = _plcNet.Read(addr, (UInt16)(length / 2));

            if (!data.IsSuccess)
            {
                var r = _plcNet.ReadUInt16(addr);
                XTrace.WriteLine($"读取数据：{addr}={r.ToJson()}");
            }
            if (!data.IsSuccess) throw new Exception($"读取数据失败：{data.ToJson()}");



            dic[name] = data.Content;
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
            Int32 v1 => _plcNet.Write(addr, v1),
            String v2 => _plcNet.Write(addr, v2),
            Boolean v3 => _plcNet.Write(addr, v3),
            Byte[] v4 => _plcNet.Write(addr, v4),
            Byte v5 => _plcNet.Write(addr, v5),
            _ => throw new ArgumentException("暂不支持写入该类型数据！"),
        };
        return res;
    }
}