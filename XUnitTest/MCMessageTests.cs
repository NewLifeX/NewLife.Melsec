using System;
using System.ComponentModel;
using System.IO;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC协议3E帧序列化/反序列化测试</summary>
public class MCMessageTests
{
    #region BuildReadWord

    [Fact]
    [DisplayName("读字请求 D100×4 编码应匹配架构文档示例")]
    public void BuildReadWord_D100_4Words_MatchesDocExample()
    {
        // 架构文档示例：读 D100 起 4 个字
        // 50 00 00 FF FF 03 00 0C 00 0A 00 01 04 00 00 64 00 00 A8 04 00
        var expected = "50-00-00-FF-FF-03-00-0C-00-0A-00-01-04-00-00-64-00-00-A8-04-00".ToHex();

        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("读字请求字段属性验证")]
    public void BuildReadWord_Properties()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);

        Assert.Equal(MCMessage.CMD_READ, msg.Command);
        Assert.Equal(MCMessage.SUBCMD_WORD, msg.SubCommand);
        Assert.Equal(DeviceCode.D, msg.DeviceCode);
        Assert.Equal(100, msg.StartAddress);
        Assert.Equal(4, msg.Count);
        Assert.Null(msg.WriteData);
    }

    [Fact]
    [DisplayName("读字请求序列化后再反序列化保持一致 Round-trip")]
    public void BuildReadWord_RoundTrip()
    {
        var original = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var bytes = original.ToBytes();

        var decoded = new MCMessage();
        var ok = decoded.Read(new MemoryStream(bytes), null);

        Assert.True(ok);
        Assert.Equal(original.Command, decoded.Command);
        Assert.Equal(original.SubCommand, decoded.SubCommand);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
        Assert.Equal(original.NetworkNo, decoded.NetworkNo);
        Assert.Equal(original.PCNo, decoded.PCNo);
        Assert.Equal(original.MonitoringTimer, decoded.MonitoringTimer);
    }

    #endregion

    #region BuildReadBit

    [Fact]
    [DisplayName("读位请求 M200x8 编码验证")]
    public void BuildReadBit_M200_8Bits()
    {
        // M=0x90, addr=200=0xC8, count=8, subCmd=0x0001(bit mode)
        // DataLength=12(no write data), 200=0xC8 LE 3bytes: C8 00 00
        var expected = "50-00-00-FF-FF-03-00-0C-00-0A-00-01-04-01-00-C8-00-00-90-08-00".ToHex();

        var msg = MCMessage.BuildReadBit(DeviceCode.M, 200, 8);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("读位请求字段属性验证")]
    public void BuildReadBit_Properties()
    {
        var msg = MCMessage.BuildReadBit(DeviceCode.X, 0x1F, 16);

        Assert.Equal(MCMessage.CMD_READ, msg.Command);
        Assert.Equal(MCMessage.SUBCMD_BIT, msg.SubCommand);
        Assert.Equal(DeviceCode.X, msg.DeviceCode);
        Assert.Equal(0x1F, msg.StartAddress);
        Assert.Equal(16, msg.Count);
    }

    [Fact]
    [DisplayName("读位请求 Round-trip")]
    public void BuildReadBit_RoundTrip()
    {
        var original = MCMessage.BuildReadBit(DeviceCode.M, 200, 16);
        var bytes = original.ToBytes();

        var decoded = new MCMessage();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(original.Command, decoded.Command);
        Assert.Equal(original.SubCommand, decoded.SubCommand);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
    }

    #endregion

    #region BuildWriteWord

    [Fact]
    [DisplayName("写字请求 D100 写入3个字 编码验证")]
    public void BuildWriteWord_D100_3Values()
    {
        // Command=0x1401 LE=01 14, SubCmd=0x0000, WriteData=[1,2,3] -> 01 00 02 00 03 00
        // DataLength = 12 + 6 = 18 = 0x12
        var expected = "50-00-00-FF-FF-03-00-12-00-0A-00-01-14-00-00-64-00-00-A8-03-00-01-00-02-00-03-00".ToHex();

        var msg = MCMessage.BuildWriteWord(DeviceCode.D, 100, [0x0001, 0x0002, 0x0003]);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("写字请求 Round-trip 包含写入数据")]
    public void BuildWriteWord_RoundTrip()
    {
        UInt16[] values = [0x1234, 0xABCD, 0x0000, 0xFFFF];
        var original = MCMessage.BuildWriteWord(DeviceCode.D, 500, values);
        var bytes = original.ToBytes();

        var decoded = new MCMessage();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(MCMessage.CMD_WRITE, decoded.Command);
        Assert.Equal(MCMessage.SUBCMD_WORD, decoded.SubCommand);
        Assert.Equal(DeviceCode.D, decoded.DeviceCode);
        Assert.Equal(500, decoded.StartAddress);
        Assert.Equal((UInt16)values.Length, decoded.Count);
        Assert.NotNull(decoded.WriteData);
        Assert.Equal(values, decoded.WriteData);
    }

    #endregion

    #region BuildWriteBit

    [Fact]
    [DisplayName("写位请求 M0 写入4位 位打包编码验证")]
    public void BuildWriteBit_M0_4Bits()
    {
        // PackBits([1,0,1,0]):
        //   byte[0] = (1<<0)|(0<<4) = 0x01
        //   byte[1] = (1<<0)|(0<<4) = 0x01
        // DataLength = 12 + 2 = 14 = 0x0E
        var expected = "50-00-00-FF-FF-03-00-0E-00-0A-00-01-14-01-00-00-00-00-90-04-00-01-01".ToHex();

        var msg = MCMessage.BuildWriteBit(DeviceCode.M, 0, [1, 0, 1, 0]);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("写位请求 Round-trip 包含写入数据")]
    public void BuildWriteBit_RoundTrip()
    {
        UInt16[] values = [1, 0, 1, 1, 0, 0];
        var original = MCMessage.BuildWriteBit(DeviceCode.M, 100, values);
        var bytes = original.ToBytes();

        var decoded = new MCMessage();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(MCMessage.CMD_WRITE, decoded.Command);
        Assert.Equal(MCMessage.SUBCMD_BIT, decoded.SubCommand);
        Assert.Equal(DeviceCode.M, decoded.DeviceCode);
        Assert.Equal(100, decoded.StartAddress);
        Assert.Equal((UInt16)values.Length, decoded.Count);
        Assert.NotNull(decoded.WriteData);
        Assert.Equal(values, decoded.WriteData);
    }

    #endregion

    #region PackBits / PackWords

    [Fact]
    [DisplayName("PackBits 偶数个点 低nibble=第1点 高nibble=第2点")]
    public void PackBits_EvenCount()
    {
        // [1,0,1,1]: byte[0]=(1|0)=0x01, byte[1]=(1|(1<<4))=0x11
        var result = MCMessage.PackBits([1, 0, 1, 1]);
        Assert.Equal([0x01, 0x11], result);
    }

    [Fact]
    [DisplayName("PackBits 奇数个点 最后字节高nibble补零")]
    public void PackBits_OddCount()
    {
        // [1,0,1]: byte[0]=0x01, byte[1]=0x01(只有低nibble)
        var result = MCMessage.PackBits([1, 0, 1]);
        Assert.Equal([0x01, 0x01], result);
    }

    [Fact]
    [DisplayName("PackWords 每个字2字节LE排列")]
    public void PackWords_MultipleValues()
    {
        var result = MCMessage.PackWords([0x0011, 0x0022, 0x1234]);
        Assert.Equal([0x11, 0x00, 0x22, 0x00, 0x34, 0x12], result);
    }

    #endregion

    #region ToString

    [Fact]
    [DisplayName("ToString 读字请求友好字符串")]
    public void ToString_ReadWord()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var s = msg.ToString();
        Assert.Contains("Read", s);
        Assert.Contains("Word", s);
        Assert.Contains("D", s);
    }

    [Fact]
    [DisplayName("ToString 写位请求友好字符串")]
    public void ToString_WriteBit()
    {
        var msg = MCMessage.BuildWriteBit(DeviceCode.M, 0, [1, 0]);
        var s = msg.ToString();
        Assert.Contains("Write", s);
        Assert.Contains("Bit", s);
    }

    #endregion

    #region MCResponse

    [Fact]
    [DisplayName("响应帧 字数据解析 应返回正确字数组")]
    public void MCResponse_ReadWordResponse_MatchesDocExample()
    {
        // 架构文档响应示例: D100-D103 = 0x0011, 0x0022, 0x0033, 0x0044
        // D0 00 00 FF FF 03 00 0A 00 00 00 11 00 22 00 33 00 44 00
        var responseBytes = "D0-00-00-FF-FF-03-00-0A-00-00-00-11-00-22-00-33-00-44-00".ToHex();

        var rs = new MCResponse();
        Assert.True(rs.Read(new MemoryStream(responseBytes), null));

        Assert.Equal(0x0000, rs.EndCode);
        Assert.Equal(0x00, rs.NetworkNo);
        Assert.Equal(0xFF, rs.PCNo);

        var words = rs.GetWordData();
        Assert.Equal(4, words.Length);
        Assert.Equal((UInt16)0x0011, words[0]);
        Assert.Equal((UInt16)0x0022, words[1]);
        Assert.Equal((UInt16)0x0033, words[2]);
        Assert.Equal((UInt16)0x0044, words[3]);
    }

    [Fact]
    [DisplayName("响应帧 位数据解析 ON/OFF 顺序正确")]
    public void MCResponse_GetBitData_CorrectOrder()
    {
        // [true,false,true,true] PackBits -> [0x01, 0x11]
        var rs = MCResponse.BuildBitResponse([true, false, true, true]);

        var bits = rs.GetBitData(4);
        Assert.Equal(4, bits.Length);
        Assert.True(bits[0]);
        Assert.False(bits[1]);
        Assert.True(bits[2]);
        Assert.True(bits[3]);
    }

    [Fact]
    [DisplayName("响应帧 错误码非零应反映在 EndCode 属性")]
    public void MCResponse_ErrorCode_ReflectedInEndCode()
    {
        var rs = MCResponse.BuildErrorResponse(0xC056);

        var bytes = rs.ToBytes();
        var decoded = new MCResponse();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal((UInt16)0xC056, decoded.EndCode);
        Assert.Null(decoded.RawData);
        Assert.Contains("ERROR", decoded.ToString());
    }

    [Fact]
    [DisplayName("响应帧 Round-trip 字读响应")]
    public void MCResponse_RoundTrip_WordData()
    {
        UInt16[] values = [0x1234, 0xABCD, 0x0000, 0xFFFF];
        var original = MCResponse.BuildWordResponse(values);
        var bytes = original.ToBytes();

        var decoded = new MCResponse();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(0, decoded.EndCode);
        var words = decoded.GetWordData();
        Assert.Equal(values, words);
    }

    [Fact]
    [DisplayName("响应帧 Round-trip 写入响应无数据域")]
    public void MCResponse_RoundTrip_WriteResponse()
    {
        var original = MCResponse.BuildWriteResponse();
        var bytes = original.ToBytes();

        var decoded = new MCResponse();
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(0, decoded.EndCode);
        Assert.Null(decoded.RawData);
    }

    [Fact]
    [DisplayName("响应帧 子头不匹配时 Read 返回 false")]
    public void MCResponse_Read_BadSubHeader_ReturnsFalse()
    {
        // 用请求帧子头 0x50 代替响应帧子头 0xD0
        var badBytes = "50-00-00-FF-FF-03-00-02-00-00-00".ToHex();
        var rs = new MCResponse();
        Assert.False(rs.Read(new MemoryStream(badBytes), null));
    }

    #endregion

    #region DeviceCodeHelper.ParseAddress

    [Fact]
    [DisplayName("解析 D100 DeviceCode.D 地址100十进制")]
    public void ParseAddress_D100()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("D100");
        Assert.Equal(DeviceCode.D, code);
        Assert.Equal(100, addr);
    }

    [Fact]
    [DisplayName("解析 M200 DeviceCode.M 地址200十进制")]
    public void ParseAddress_M200()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("M200");
        Assert.Equal(DeviceCode.M, code);
        Assert.Equal(200, addr);
    }

    [Fact]
    [DisplayName("解析 X1F DeviceCode.X 地址31十六进制")]
    public void ParseAddress_X1F()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("X1F");
        Assert.Equal(DeviceCode.X, code);
        Assert.Equal(0x1F, addr);
    }

    [Fact]
    [DisplayName("解析 Y2A DeviceCode.Y 地址42十六进制")]
    public void ParseAddress_Y2A()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("Y2A");
        Assert.Equal(DeviceCode.Y, code);
        Assert.Equal(0x2A, addr);
    }

    [Fact]
    [DisplayName("解析 B100 DeviceCode.B 地址256十六进制")]
    public void ParseAddress_B100()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("B100");
        Assert.Equal(DeviceCode.B, code);
        Assert.Equal(0x100, addr);
    }

    [Fact]
    [DisplayName("解析 W20 DeviceCode.W 地址32十六进制")]
    public void ParseAddress_W20()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("W20");
        Assert.Equal(DeviceCode.W, code);
        Assert.Equal(0x20, addr);
    }

    [Fact]
    [DisplayName("解析 TC5 DeviceCode.TC 地址5十进制")]
    public void ParseAddress_TC5()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("TC5");
        Assert.Equal(DeviceCode.TC, code);
        Assert.Equal(5, addr);
    }

    [Fact]
    [DisplayName("解析 ZR1000 DeviceCode.ZR 地址1000十进制")]
    public void ParseAddress_ZR1000()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("ZR1000");
        Assert.Equal(DeviceCode.ZR, code);
        Assert.Equal(1000, addr);
    }

    [Fact]
    [DisplayName("解析 SM10 DeviceCode.SM 地址10十进制")]
    public void ParseAddress_SM10()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("SM10");
        Assert.Equal(DeviceCode.SM, code);
        Assert.Equal(10, addr);
    }

    [Fact]
    [DisplayName("解析小写地址 d100:0 忽略位域和大小写")]
    public void ParseAddress_LowerCaseWithBitSuffix()
    {
        var (code, addr) = DeviceCodeHelper.ParseAddress("d100:0");
        Assert.Equal(DeviceCode.D, code);
        Assert.Equal(100, addr);
    }

    [Fact]
    [DisplayName("解析不支持的地址格式 抛出 NotSupportedException")]
    public void ParseAddress_InvalidFormat_Throws()
    {
        Assert.Throws<NotSupportedException>(() => DeviceCodeHelper.ParseAddress("Z100"));
    }

    [Fact]
    [DisplayName("解析 null 地址 抛出 ArgumentNullException")]
    public void ParseAddress_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => DeviceCodeHelper.ParseAddress(null));
    }

    #endregion

    #region DeviceCodeHelper.IsBitDevice

    [Fact]
    [DisplayName("M/X/Y/B/TC/CC/SM/DX/DY 是位软元件")]
    public void IsBitDevice_BitDevices()
    {
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.M));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.X));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.Y));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.B));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.TC));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.CC));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.SM));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.DX));
        Assert.True(DeviceCodeHelper.IsBitDevice(DeviceCode.DY));
    }

    [Fact]
    [DisplayName("D/W/R/ZR/TS/CS/SD 是字软元件")]
    public void IsBitDevice_WordDevices()
    {
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.D));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.W));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.R));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.ZR));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.TS));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.CS));
        Assert.False(DeviceCodeHelper.IsBitDevice(DeviceCode.SD));
    }

    #endregion
}
