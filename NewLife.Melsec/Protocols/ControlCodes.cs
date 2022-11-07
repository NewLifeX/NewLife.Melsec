namespace NewLife.Melsec.Protocols;

/// <summary>控制码</summary>
public enum ControlCodes : Byte
{
    /// <summary>询问</summary>
    ENQ = 05,

    /// <summary>文本起点</summary>
    STX = 02,

    /// <summary>文本终点</summary>
    ETX = 03,

    /// <summary>传送结束</summary>
    EOT = 04,

    /// <summary>确认</summary>
    ACK = 06,

    /// <summary>不确认</summary>
    NAK = 0x15,
}