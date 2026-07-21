using NewLife.Serialization;

namespace NewLife.Melsec.Protocols;

/// <summary>MC协议3E帧响应消息（二进制/ASCII模式）</summary>
/// <remarks>
/// 帧格式（二进制 Little-Endian）：
/// [0~1]  子头       2    固定 0xD0 0x00
/// [2]    网络号     1
/// [3]    PC号       1
/// [4~5]  IO单元号   2
/// [6]    通道号     1
/// [7~8]  数据长度   2    LE，从字节[9]起的字节数
/// [9~10] 结束码     2    LE，0x0000=成功，非零=错误
/// [11~]  响应数据   N    字读：每字 2 字节 LE；位读：每字节含 2 点（低4位=第1点，高4位=第2点）
///
/// ASCII 模式下，每个字节以 2 个 ASCII 十六进制字符表示，
/// 字节序与二进制模式一致。
/// </remarks>
public class MCResponse : IAccessor
{
    #region 常量

    /// <summary>3E帧子头（响应，二进制）</summary>
    public const UInt16 SUB_HEADER = 0x00D0;

    /// <summary>3E帧子头（响应，ASCII）</summary>
    public const String SUB_HEADER_ASCII = "D000";

    /// <summary>响应帧固定头长度（子头+网络号+PC号+IO单元号+通道号+数据长度 = 9字节）</summary>
    public const Int32 FIXED_HEADER_LEN = 9;

    /// <summary>ASCII 响应帧固定头长度（9字节二进制 → 18 ASCII 字符 + 数据长度字段本身 = 9*2 = 18）</summary>
    public const Int32 FIXED_HEADER_ASCII_LEN = 18;

    #endregion

    #region 属性

    /// <summary>数据格式。默认 Binary</summary>
    public MCDataFormat DataFormat { get; set; } = MCDataFormat.Binary;

    /// <summary>网络号</summary>
    public Byte NetworkNo { get; set; }

    /// <summary>PC号</summary>
    public Byte PCNo { get; set; }

    /// <summary>IO单元号</summary>
    public UInt16 IOUnitNo { get; set; }

    /// <summary>通道号</summary>
    public Byte ChannelNo { get; set; }

    /// <summary>结束码。0x0000 = 成功；非零 = 错误</summary>
    public UInt16 EndCode { get; set; }

    /// <summary>响应原始数据（结束码之后的字节）</summary>
    public Byte[] RawData { get; set; }

    #endregion

    #region 构造

    /// <summary>已重载。友好字符串</summary>
    public override String ToString()
    {
        if (EndCode != 0) return $"MC Response ERROR 0x{EndCode:X4}";
        return $"MC Response OK, DataLen={RawData?.Length ?? 0}";
    }

    #endregion

    #region 序列化

    /// <summary>读取（反序列化）响应帧</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文（忽略）</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        if (DataFormat.IsAscii())
            return ReadAscii(stream);

        // 子头 0xD0 0x00
        var sh0 = stream.ReadByte();
        var sh1 = stream.ReadByte();
        if (sh0 != 0xD0 || sh1 != 0x00) return false;

        NetworkNo = (Byte)stream.ReadByte();
        PCNo = (Byte)stream.ReadByte();
        IOUnitNo = MCMessage.ReadUInt16LE(stream);
        ChannelNo = (Byte)stream.ReadByte();

        var dataLength = MCMessage.ReadUInt16LE(stream);  // EndCode(2) + ResponseData(N)
        EndCode = MCMessage.ReadUInt16LE(stream);

        // 响应数据 = dataLength - EndCode(2)
        var dataLen = dataLength - 2;
        if (dataLen > 0)
        {
            RawData = new Byte[dataLen];
            stream.Read(RawData, 0, dataLen);
        }

        return true;
    }

    /// <summary>ASCII 模式反序列化</summary>
    private Boolean ReadAscii(Stream stream)
    {
        // 子头 "D000" (4 ASCII chars → 2 bytes)
        var sh = MCMessage.ReadAsciiHex(stream, 2);
        if (sh[0] != 0xD0 || sh[1] != 0x00) return false;

        NetworkNo = MCMessage.ReadAsciiByte(stream);
        PCNo = MCMessage.ReadAsciiByte(stream);
        IOUnitNo = MCMessage.ReadAsciiUInt16(stream);
        ChannelNo = MCMessage.ReadAsciiByte(stream);

        var dataLength = MCMessage.ReadAsciiUInt16(stream);  // EndCode(2) + ResponseData(N)
        EndCode = MCMessage.ReadAsciiUInt16(stream);

        // 响应数据 = dataLength - EndCode(2)
        var dataLen = dataLength - 2;
        if (dataLen > 0)
        {
            RawData = MCMessage.ReadAsciiHex(stream, dataLen);
        }

        return true;
    }

    /// <summary>写入（序列化）响应帧（主要用于测试模拟）</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文（忽略）</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Write(Stream stream, Object context)
    {
        if (DataFormat.IsAscii())
            return WriteAscii(stream);

        // 子头 0xD0 0x00
        stream.WriteByte(0xD0);
        stream.WriteByte(0x00);

        stream.WriteByte(NetworkNo);
        stream.WriteByte(PCNo);
        MCMessage.WriteUInt16LE(stream, IOUnitNo);
        stream.WriteByte(ChannelNo);

        // 数据长度 = EndCode(2) + RawData
        var dataLength = (UInt16)(2 + (RawData?.Length ?? 0));
        MCMessage.WriteUInt16LE(stream, dataLength);
        MCMessage.WriteUInt16LE(stream, EndCode);

        if (RawData != null && RawData.Length > 0)
            stream.Write(RawData, 0, RawData.Length);

        return true;
    }

    /// <summary>ASCII 模式序列化</summary>
    private Boolean WriteAscii(Stream stream)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(0xD0);
        ms.WriteByte(0x00);

        ms.WriteByte(NetworkNo);
        ms.WriteByte(PCNo);
        MCMessage.WriteUInt16LE(ms, IOUnitNo);
        ms.WriteByte(ChannelNo);

        var dataLength = (UInt16)(2 + (RawData?.Length ?? 0));
        MCMessage.WriteUInt16LE(ms, dataLength);
        MCMessage.WriteUInt16LE(ms, EndCode);

        if (RawData != null && RawData.Length > 0)
            ms.Write(RawData, 0, RawData.Length);

        // 二进制字节 → ASCII 十六进制字符串 → 写入输出流
        var binary = ms.ToArray();
        var hex = binary.ToHex();
        var hexBytes = System.Text.Encoding.ASCII.GetBytes(hex);
        stream.Write(hexBytes, 0, hexBytes.Length);

        return true;
    }

    /// <summary>序列化为字节数组（用于测试模拟服务端返回数据）</summary>
    public Byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        Write(ms, null);
        return ms.ToArray();
    }

    /// <summary>从字节数组解析响应帧</summary>
    /// <param name="data">帧数据（二进制或 ASCII 模式）</param>
    /// <param name="format">数据格式</param>
    public static MCResponse FromBytes(Byte[] data, MCDataFormat format = MCDataFormat.Binary)
    {
        using var ms = new MemoryStream(data);
        var response = new MCResponse { DataFormat = format };
        response.Read(ms, null);
        return response;
    }

    #endregion

    #region 数据解析

    /// <summary>将响应数据解析为字软元件数组（每字 2 字节 LE）</summary>
    public UInt16[] GetWordData()
    {
        if (RawData == null || RawData.Length == 0) return [];
        var words = new UInt16[RawData.Length / 2];
        for (var i = 0; i < words.Length; i++)
            words[i] = (UInt16)(RawData[i * 2] | (RawData[i * 2 + 1] << 8));
        return words;
    }

    /// <summary>将响应数据解析为位软元件数组（每字节含 2 点，低 4 位 = 第 1 点，高 4 位 = 第 2 点）</summary>
    /// <param name="count">期望的点数</param>
    public Boolean[] GetBitData(Int32 count)
    {
        if (RawData == null || RawData.Length == 0) return [];
        var bits = new Boolean[count];
        for (var i = 0; i < count; i++)
        {
            var byteIdx = i / 2;
            var shift = (i % 2) * 4;
            if (byteIdx < RawData.Length)
                bits[i] = ((RawData[byteIdx] >> shift) & 0x0F) != 0;
        }
        return bits;
    }

    #endregion

    #region 工厂方法（测试辅助）

    /// <summary>构造成功的字读取响应（用于测试）</summary>
    /// <param name="values">字数据</param>
    public static MCResponse BuildWordResponse(UInt16[] values)
    {
        var raw = MCMessage.PackWords(values);
        return new MCResponse { EndCode = 0, RawData = raw };
    }

    /// <summary>构造成功的位读取响应（用于测试）</summary>
    /// <param name="values">位数据（true=ON，false=OFF）</param>
    public static MCResponse BuildBitResponse(Boolean[] values)
    {
        var ushorts = Array.ConvertAll(values, v => v ? (UInt16)1 : (UInt16)0);
        var raw = MCMessage.PackBits(ushorts);
        return new MCResponse { EndCode = 0, RawData = raw };
    }

    /// <summary>构造成功的写入响应（用于测试）</summary>
    public static MCResponse BuildWriteResponse() => new() { EndCode = 0 };

    /// <summary>构造错误响应（用于测试）</summary>
    /// <param name="endCode">错误码</param>
    public static MCResponse BuildErrorResponse(UInt16 endCode) => new() { EndCode = endCode };

    #endregion
}
