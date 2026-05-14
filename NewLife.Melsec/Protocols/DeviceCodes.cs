namespace NewLife.Melsec.Protocols;

/// <summary>MC协议软元件（Device）代码，二进制模式</summary>
public enum DeviceCode : Byte
{
    /// <summary>特殊继电器（位）</summary>
    SM = 0x91,

    /// <summary>特殊寄存器（字）</summary>
    SD = 0xA9,

    /// <summary>输入继电器（位，编号十六进制）</summary>
    X = 0x9C,

    /// <summary>输出继电器（位，编号十六进制）</summary>
    Y = 0x9D,

    /// <summary>直接输入（位，编号十六进制）</summary>
    DX = 0xA2,

    /// <summary>直接输出（位，编号十六进制）</summary>
    DY = 0xA3,

    /// <summary>辅助继电器（位）</summary>
    M = 0x90,

    /// <summary>锁存继电器（位）</summary>
    L = 0x92,

    /// <summary>报警继电器（位）</summary>
    F = 0x93,

    /// <summary>边沿继电器（位）</summary>
    V = 0x94,

    /// <summary>链接继电器（位，编号十六进制）</summary>
    B = 0xA0,

    /// <summary>定时器线圈（位）</summary>
    TC = 0xC1,

    /// <summary>定时器当前值（字）</summary>
    TS = 0xC0,

    /// <summary>计数器线圈（位）</summary>
    CC = 0xC5,

    /// <summary>计数器当前值（字）</summary>
    CS = 0xC4,

    /// <summary>数据寄存器（字）</summary>
    D = 0xA8,

    /// <summary>链接寄存器（字，编号十六进制）</summary>
    W = 0xB4,

    /// <summary>文件寄存器（字）</summary>
    R = 0xAF,

    /// <summary>扩展文件寄存器（字）</summary>
    ZR = 0xB0,
}

/// <summary>DeviceCode 工具方法</summary>
public static class DeviceCodeHelper
{
    /// <summary>判断是否为位软元件（Bit Device）</summary>
    /// <param name="code">软元件代码</param>
    /// <returns>true=位软元件；false=字软元件</returns>
    public static Boolean IsBitDevice(DeviceCode code) => code switch
    {
        DeviceCode.SM or DeviceCode.X or DeviceCode.Y or DeviceCode.DX or
        DeviceCode.DY or DeviceCode.M or DeviceCode.L or DeviceCode.F or
        DeviceCode.V or DeviceCode.B or DeviceCode.TC or DeviceCode.CC => true,
        _ => false,
    };

    /// <summary>解析MC协议地址字符串，返回软元件代码与地址编号</summary>
    /// <remarks>
    /// 地址格式：D100、M200、X1F、Y2A、B100、TC5、ZR10000 等。
    /// X/Y/B/W/DX/DY 使用十六进制编号；其余使用十进制编号。
    /// 冒号后面的位域（如 D100:0）会被自动忽略。
    /// </remarks>
    /// <param name="address">地址字符串</param>
    /// <returns>软元件代码与地址编号的元组</returns>
    public static (DeviceCode Code, Int32 Address) ParseAddress(String address)
    {
        if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

        // 去除冒号后面的位域，如 "D100:0" → "D100"
        var colonPos = address.IndexOf(':');
        if (colonPos > 0) address = address[..colonPos];

        address = address.ToUpperInvariant().Trim();

        // 先尝试双字符前缀（ZR、SM、SD、DX、DY、TC、TS、CC、CS）
        if (address.Length >= 3)
        {
            var prefix2 = address[..2];
            switch (prefix2)
            {
                case "SM": return (DeviceCode.SM, ParseNum(address[2..], false));
                case "SD": return (DeviceCode.SD, ParseNum(address[2..], false));
                case "DX": return (DeviceCode.DX, ParseNum(address[2..], true));
                case "DY": return (DeviceCode.DY, ParseNum(address[2..], true));
                case "TC": return (DeviceCode.TC, ParseNum(address[2..], false));
                case "TS": return (DeviceCode.TS, ParseNum(address[2..], false));
                case "CC": return (DeviceCode.CC, ParseNum(address[2..], false));
                case "CS": return (DeviceCode.CS, ParseNum(address[2..], false));
                case "ZR": return (DeviceCode.ZR, ParseNum(address[2..], false));
            }
        }

        // 单字符前缀
        if (address.Length >= 2)
        {
            var prefix1 = address[..1];
            switch (prefix1)
            {
                case "X": return (DeviceCode.X, ParseNum(address[1..], true));
                case "Y": return (DeviceCode.Y, ParseNum(address[1..], true));
                case "M": return (DeviceCode.M, ParseNum(address[1..], false));
                case "L": return (DeviceCode.L, ParseNum(address[1..], false));
                case "F": return (DeviceCode.F, ParseNum(address[1..], false));
                case "V": return (DeviceCode.V, ParseNum(address[1..], false));
                case "B": return (DeviceCode.B, ParseNum(address[1..], true));
                case "D": return (DeviceCode.D, ParseNum(address[1..], false));
                case "W": return (DeviceCode.W, ParseNum(address[1..], true));
                case "R": return (DeviceCode.R, ParseNum(address[1..], false));
            }
        }

        throw new NotSupportedException($"不支持的MC协议软元件地址格式：{address}");
    }

    private static Int32 ParseNum(String s, Boolean isHex) =>
        isHex ? Convert.ToInt32(s, 16) : Int32.Parse(s);
}
