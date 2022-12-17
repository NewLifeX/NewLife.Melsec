namespace NewLife.Melsec.Protocols;

/// <summary>控制码</summary>
public enum ControlCodes : Byte
{
    /// <summary>询问。发出请求</summary>
    ENQ = 05,

    /// <summary>文本起点。读取操作的响应开始</summary>
    STX = 02,

    /// <summary>文本终点。读取操作的响应结束</summary>
    ETX = 03,

    /// <summary>传送结束</summary>
    EOT = 04,

    /// <summary>确认。写入操作的响应</summary>
    ACK = 06,

    /// <summary>不确认</summary>
    NAK = 0x15,
}