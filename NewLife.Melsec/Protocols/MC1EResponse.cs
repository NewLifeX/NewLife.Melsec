namespace NewLife.Melsec.Protocols;

/// <summary>MC协议1E帧响应消息（A 系列兼容）</summary>
/// <remarks>
/// 1E 响应帧格式：
/// [0]    副头       1    0x00=读成功，0x80=读异常；写成功=0x00
/// [1]    结束码     1    0x00=成功
/// [2~]   响应数据   N    字读：每字 2 字节 LE；位读：每字节含 2 点（低4位=第1点，高4位=第2点）
/// </remarks>
public class MC1EResponse
{
    #region 常量

    /// <summary>读响应成功副头</summary>
    public const Byte SUB_READ_SUCCESS = 0x00;

    /// <summary>读响应异常副头</summary>
    public const Byte SUB_READ_ERROR = 0x80;

    /// <summary>写响应成功副头</summary>
    public const Byte SUB_WRITE_SUCCESS = 0x00;

    /// <summary>1E 帧响应固定头长度（副头+结束码 = 2 字节）</summary>
    public const Int32 FIXED_HEADER_LEN = 2;

    #endregion

    #region 属性

    /// <summary>副头。读响应为 0x00（成功）或 0x80（异常）；写响应为 0x00</summary>
    public Byte SubHeader { get; set; }

    /// <summary>结束码。0x00=成功，非零=错误</summary>
    public Byte EndCode { get; set; }

    /// <summary>响应原始数据（结束码之后的字节）</summary>
    public Byte[] RawData { get; set; }

    #endregion

    #region 构造

    /// <summary>已重载。友好字符串</summary>
    public override String ToString()
    {
        if (EndCode != 0) return $"MC1E Response ERROR 0x{EndCode:X2}";
        return $"MC1E Response OK, DataLen={RawData?.Length ?? 0}";
    }

    #endregion

    #region 序列化

    /// <summary>从字节数组解析响应帧</summary>
    /// <param name="data">帧数据</param>
    /// <returns>是否成功</returns>
    public Boolean Read(Byte[] data)
    {
        if (data == null || data.Length < FIXED_HEADER_LEN) return false;

        var offset = 0;
        SubHeader = data[offset++];
        EndCode = data[offset++];

        var dataLen = data.Length - FIXED_HEADER_LEN;
        if (dataLen > 0)
        {
            RawData = new Byte[dataLen];
            Array.Copy(data, offset, RawData, 0, dataLen);
        }

        return true;
    }

    /// <summary>序列化为字节数组（用于测试模拟服务端返回）</summary>
    public Byte[] ToBytes()
    {
        var dataLength = FIXED_HEADER_LEN + (RawData?.Length ?? 0);
        var buffer = new Byte[dataLength];
        var offset = 0;

        buffer[offset++] = SubHeader;
        buffer[offset++] = EndCode;

        if (RawData != null && RawData.Length > 0)
            Array.Copy(RawData, 0, buffer, offset, RawData.Length);

        return buffer;
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
    public static MC1EResponse BuildWordResponse(UInt16[] values)
    {
        var raw = MCMessage.PackWords(values);
        return new MC1EResponse { SubHeader = SUB_READ_SUCCESS, EndCode = 0, RawData = raw };
    }

    /// <summary>构造成功的位读取响应（用于测试）</summary>
    /// <param name="values">位数据（true=ON，false=OFF）</param>
    public static MC1EResponse BuildBitResponse(Boolean[] values)
    {
        var ushorts = Array.ConvertAll(values, v => v ? (UInt16)1 : (UInt16)0);
        var raw = MCMessage.PackBits(ushorts);
        return new MC1EResponse { SubHeader = SUB_READ_SUCCESS, EndCode = 0, RawData = raw };
    }

    /// <summary>构造成功的写入响应（用于测试）</summary>
    public static MC1EResponse BuildWriteResponse() => new() { SubHeader = SUB_WRITE_SUCCESS, EndCode = 0 };

    /// <summary>构造错误响应（用于测试）</summary>
    /// <param name="endCode">错误码</param>
    public static MC1EResponse BuildErrorResponse(Byte endCode) => new() { SubHeader = SUB_READ_ERROR, EndCode = endCode };

    #endregion
}
