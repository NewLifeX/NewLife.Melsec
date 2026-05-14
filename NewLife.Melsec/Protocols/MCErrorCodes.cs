namespace NewLife.Melsec.Protocols;

/// <summary>MC协议结束码（EndCode）</summary>
/// <remarks>结束码 0x0000 表示成功；其余均为错误码。常见错误码定义如下。</remarks>
public enum MCEndCode : UInt16
{
    /// <summary>正常结束</summary>
    Success = 0x0000,

    /// <summary>PLC 正在运行，不能执行（请求在 RUN 模式下不被允许）</summary>
    PlcRunning = 0xC050,

    /// <summary>软元件地址超范围</summary>
    AddressOutOfRange = 0xC056,

    /// <summary>命令或子命令错误</summary>
    CommandError = 0xC059,

    /// <summary>点数超出范围</summary>
    PointCountError = 0xC05B,

    /// <summary>请求数据长度与实际不匹配</summary>
    DataLengthError = 0xC060,

    /// <summary>CPU 正在处理其他请求</summary>
    CpuBusy = 0xC0B5,

    /// <summary>通信超时（路由超时）</summary>
    Timeout = 0xC100,
}

/// <summary>MC协议异常</summary>
public class MCException : Exception
{
    /// <summary>结束码</summary>
    public UInt16 EndCode { get; set; }

    /// <summary>实例化MC协议异常</summary>
    /// <param name="endCode">结束码</param>
    public MCException(UInt16 endCode)
        : base(GetMessage(endCode)) => EndCode = endCode;

    /// <summary>实例化MC协议异常</summary>
    /// <param name="endCode">结束码</param>
    /// <param name="message">附加描述</param>
    public MCException(UInt16 endCode, String message)
        : base(message) => EndCode = endCode;

    private static String GetMessage(UInt16 code)
    {
        var known = (MCEndCode)code;
        return known switch
        {
            MCEndCode.Success => "正常",
            MCEndCode.PlcRunning => $"0x{code:X4} PLC正在运行，无法执行请求",
            MCEndCode.AddressOutOfRange => $"0x{code:X4} 软元件地址超范围",
            MCEndCode.CommandError => $"0x{code:X4} 命令或子命令错误",
            MCEndCode.PointCountError => $"0x{code:X4} 读写点数超出范围",
            MCEndCode.DataLengthError => $"0x{code:X4} 请求数据长度不匹配",
            MCEndCode.CpuBusy => $"0x{code:X4} CPU忙，请稍后重试",
            MCEndCode.Timeout => $"0x{code:X4} 通信超时",
            _ => $"0x{code:X4} MC协议错误",
        };
    }
}
