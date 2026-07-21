namespace NewLife.Melsec.Protocols;

/// <summary>SLMP 协议 3C 帧响应消息</summary>
/// <remarks>
/// SLMP 3C 帧响应格式与 MC 3E 帧完全兼容。
/// 子头 D000h，结束码 0000h=成功。
/// 此类是对 <see cref="MCResponse"/> 的 SLMP 语义封装。
/// </remarks>
public class SLMPResponse
{
    private readonly MCResponse _inner;

    /// <summary>内部 MCResponse 实例</summary>
    public MCResponse Inner => _inner;

    /// <summary>实例化 SLMP 响应</summary>
    public SLMPResponse() => _inner = new MCResponse();

    /// <summary>使用现有 MCResponse 实例包装</summary>
    public SLMPResponse(MCResponse response) => _inner = response;

    /// <summary>结束码。0x0000 = 成功；非零 = 错误</summary>
    public UInt16 EndCode => _inner.EndCode;

    /// <summary>响应原始数据</summary>
    public Byte[] RawData { get => _inner.RawData; set => _inner.RawData = value; }

    /// <summary>将响应数据解析为字软元件数组（每字 2 字节 LE）</summary>
    public UInt16[] GetWordData() => _inner.GetWordData();

    /// <summary>将响应数据解析为位软元件数组</summary>
    public Boolean[] GetBitData(Int32 count) => _inner.GetBitData(count);

    /// <summary>从字节数组解析响应帧</summary>
    public Boolean Read(Byte[] data)
    {
        using var ms = new MemoryStream(data);
        return _inner.Read(ms, null);
    }

    /// <summary>序列化为字节数组（用于测试模拟）</summary>
    public Byte[] ToBytes() => _inner.ToBytes();

    /// <summary>友好字符串</summary>
    public override String ToString() => $"SLMP {_inner}";

    #region 工厂方法（测试辅助）

    /// <summary>构造成功的字读取响应（用于测试）</summary>
    public static SLMPResponse BuildWordResponse(UInt16[] values) =>
        new(MCResponse.BuildWordResponse(values));

    /// <summary>构造成功的位读取响应（用于测试）</summary>
    public static SLMPResponse BuildBitResponse(Boolean[] values) =>
        new(MCResponse.BuildBitResponse(values));

    /// <summary>构造成功的写入响应（用于测试）</summary>
    public static SLMPResponse BuildWriteResponse() =>
        new(MCResponse.BuildWriteResponse());

    /// <summary>构造错误响应（用于测试）</summary>
    public static SLMPResponse BuildErrorResponse(UInt16 endCode) =>
        new(MCResponse.BuildErrorResponse(endCode));

    #endregion
}
