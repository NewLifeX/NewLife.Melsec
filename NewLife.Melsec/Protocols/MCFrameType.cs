namespace NewLife.Melsec.Protocols;

/// <summary>MC 协议帧类型</summary>
/// <remarks>
/// 三菱 MC 协议支持多种帧格式：
/// <see cref="Frame3E"/>（Qna 兼容 3E 帧，Q/L/iQ-R/FX5U 系列）、
/// <see cref="Frame1E"/>（A 兼容 1E 帧，A 系列旧式 PLC）和
/// <see cref="Frame4E"/>（扩展 4E 帧，iQ-R 系列，含序列号字段）。
/// </remarks>
public enum MCFrameType
{
    /// <summary>3E 帧（Qna 兼容）。含网络号/PC 号/IO 单元号等字段，子头 5000h</summary>
    Frame3E = 0,

    /// <summary>1E 帧（A 兼容）。不含网络号等字段，结构更简单，子头为单字节命令</summary>
    Frame1E = 1,

    /// <summary>4E 帧（iQ-R 系列扩展）。子头 5400h/D400h，在子头后增加 4 字节序列号字段</summary>
    /// <remarks>
    /// 4E 帧在标准 3E 帧的子头之后插入了 4 字节的序列号字段（2 字节序列号 + 2 字节保留），
    /// 用于请求-应答的严格匹配校验。后续字段布局与 3E 帧完全一致。
    /// 请求子头：0x5400（二进制）/ "5400"（ASCII）
    /// 响应子头：0xD400（二进制）/ "D400"（ASCII）
    /// </remarks>
    Frame4E = 2,
}

/// <summary>MCFrameType 扩展方法</summary>
internal static class MCFrameTypeHelper
{
    /// <summary>判断是否为 1E 帧</summary>
    public static Boolean Is1E(this MCFrameType frameType) => frameType == MCFrameType.Frame1E;

    /// <summary>判断是否为 4E 帧</summary>
    public static Boolean Is4E(this MCFrameType frameType) => frameType == MCFrameType.Frame4E;

    /// <summary>判断是否为 3E 或 4E 帧（共享相同后续字段布局）</summary>
    public static Boolean Is3EOr4E(this MCFrameType frameType) => frameType == MCFrameType.Frame3E || frameType == MCFrameType.Frame4E;
}
