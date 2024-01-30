using System.ComponentModel;
using System.IO.Ports;
using NewLife.IoT.Drivers;

namespace NewLife.Melsec.Drivers;

/// <summary>三菱FxLinks参数</summary>
public class FxLinksParameter : IDriverParameter, IDriverParameterKey
{
    /// <summary>串口名称</summary>
    [Description("串口名称")]
    public String PortName { get; set; }

    /// <summary>波特率。默认9600</summary>
    [Description("波特率")]
    public Int32 Baudrate { get; set; } = 9600;

    /// <summary>数据位。默认7</summary>
    [Description("数据位")]
    public Int32 DataBits { get; set; } = 7;

    /// <summary>奇偶校验位。默认Even偶校验</summary>
    [Description("奇偶校验位")]
    public Parity Parity { get; set; } = Parity.Even;

    /// <summary>停止位。默认One</summary>
    [Description("停止位")]
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>主机/站号</summary>
    [Description("主机/站号")]
    public Byte Host { get; set; }

    /// <summary>网络超时。发起请求后等待响应的超时时间，默认3000ms</summary>
    [Description("网络超时。发起请求后等待响应的超时时间，默认3000ms")]
    public Int32 Timeout { get; set; } = 3000;

    /// <summary>批间隔。两个点位地址小于等于该值时凑为一批，默认1</summary>
    [Description("批间隔。两个点位地址小于等于该值时凑为一批，默认1")]
    public Int32 BatchStep { get; set; } = 1;

    /// <summary>批大小。凑批请求时，每批最多点位个数</summary>
    [Description("批大小。凑批请求时，每批最多点位个数")]
    public Int32 BatchSize { get; set; }

    /// <summary>批延迟。相邻请求之间的延迟时间，单位毫秒</summary>
    [Description("批延迟。相邻请求之间的延迟时间，单位毫秒")]
    public Int32 BatchDelay { get; set; }

    /// <summary>获取驱动参数的唯一标识</summary>
    /// <returns></returns>
    public String GetKey() => PortName;
}