namespace NewLife.Melsec.Protocols;

/// <summary>MC 协议 1E 帧软元件代码（A 系列兼容）</summary>
/// <remarks>
/// 1E 帧使用与 3E 帧不同的软元件代码体系。
/// 3E 帧代码见 <see cref="DeviceCode"/> 枚举。
/// </remarks>
public enum MC1EDeviceCode : Byte
{
    /// <summary>辅助继电器（位）</summary>
    M = 0x10,

    /// <summary>输入继电器（位，编号十六进制）</summary>
    X = 0x1C,

    /// <summary>输出继电器（位，编号十六进制）</summary>
    Y = 0x1D,

    /// <summary>锁存继电器（位）</summary>
    L = 0x12,

    /// <summary>报警继电器（位）</summary>
    F = 0x13,

    /// <summary>边沿继电器（位）</summary>
    V = 0x14,

    /// <summary>数据寄存器（字）</summary>
    D = 0x40,

    /// <summary>链接寄存器（字，编号十六进制）</summary>
    W = 0x44,

    /// <summary>文件寄存器（字）</summary>
    R = 0x48,

    /// <summary>定时器线圈（位）</summary>
    TC = 0x31,

    /// <summary>定时器当前值（字）</summary>
    TS = 0x30,

    /// <summary>计数器线圈（位）</summary>
    CC = 0x35,

    /// <summary>计数器当前值（字）</summary>
    CS = 0x34,

    /// <summary>特殊继电器（位）</summary>
    SM = 0x91,

    /// <summary>特殊寄存器（字）</summary>
    SD = 0xA9,
}

/// <summary>MC1EDeviceCode 工具方法</summary>
public static class MC1EDeviceCodeHelper
{
    /// <summary>判断是否为位软元件（Bit Device）</summary>
    public static Boolean IsBitDevice(MC1EDeviceCode code) => code switch
    {
        MC1EDeviceCode.M or MC1EDeviceCode.X or MC1EDeviceCode.Y or
        MC1EDeviceCode.L or MC1EDeviceCode.F or MC1EDeviceCode.V or
        MC1EDeviceCode.TC or MC1EDeviceCode.CC or MC1EDeviceCode.SM => true,
        _ => false,
    };

    /// <summary>从 3E 帧 DeviceCode 映射到 1E 帧 MC1EDeviceCode</summary>
    /// <param name="code">3E 帧软元件代码</param>
    /// <returns>1E 帧软元件代码</returns>
    public static MC1EDeviceCode From3E(DeviceCode code) => code switch
    {
        DeviceCode.M => MC1EDeviceCode.M,
        DeviceCode.X => MC1EDeviceCode.X,
        DeviceCode.Y => MC1EDeviceCode.Y,
        DeviceCode.L => MC1EDeviceCode.L,
        DeviceCode.F => MC1EDeviceCode.F,
        DeviceCode.V => MC1EDeviceCode.V,
        DeviceCode.D => MC1EDeviceCode.D,
        DeviceCode.W => MC1EDeviceCode.W,
        DeviceCode.R => MC1EDeviceCode.R,
        DeviceCode.TC => MC1EDeviceCode.TC,
        DeviceCode.TS => MC1EDeviceCode.TS,
        DeviceCode.CC => MC1EDeviceCode.CC,
        DeviceCode.CS => MC1EDeviceCode.CS,
        DeviceCode.SM => MC1EDeviceCode.SM,
        DeviceCode.SD => MC1EDeviceCode.SD,
        _ => throw new NotSupportedException($"不支持的软元件代码 0x{(Byte)code:X2}，仅 A 系列兼容子集可用"),
    };

    /// <summary>从 1E 帧 MC1EDeviceCode 映射回 3E 帧 DeviceCode</summary>
    public static DeviceCode To3E(MC1EDeviceCode code) => code switch
    {
        MC1EDeviceCode.M => DeviceCode.M,
        MC1EDeviceCode.X => DeviceCode.X,
        MC1EDeviceCode.Y => DeviceCode.Y,
        MC1EDeviceCode.L => DeviceCode.L,
        MC1EDeviceCode.F => DeviceCode.F,
        MC1EDeviceCode.V => DeviceCode.V,
        MC1EDeviceCode.D => DeviceCode.D,
        MC1EDeviceCode.W => DeviceCode.W,
        MC1EDeviceCode.R => DeviceCode.R,
        MC1EDeviceCode.TC => DeviceCode.TC,
        MC1EDeviceCode.TS => DeviceCode.TS,
        MC1EDeviceCode.CC => DeviceCode.CC,
        MC1EDeviceCode.CS => DeviceCode.CS,
        MC1EDeviceCode.SM => DeviceCode.SM,
        MC1EDeviceCode.SD => DeviceCode.SD,
        _ => throw new NotSupportedException($"不支持的 1E 软元件代码 0x{(Byte)code:X2}"),
    };
}
