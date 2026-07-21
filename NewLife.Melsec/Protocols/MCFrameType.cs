namespace NewLife.Melsec.Protocols;

/// <summary>MC 协议帧类型</summary>
/// <remarks>
/// 三菱 MC 协议支持多种帧格式：
/// <see cref="Frame3E"/>（Qna 兼容 3E 帧，Q/L/iQ-R/FX5U 系列）和
/// <see cref="Frame1E"/>（A 兼容 1E 帧，A 系列旧式 PLC）。
/// </remarks>
public enum MCFrameType
{
    /// <summary>3E 帧（Qna 兼容）。含网络号/PC 号/IO 单元号等字段，子头 5000h</summary>
    Frame3E = 0,

    /// <summary>1E 帧（A 兼容）。不含网络号等字段，结构更简单，子头为单字节命令</summary>
    Frame1E = 1,
}

/// <summary>MCFrameType 扩展方法</summary>
internal static class MCFrameTypeHelper
{
    /// <summary>判断是否为 1E 帧</summary>
    public static Boolean Is1E(this MCFrameType frameType) => frameType == MCFrameType.Frame1E;
}
