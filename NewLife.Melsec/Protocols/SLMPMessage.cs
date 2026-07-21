namespace NewLife.Melsec.Protocols;

/// <summary>SLMP 协议 3C 帧请求消息</summary>
/// <remarks>
/// SLMP（Seamless Message Protocol）3C 帧格式与 MC 3E 帧二进制模式完全兼容。
/// 子头 5000h，Little-Endian 字节序。
/// 
/// 此类是对 <see cref="MCMessage"/> 的 SLMP 语义封装，内部复用其序列化逻辑。
/// 适用于 iQ-R/iQ-F 系列等支持 SLMP 标准的三菱设备。
/// </remarks>
public class SLMPMessage
{
    private readonly MCMessage _inner;

    /// <summary>内部 MCMessage 实例</summary>
    public MCMessage Inner => _inner;

    /// <summary>实例化 SLMP 请求消息</summary>
    public SLMPMessage() => _inner = new MCMessage { DataFormat = MCDataFormat.Binary };

    /// <summary>使用现有 MCMessage 实例包装</summary>
    /// <param name="message">MCMessage 实例</param>
    public SLMPMessage(MCMessage message) => _inner = message;

    /// <summary>网络号。通常 0x00</summary>
    public Byte NetworkNo { get => _inner.NetworkNo; set => _inner.NetworkNo = value; }

    /// <summary>PC号。通常 0xFF</summary>
    public Byte PCNo { get => _inner.PCNo; set => _inner.PCNo = value; }

    /// <summary>命令码</summary>
    public UInt16 Command { get => _inner.Command; set => _inner.Command = value; }

    /// <summary>子命令</summary>
    public UInt16 SubCommand { get => _inner.SubCommand; set => _inner.SubCommand = value; }

    /// <summary>起始地址</summary>
    public Int32 StartAddress { get => _inner.StartAddress; set => _inner.StartAddress = value; }

    /// <summary>软元件代码</summary>
    public SLMPDeviceCode DeviceCode { get => (SLMPDeviceCode)_inner.DeviceCode; set => _inner.DeviceCode = (DeviceCode)value; }

    /// <summary>软元件点数</summary>
    public UInt16 Count { get => _inner.Count; set => _inner.Count = value; }

    /// <summary>写入数据</summary>
    public UInt16[] WriteData { get => _inner.WriteData; set => _inner.WriteData = value; }

    /// <summary>序列化为字节数组</summary>
    public Byte[] ToBytes() => _inner.ToBytes();

    /// <summary>从字节数组反序列化</summary>
    public Boolean Read(Byte[] data)
    {
        using var ms = new MemoryStream(data);
        return _inner.Read(ms, null);
    }

    /// <summary>创建对应的响应对象</summary>
    public SLMPResponse CreateReply() => new(_inner.CreateReply());

    /// <summary>友好字符串</summary>
    public override String ToString() => $"SLMP {_inner}";

    #region 工厂方法

    /// <summary>构造字软元件批量读取请求</summary>
    public static SLMPMessage BuildReadWord(SLMPDeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var inner = MCMessage.BuildReadWord((DeviceCode)devCode, startAddr, count);
        return new SLMPMessage(inner);
    }

    /// <summary>构造位软元件批量读取请求</summary>
    public static SLMPMessage BuildReadBit(SLMPDeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var inner = MCMessage.BuildReadBit((DeviceCode)devCode, startAddr, count);
        return new SLMPMessage(inner);
    }

    /// <summary>构造字软元件批量写入请求</summary>
    public static SLMPMessage BuildWriteWord(SLMPDeviceCode devCode, Int32 startAddr, UInt16[] values)
    {
        var inner = MCMessage.BuildWriteWord((DeviceCode)devCode, startAddr, values);
        return new SLMPMessage(inner);
    }

    /// <summary>构造位软元件批量写入请求</summary>
    public static SLMPMessage BuildWriteBit(SLMPDeviceCode devCode, Int32 startAddr, UInt16[] values)
    {
        var inner = MCMessage.BuildWriteBit((DeviceCode)devCode, startAddr, values);
        return new SLMPMessage(inner);
    }

    #endregion
}
