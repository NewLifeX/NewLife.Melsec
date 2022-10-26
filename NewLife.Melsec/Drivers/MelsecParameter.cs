using System.ComponentModel;
using NewLife.IoT.Drivers;

namespace NewLife.Melsec.Drivers;

/// <summary>Melsec参数</summary>
public class MelsecParameter : IDriverParameter
{
    /// <summary>地址。例如 127.0.0.1:6000</summary>
    [Description("地址。例如 127.0.0.1:6000")]
    public String Address { get; set; }

    /// <summary>数据格式。ABCD/BADC/CDAB/DCBA</summary>
    [Description("数据格式。ABCD/BADC/CDAB/DCBA")]
    public String DataFormat { get; set; }

    /// <summary>通信协议。MCQna3E/FxLinks485</summary>
    [Description("通信协议。MCQna3E/FxLinks485")]
    public Protocol Protocol { get; set; } = Protocol.MCQna3E;

    /// <summary>串口名称</summary>
    [Description("串口名称")]
    public String PortName { get; set; }

    /// <summary>波特率</summary>
    [Description("波特率")]
    public Int32 Baudrate { get; set; } = 9600;
}

public enum Protocol
{
    /// <summary>
    /// MC协议Qna-3E模式
    /// </summary>
    MCQna3E,

    /// <summary>
    /// FX系列计算机链
    /// </summary>
    FxLinks485
}