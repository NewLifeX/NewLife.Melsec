using System.ComponentModel;
using NewLife.IoT.Drivers;
using NewLife.Melsec.Protocols;

namespace NewLife.Melsec.Drivers;

/// <summary>三菱MC协议驱动参数（以太网 3E 帧，支持二进制/ASCII模式）</summary>
public class MCParameter : IDriverParameter, IDriverParameterKey
{
    /// <summary>PLC地址。格式：IP:端口，如 192.168.1.10:6000</summary>
    [Description("PLC地址。格式：IP:端口，如 192.168.1.10:6000")]
    public String Address { get; set; }

    /// <summary>网络号。通常 0x00 表示本机网络</summary>
    [Description("网络号。通常 0")]
    public Byte NetworkNo { get; set; } = 0x00;

    /// <summary>数据格式。Binary=二进制模式（默认），Ascii=ASCII十六进制模式</summary>
    [Description("数据格式。Binary=二进制模式（默认），Ascii=ASCII十六进制模式")]
    public MCDataFormat DataFormat { get; set; } = MCDataFormat.Binary;

    /// <summary>网络超时。发起请求后等待响应的超时时间，默认 5000ms</summary>
    [Description("网络超时。发起请求后等待响应的超时时间，默认 5000ms")]
    public Int32 Timeout { get; set; } = 5000;

    /// <summary>批间隔。两个点位地址差小于等于该值时合并为一批，默认 1</summary>
    [Description("批间隔。两个点位地址差小于等于该值时合并为一批，默认 1")]
    public Int32 BatchStep { get; set; } = 1;

    /// <summary>批大小。每批最多包含的点位个数，0=不限制</summary>
    [Description("批大小。每批最多包含的点位个数，0=不限制")]
    public Int32 BatchSize { get; set; }

    /// <summary>批延迟。相邻批次请求之间的延迟时间，单位毫秒</summary>
    [Description("批延迟。相邻批次请求之间的延迟时间，单位毫秒")]
    public Int32 BatchDelay { get; set; }

    /// <summary>获取驱动参数的唯一标识（以 Address 区分不同 PLC）</summary>
    public String GetKey() => Address;
}
