namespace NewLife.Melsec.Protocols;

/// <summary>SLMP 协议软元件代码（与 MC 3E 帧兼容）</summary>
/// <remarks>
/// SLMP（Seamless Message Protocol）的软元件代码体系与 MC 协议 3E 帧完全兼容。
/// 此枚举提供显式的 SLMP 命名，便于代码表达意图。
/// 实际值与 <see cref="DeviceCode"/> 相同，可通过 <see cref="SLMPDeviceCodeHelper"/> 转换。
/// </remarks>
public enum SLMPDeviceCode : Byte
{
    /// <summary>辅助继电器（位）</summary>
    M = 0x90,

    /// <summary>输入继电器（位，编号十六进制）</summary>
    X = 0x9C,

    /// <summary>输出继电器（位，编号十六进制）</summary>
    Y = 0x9D,

    /// <summary>数据寄存器（字）</summary>
    D = 0xA8,

    /// <summary>链接寄存器（字，编号十六进制）</summary>
    W = 0xB4,

    /// <summary>文件寄存器（字）</summary>
    R = 0xAF,

    /// <summary>扩展文件寄存器（字）</summary>
    ZR = 0xB0,

    /// <summary>链接继电器（位，编号十六进制）</summary>
    B = 0xA0,

    /// <summary>特殊继电器（位）</summary>
    SM = 0x91,

    /// <summary>特殊寄存器（字）</summary>
    SD = 0xA9,

    /// <summary>锁存继电器（位）</summary>
    L = 0x92,

    /// <summary>定时器线圈（位）</summary>
    TC = 0xC1,

    /// <summary>定时器当前值（字）</summary>
    TS = 0xC0,

    /// <summary>计数器线圈（位）</summary>
    CC = 0xC5,

    /// <summary>计数器当前值（字）</summary>
    CS = 0xC4,
}

/// <summary>SLMPDeviceCode 工具方法</summary>
public static class SLMPDeviceCodeHelper
{
    /// <summary>判断是否为位软元件</summary>
    public static Boolean IsBitDevice(SLMPDeviceCode code) =>
        DeviceCodeHelper.IsBitDevice((DeviceCode)code);

    /// <summary>转换为 MC 3E DeviceCode（实际值相同，仅为类型转换）</summary>
    public static DeviceCode ToMC3E(SLMPDeviceCode code) => (DeviceCode)code;

    /// <summary>从 MC 3E DeviceCode 转换</summary>
    public static SLMPDeviceCode FromMC3E(DeviceCode code) => (SLMPDeviceCode)code;
}
