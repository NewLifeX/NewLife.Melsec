using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Melsec.Protocols;

/// <summary>MC协议3E帧请求消息（二进制/ASCII模式）</summary>
/// <remarks>
/// 帧格式（二进制 Little-Endian）：
/// [0~1]  子头       2    固定 0x50 0x00
/// [2]    网络号     1    通常 0x00
/// [3]    PC号       1    通常 0xFF
/// [4~5]  IO单元号   2    固定 0xFF 0x03 (= 0x03FF LE)
/// [6]    通道号     1    通常 0x00
/// [7~8]  数据长度   2    LE，从字节[9]起的字节数
/// [9~10] 监视定时器 2    LE，默认 0x000A（10 × 250ms = 2.5s）
/// [11~12]命令       2    LE，0x0401=批量读，0x1401=批量写
/// [13~14]子命令     2    LE，0x0000=字模式，0x0001=位模式
/// [15~17]起始软元件 3    LE，起始地址（最大 0xFFFFFF）
/// [18]   软元件代码 1    见 DeviceCode 枚举
/// [19~20]软元件点数 2    LE
/// [21~]  写入数据   N    仅写命令携带
///
/// ASCII 模式下，每个字节以 2 个 ASCII 十六进制字符表示，
/// 字节序与二进制模式一致（Little-Endian 字节序在十六进制字符串中顺序保留）。
/// 例如命令 0x0401 在二进制中为 [01][04]，ASCII 中为 "0104"。
/// </remarks>
public class MCMessage : IAccessor
{
    #region 常量

    /// <summary>批量读取命令码</summary>
    public const UInt16 CMD_READ = 0x0401;

    /// <summary>批量写入命令码</summary>
    public const UInt16 CMD_WRITE = 0x1401;

    /// <summary>随机读取命令码</summary>
    public const UInt16 CMD_RANDOM_READ = 0x0403;

    /// <summary>随机写入命令码</summary>
    public const UInt16 CMD_RANDOM_WRITE = 0x1403;

    /// <summary>远程 RUN 命令码</summary>
    public const UInt16 CMD_REMOTE_RUN = 0x1001;

    /// <summary>远程 STOP 命令码</summary>
    public const UInt16 CMD_REMOTE_STOP = 0x1002;

    /// <summary>字软元件子命令</summary>
    public const UInt16 SUBCMD_WORD = 0x0000;

    /// <summary>位软元件子命令</summary>
    public const UInt16 SUBCMD_BIT = 0x0001;

    /// <summary>3E帧子头（请求，二进制）</summary>
    public const UInt16 SUB_HEADER = 0x0050;

    /// <summary>3E帧子头（请求，ASCII）</summary>
    public const String SUB_HEADER_ASCII = "5000";

    /// <summary>IO单元号（Q/L/FX5U系列固定值）</summary>
    public const UInt16 IO_UNIT_NO = 0x03FF;

    #endregion

    #region 属性

    /// <summary>数据格式。默认 Binary</summary>
    public MCDataFormat DataFormat { get; set; } = MCDataFormat.Binary;

    /// <summary>网络号。通常 0x00 表示本机</summary>
    public Byte NetworkNo { get; set; } = 0x00;

    /// <summary>PC号。通常 0xFF</summary>
    public Byte PCNo { get; set; } = 0xFF;

    /// <summary>IO单元号。Q/L/FX5U系列固定 0x03FF</summary>
    public UInt16 IOUnitNo { get; set; } = IO_UNIT_NO;

    /// <summary>通道号。通常 0x00</summary>
    public Byte ChannelNo { get; set; } = 0x00;

    /// <summary>监视定时器。单位 250ms，默认 10（= 2500ms）</summary>
    public UInt16 MonitoringTimer { get; set; } = 0x000A;

    /// <summary>命令码。0x0401=批量读，0x1401=批量写</summary>
    public UInt16 Command { get; set; }

    /// <summary>子命令。0x0000=字模式，0x0001=位模式</summary>
    public UInt16 SubCommand { get; set; }

    /// <summary>起始软元件地址</summary>
    public Int32 StartAddress { get; set; }

    /// <summary>软元件代码</summary>
    public DeviceCode DeviceCode { get; set; }

    /// <summary>软元件点数</summary>
    public UInt16 Count { get; set; }

    /// <summary>写入数据（仅写命令携带）。位写时每个元素 0=OFF 1=ON；字写时为字值</summary>
    public UInt16[] WriteData { get; set; }

    /// <summary>原始请求数据。当设置此属性时，序列化使用它替代标准的起始地址+软元件代码+点数区域</summary>
    /// <remarks>
    /// 用于随机读取（0403h）、远程控制（1001h/1002h）等非标准请求。
    /// 设置后，<see cref="StartAddress"/>、<see cref="DeviceCode"/>、<see cref="Count"/> 将被忽略。
    /// 不包含子头+网络号+PC号+IO单元号+通道号+数据长度+监视定时器+命令+子命令等固定头。
    /// </remarks>
    public Byte[] RawRequestData { get; set; }

    #endregion

    #region 构造

    /// <summary>已重载。友好字符串</summary>
    public override String ToString()
    {
        var cmd = Command == CMD_READ ? "Read" : Command == CMD_WRITE ? "Write" : $"Cmd={Command:X4}";
        var mode = SubCommand == SUBCMD_BIT ? "Bit" : "Word";
        return $"MC {cmd}({mode}) {DeviceCode} addr={StartAddress} count={Count}";
    }

    #endregion

    #region 序列化

    /// <summary>读取（反序列化）帧数据</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文（忽略）</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        if (DataFormat.IsAscii())
            return ReadAscii(stream);

        // 子头 0x5000
        var sh0 = stream.ReadByte();
        var sh1 = stream.ReadByte();
        if (sh0 != 0x50 || sh1 != 0x00) return false;

        NetworkNo = (Byte)stream.ReadByte();
        PCNo = (Byte)stream.ReadByte();
        IOUnitNo = ReadUInt16LE(stream);
        ChannelNo = (Byte)stream.ReadByte();

        var dataLength = ReadUInt16LE(stream);
        MonitoringTimer = ReadUInt16LE(stream);
        Command = ReadUInt16LE(stream);
        SubCommand = ReadUInt16LE(stream);

        // 起始地址 3 字节 LE
        StartAddress = stream.ReadByte() | (stream.ReadByte() << 8) | (stream.ReadByte() << 16);
        DeviceCode = (DeviceCode)stream.ReadByte();
        Count = ReadUInt16LE(stream);

        // 写入数据（如有）
        // dataLength = MonTimer(2)+Cmd(2)+SubCmd(2)+StartAddr(3)+DevCode(1)+Count(2) + writeDataBytes
        //            = 12 + writeDataBytes
        var fixedLen = 2 + 2 + 2 + 3 + 1 + 2; // 12
        var extraLen = dataLength - fixedLen;
        if (extraLen > 0)
        {
            var buf = new Byte[extraLen];
            stream.Read(buf, 0, extraLen);
            WriteData = UnpackWriteData(buf, Count, SubCommand);
        }

        return true;
    }

    /// <summary>ASCII 模式反序列化</summary>
    private Boolean ReadAscii(Stream stream)
    {
        // 子头 "5000" (4 ASCII chars)
        var sh = ReadAsciiHex(stream, 2);
        if (sh[0] != 0x50 || sh[1] != 0x00) return false;

        NetworkNo = ReadAsciiByte(stream);
        PCNo = ReadAsciiByte(stream);
        IOUnitNo = ReadAsciiUInt16(stream);
        ChannelNo = ReadAsciiByte(stream);

        var dataLength = ReadAsciiUInt16(stream);
        MonitoringTimer = ReadAsciiUInt16(stream);
        Command = ReadAsciiUInt16(stream);
        SubCommand = ReadAsciiUInt16(stream);

        // 起始地址 3 字节 LE
        var addrBytes = ReadAsciiHex(stream, 3);
        StartAddress = addrBytes[0] | (addrBytes[1] << 8) | (addrBytes[2] << 16);
        DeviceCode = (DeviceCode)ReadAsciiByte(stream);
        Count = ReadAsciiUInt16(stream);

        // 写入数据（如有）
        var fixedLen = 2 + 2 + 2 + 3 + 1 + 2; // 12
        var extraLen = dataLength - fixedLen;
        if (extraLen > 0)
        {
            var buf = ReadAsciiHex(stream, extraLen);
            WriteData = UnpackWriteData(buf, Count, SubCommand);
        }

        return true;
    }

    /// <summary>写入（序列化）帧数据</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文（忽略）</param>
    /// <returns>是否成功</returns>
    public virtual Boolean Write(Stream stream, Object context)
    {
        if (DataFormat.IsAscii())
            return WriteAscii(stream);

        // 子头 0x50 0x00
        stream.WriteByte(0x50);
        stream.WriteByte(0x00);

        stream.WriteByte(NetworkNo);
        stream.WriteByte(PCNo);
        WriteUInt16LE(stream, IOUnitNo);
        stream.WriteByte(ChannelNo);

        // 打包写入数据
        Byte[] writeBytes = null;
        if (WriteData != null && WriteData.Length > 0)
            writeBytes = SubCommand == SUBCMD_BIT ? PackBits(WriteData) : PackWords(WriteData);

        if (RawRequestData != null)
        {
            // 使用原始请求数据替换标准区域
            var dataLength = (UInt16)(12 - 3 - 1 - 2 + RawRequestData.Length); // 标准区域=6字节，替换为自定义数据
            WriteUInt16LE(stream, dataLength);

            WriteUInt16LE(stream, MonitoringTimer);
            WriteUInt16LE(stream, Command);
            WriteUInt16LE(stream, SubCommand);

            stream.Write(RawRequestData, 0, RawRequestData.Length);
        }
        else
        {
            // 常规：数据长度 = MonTimer(2) + Cmd(2) + SubCmd(2) + StartAddr(3) + DevCode(1) + Count(2) + writeBytes
            var dataLength = (UInt16)(12 + (writeBytes?.Length ?? 0));
            WriteUInt16LE(stream, dataLength);

            WriteUInt16LE(stream, MonitoringTimer);
            WriteUInt16LE(stream, Command);
            WriteUInt16LE(stream, SubCommand);

            // 起始地址 3 字节 LE
            stream.WriteByte((Byte)(StartAddress & 0xFF));
            stream.WriteByte((Byte)((StartAddress >> 8) & 0xFF));
            stream.WriteByte((Byte)((StartAddress >> 16) & 0xFF));

            stream.WriteByte((Byte)DeviceCode);
            WriteUInt16LE(stream, Count);

            if (writeBytes != null)
                stream.Write(writeBytes, 0, writeBytes.Length);
        }

        return true;
    }

    /// <summary>ASCII 模式序列化</summary>
    private Boolean WriteAscii(Stream stream)
    {
        // 先序列化为二进制字节，再整体转为 ASCII 十六进制字符串写入
        using var ms = new MemoryStream();
        ms.WriteByte(0x50);
        ms.WriteByte(0x00);

        ms.WriteByte(NetworkNo);
        ms.WriteByte(PCNo);
        WriteUInt16LE(ms, IOUnitNo);
        ms.WriteByte(ChannelNo);

        // 打包写入数据
        Byte[] writeBytes = null;
        if (WriteData != null && WriteData.Length > 0)
            writeBytes = SubCommand == SUBCMD_BIT ? PackBits(WriteData) : PackWords(WriteData);

        if (RawRequestData != null)
        {
            var dataLength = (UInt16)(6 + RawRequestData.Length);
            WriteUInt16LE(ms, dataLength);

            WriteUInt16LE(ms, MonitoringTimer);
            WriteUInt16LE(ms, Command);
            WriteUInt16LE(ms, SubCommand);

            ms.Write(RawRequestData, 0, RawRequestData.Length);
        }
        else
        {
            var dataLength = (UInt16)(12 + (writeBytes?.Length ?? 0));
            WriteUInt16LE(ms, dataLength);

            WriteUInt16LE(ms, MonitoringTimer);
            WriteUInt16LE(ms, Command);
            WriteUInt16LE(ms, SubCommand);

            ms.WriteByte((Byte)(StartAddress & 0xFF));
            ms.WriteByte((Byte)((StartAddress >> 8) & 0xFF));
            ms.WriteByte((Byte)((StartAddress >> 16) & 0xFF));

            ms.WriteByte((Byte)DeviceCode);
            WriteUInt16LE(ms, Count);

            if (writeBytes != null)
                ms.Write(writeBytes, 0, writeBytes.Length);
        }

        // 二进制字节 → ASCII 十六进制字符串 → 写入输出流
        var binary = ms.ToArray();
        var hex = binary.ToHex();
        var hexBytes = System.Text.Encoding.ASCII.GetBytes(hex);
        stream.Write(hexBytes, 0, hexBytes.Length);

        return true;
    }

    /// <summary>序列化为字节数组</summary>
    public Byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        Write(ms, null);
        return ms.ToArray();
    }

    /// <summary>创建对应的响应对象（继承当前数据格式）</summary>
    public MCResponse CreateReply() => new()
    {
        DataFormat = DataFormat,
        NetworkNo = NetworkNo,
        PCNo = PCNo,
        IOUnitNo = IOUnitNo,
        ChannelNo = ChannelNo,
    };

    #endregion

    #region 工厂方法

    /// <summary>构造字软元件批量读取请求</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public static MCMessage BuildReadWord(DeviceCode devCode, Int32 startAddr, Int32 count) => new()
    {
        Command = CMD_READ,
        SubCommand = SUBCMD_WORD,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)count,
    };

    /// <summary>构造位软元件批量读取请求</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public static MCMessage BuildReadBit(DeviceCode devCode, Int32 startAddr, Int32 count) => new()
    {
        Command = CMD_READ,
        SubCommand = SUBCMD_BIT,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)count,
    };

    /// <summary>构造字软元件批量写入请求</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="values">字数据（每个 UInt16）</param>
    public static MCMessage BuildWriteWord(DeviceCode devCode, Int32 startAddr, UInt16[] values) => new()
    {
        Command = CMD_WRITE,
        SubCommand = SUBCMD_WORD,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)values.Length,
        WriteData = values,
    };

    /// <summary>构造位软元件批量写入请求</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="values">位数据，0=OFF，1=ON</param>
    public static MCMessage BuildWriteBit(DeviceCode devCode, Int32 startAddr, UInt16[] values) => new()
    {
        Command = CMD_WRITE,
        SubCommand = SUBCMD_BIT,
        DeviceCode = devCode,
        StartAddress = startAddr,
        Count = (UInt16)values.Length,
        WriteData = values,
    };

    #endregion

    #region 辅助

    /// <summary>将位数据（0/1 数组）打包为字节流（每字节低 4 位 = 第 1 点，高 4 位 = 第 2 点）</summary>
    internal static Byte[] PackBits(UInt16[] values)
    {
        var len = (values.Length + 1) / 2;
        var result = new Byte[len];
        for (var i = 0; i < values.Length; i++)
        {
            var byteIdx = i / 2;
            var shift = (i % 2) * 4;
            result[byteIdx] |= (Byte)((values[i] & 0x0F) << shift);
        }
        return result;
    }

    /// <summary>将字数据打包为字节流（每字 2 字节 LE）</summary>
    internal static Byte[] PackWords(UInt16[] values)
    {
        var result = new Byte[values.Length * 2];
        for (var i = 0; i < values.Length; i++)
        {
            result[i * 2] = (Byte)(values[i] & 0xFF);
            result[i * 2 + 1] = (Byte)(values[i] >> 8);
        }
        return result;
    }

    private static UInt16[] UnpackWriteData(Byte[] buf, UInt16 count, UInt16 subCommand)
    {
        if (subCommand == SUBCMD_BIT)
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

    internal static UInt16 ReadUInt16LE(Stream stream) =>
        (UInt16)(stream.ReadByte() | (stream.ReadByte() << 8));

    internal static void WriteUInt16LE(Stream stream, UInt16 value)
    {
        stream.WriteByte((Byte)(value & 0xFF));
        stream.WriteByte((Byte)(value >> 8));
    }

    #endregion

    #region ASCII 辅助

    /// <summary>从 ASCII 流中读取指定字节数的十六进制数据</summary>
    /// <param name="stream">数据流</param>
    /// <param name="byteCount">期望的二进制字节数</param>
    internal static Byte[] ReadAsciiHex(Stream stream, Int32 byteCount)
    {
        var hexLen = byteCount * 2;
        var hex = new Char[hexLen];
        for (var i = 0; i < hexLen; i++)
        {
            var b = stream.ReadByte();
            if (b < 0) throw new InvalidOperationException("无法读取 ASCII 十六进制数据，流已结束");
            hex[i] = (Char)b;
        }
        return new String(hex).ToHex();
    }

    /// <summary>从 ASCII 流中读取一个字节的十六进制数据</summary>
    internal static Byte ReadAsciiByte(Stream stream)
    {
        var buf = ReadAsciiHex(stream, 1);
        return buf[0];
    }

    /// <summary>从 ASCII 流中读取一个 UInt16 的十六进制数据</summary>
    internal static UInt16 ReadAsciiUInt16(Stream stream)
    {
        var buf = ReadAsciiHex(stream, 2);
        return (UInt16)(buf[0] | (buf[1] << 8));
    }

    #endregion
}
