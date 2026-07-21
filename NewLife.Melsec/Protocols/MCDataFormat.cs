namespace NewLife.Melsec.Protocols;

/// <summary>MC协议数据格式</summary>
/// <remarks>
/// 三菱 MC 协议支持两种数据编码格式：
/// <see cref="Binary"/>（二进制模式）和 <see cref="Ascii"/>（ASCII 十六进制模式）。
/// 
/// ASCII 模式通常用于 Q 系列部分旧设备及需要文本调试的场景。
/// </remarks>
public enum MCDataFormat
{
    /// <summary>二进制模式（默认）。Little-Endian 字节序，帧紧凑，效率高</summary>
    Binary = 0,

    /// <summary>ASCII 十六进制模式。Big-Endian 十六进制字符串表示，兼容性更好</summary>
    Ascii = 1,
}

/// <summary>MCDataFormat 扩展方法</summary>
internal static class MCDataFormatHelper
{
    /// <summary>判断是否为 ASCII 模式</summary>
    public static Boolean IsAscii(this MCDataFormat format) => format == MCDataFormat.Ascii;
}
