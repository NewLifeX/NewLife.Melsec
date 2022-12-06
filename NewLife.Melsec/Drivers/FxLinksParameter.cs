using System.ComponentModel;
using System.IO.Ports;
using NewLife.IoT.Drivers;

namespace NewLife.Melsec.Drivers;

/// <summary>三菱FxLinks参数</summary>
public class FxLinksParameter : IDriverParameter
{
    /// <summary>串口名称</summary>
    [Description("串口名称")]
    public String PortName { get; set; }

    /// <summary>波特率。默认9600</summary>
    [Description("波特率")]
    public Int32 Baudrate { get; set; } = 9600;

    /// <summary>数据位长度。默认7</summary>
    [Description("波特率")]
    public Int32 DataBits { get; set; } = 7;

    /// <summary>奇偶校验位。默认Even偶校验</summary>
    [Description("波特率")]
    public Parity Parity { get; set; } = Parity.Even;

    /// <summary>停止位。默认One</summary>
    [Description("波特率")]
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>主机/站号</summary>
    [Description("主机/站号")]
    public Byte Host { get; set; }

    /// <summary>网络超时。发起请求后等待响应的超时时间，默认3000ms</summary>
    [Description("网络超时。发起请求后等待响应的超时时间，默认3000ms")]
    public Int32 Timeout { get; set; } = 3000;
}