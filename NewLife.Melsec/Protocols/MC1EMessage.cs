namespace NewLife.Melsec.Protocols;

/// <summary>MC协议1E帧请求消息（A 系列兼容）</summary>
/// <remarks>
/// 1E 帧格式（Little-Endian）：
/// [0]    副头       1    0x00=字读，0x01=位读，0x02=字写，0x03=位写
/// [1]    PLC号      1    通常 0xFF
/// [2~3]  监视定时器 2    LE
/// [4~5]  起始地址   2    LE（1E 帧地址为 16 位）
/// [6]    软元件代码 1    见 MC1EDeviceCode 枚举（A 系列兼容）
/// [7~8]  点数       2    LE
/// [9~]   写入数据   N    仅写命令携带，格式同 3E（字写：每字 2 字节 LE；位写：每字节 2 点）
/// </remarks>
public class MC1EMessage
{
    #region 常量

    /// <summary>字软元件读取副头</summary>
    public const Byte SUB_READ_WORD = 0x00;

    /// <summary>位软元件读取副头</summary>
    public const Byte SUB_READ_BIT = 0x01;

    /// <summary>字软元件写入副头</summary>
    public const Byte SUB_WRITE_WORD = 0x02;

    /// <summary>位软元件写入副头</summary>
    public const Byte SUB_WRITE_BIT = 0x03;

    /// <summary>PLC 号默认值</summary>
    public const Byte DEFAULT_PC_NO = 0xFF;

    /// <summary>1E 帧请求固定头长度（副头+PLC号+监视定时器+起始地址+软元件代码+点数 = 9 字节）</summary>
    public const Int32 FIXED_HEADER_LEN = 9;

    #endregion

    #region 属性

    /// <summary>副头。指示读写操作和字/位模式</summary>
    public Byte SubHeader { get; set; }

    /// <summary>PLC号。通常 0xFF</summary>
    public Byte PCNo { get; set; } = DEFAULT_PC_NO;

    /// <summary>监视定时器。单位 250ms，默认 10（= 2500ms）</summary>
    public UInt16 MonitoringTimer { get; set; } = 0x000A;

    /// <summary>起始地址（16 位）。1E 帧地址最大 0xFFFF</summary>
    public UInt16 StartAddress { get; set; }

    /// <summary>软元件代码（A 系列兼容）</summary>
    public MC1EDeviceCode DeviceCode { get; set; }

    /// <summary>软元件点数</summary>
    public UInt16 Count { get; set; }

    /// <summary>写入数据（仅写命令携带）。位写时每个元素 0=OFF 1=ON；字写时为字值</summary>
    public UInt16[] WriteData { get; set; }

    #endregion

    #region 构造

    /// <summary>已重载。友好字符串</summary>
    public override String ToString()
    {
        var op = SubHeader switch
        {
            SUB_READ_WORD => "Read Word",
            SUB_READ_BIT => "Read Bit",
            SUB_WRITE_WORD => "Write Word",
            SUB_WRITE_BIT => "Write Bit",
            _ => $"Cmd={SubHeader:X2}",
        };
        return $"MC1E {op} {DeviceCode} addr={StartAddress} count={Count}";
    }

    #endregion

    #region 序列化

    /// <summary>序列化为字节数组</summary>
    public Byte[] ToBytes()
    {
        var dataLength = FIXED_HEADER_LEN;

        // 打包写入数据
        Byte[] writeBytes = null;
        if (WriteData != null && WriteData.Length > 0)
        {
            if (SubHeader == SUB_WRITE_BIT || SubHeader == SUB_READ_BIT)
                writeBytes = MCMessage.PackBits(WriteData);
            else
                writeBytes = MCMessage.PackWords(WriteData);
            dataLength += writeBytes.Length;
        }

        var buffer = new Byte[dataLength];
        var offset = 0;

        buffer[offset++] = SubHeader;
        buffer[offset++] = PCNo;
        WriteUInt16LE(buffer, ref offset, MonitoringTimer);
        WriteUInt16LE(buffer, ref offset, StartAddress);
        buffer[offset++] = (Byte)DeviceCode;
        WriteUInt16LE(buffer, ref offset, Count);

        if (writeBytes != null)
        {
            Array.Copy(writeBytes, 0, buffer, offset, writeBytes.Length);
            offset += writeBytes.Length;
        }

        return buffer;
    }

    /// <summary>从字节数组反序列化</summary>
    /// <param name="data">帧数据</param>
    /// <returns>是否成功</returns>
    public Boolean Read(Byte[] data)
    {
        if (data == null || data.Length < FIXED_HEADER_LEN) return false;

        var offset = 0;
        SubHeader = data[offset++];
        PCNo = data[offset++];
        MonitoringTimer = ReadUInt16LE(data, ref offset);
        StartAddress = ReadUInt16LE(data, ref offset);
        DeviceCode = (MC1EDeviceCode)data[offset++];
        Count = ReadUInt16LE(data, ref offset);

        // 写入数据
        var extraLen = data.Length - FIXED_HEADER_LEN;
        if (extraLen > 0)
        {
            var buf = new Byte[extraLen];
            Array.Copy(data, offset, buf, 0, extraLen);
            WriteData = UnpackWriteData(buf, Count, SubHeader);
        }

        return true;
    }

    /// <summary>创建对应的 1E 响应对象</summary>
    public MC1EResponse CreateReply() => new();

    #endregion

    #region 工厂方法

    /// <summary>构造字软元件批量读取请求</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public static MC1EMessage BuildReadWord(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count) => new()
    {
        SubHeader = SUB_READ_WORD,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)count,
    };

    /// <summary>构造位软元件批量读取请求</summary>
    public static MC1EMessage BuildReadBit(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count) => new()
    {
        SubHeader = SUB_READ_BIT,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)count,
    };

    /// <summary>构造字软元件批量写入请求</summary>
    public static MC1EMessage BuildWriteWord(MC1EDeviceCode devCode, UInt16 startAddr, UInt16[] values) => new()
    {
        SubHeader = SUB_WRITE_WORD,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)values.Length,
        WriteData = values,
    };

    /// <summary>构造位软元件批量写入请求</summary>
    public static MC1EMessage BuildWriteBit(MC1EDeviceCode devCode, UInt16 startAddr, UInt16[] values) => new()
    {
        SubHeader = SUB_WRITE_BIT,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)values.Length,
        WriteData = values,
    };

    #endregion

    #region 辅助

    private static UInt16[] UnpackWriteData(Byte[] buf, UInt16 count, Byte subHeader)
    {
        if (subHeader == SUB_WRITE_BIT || subHeader == SUB_READ_BIT)
        {
            var result = new UInt16[count];
            for (var i = 0; i < count; i++)
            {
                var byteIdx = i / 2;
                var shift = (i % 2) * 4;
                result[i] = (UInt16)((buf[byteIdx] >> shift) & 0x0F);
            }
            return result;
        }
        else
        {
            var result = new UInt16[buf.Length / 2];
            for (var i = 0; i < result.Length; i++)
                result[i] = (UInt16)(buf[i * 2] | (buf[i * 2 + 1] << 8));
            return result;
        }
    }

    private static void WriteUInt16LE(Byte[] buffer, ref Int32 offset, UInt16 value)
    {
        buffer[offset++] = (Byte)(value & 0xFF);
        buffer[offset++] = (Byte)(value >> 8);
    }

    private static UInt16 ReadUInt16LE(Byte[] data, ref Int32 offset)
    {
        var value = (UInt16)(data[offset] | (data[offset + 1] << 8));
        offset += 2;
        return value;
    }

    #endregion
}
