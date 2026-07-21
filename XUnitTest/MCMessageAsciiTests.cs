using System;
using System.ComponentModel;
using System.IO;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC协议3E帧 ASCII 模式序列化/反序列化测试</summary>
public class MCMessageAsciiTests
{
    #region BuildReadWord

    [Fact]
    [DisplayName("ASCII 读字请求 D100×4 编码验证")]
    public void BuildReadWord_D100_4Words_Ascii()
    {
        // 先构建二进制模式消息获取二进制字节，再验证 ASCII 模式与之等价
        var binaryMsg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var expectedHexText = binaryMsg.ToBytes().ToHex();

        var asciiMsg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        asciiMsg.DataFormat = MCDataFormat.Ascii;
        var actual = asciiMsg.ToBytes();

        // ASCII 模式下 ToBytes 返回的是 ASCII 十六进制文本的字节
        var actualText = System.Text.Encoding.ASCII.GetString(actual);
        Assert.Equal(expectedHexText, actualText);
    }

    [Fact]
    [DisplayName("ASCII 读字请求 Round-trip")]
    public void BuildReadWord_RoundTrip_Ascii()
    {
        var original = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        original.DataFormat = MCDataFormat.Ascii;
        var bytes = original.ToBytes();

        // 反序列化时使用 ASCII 模式
        var decoded = new MCMessage { DataFormat = MCDataFormat.Ascii };
        var ok = decoded.Read(new MemoryStream(bytes), null);

        Assert.True(ok);
        Assert.Equal(original.Command, decoded.Command);
        Assert.Equal(original.SubCommand, decoded.SubCommand);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
        Assert.Equal(original.NetworkNo, decoded.NetworkNo);
        Assert.Equal(original.PCNo, decoded.PCNo);
    }

    [Fact]
    [DisplayName("ASCII 读字请求属性验证")]
    public void BuildReadWord_Properties_Ascii()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        msg.DataFormat = MCDataFormat.Ascii;

        Assert.Equal(MCMessage.CMD_READ, msg.Command);
        Assert.Equal(MCMessage.SUBCMD_WORD, msg.SubCommand);
        Assert.Equal(DeviceCode.D, msg.DeviceCode);
        Assert.Equal(100, msg.StartAddress);
        Assert.Equal(4, msg.Count);
        Assert.Null(msg.WriteData);
        Assert.Equal(MCDataFormat.Ascii, msg.DataFormat);
    }

    #endregion

    #region BuildReadBit

    [Fact]
    [DisplayName("ASCII 读位请求 M200×8 编码验证")]
    public void BuildReadBit_M200_8Bits_Ascii()
    {
        var binaryMsg = MCMessage.BuildReadBit(DeviceCode.M, 200, 8);
        var expectedHexText = binaryMsg.ToBytes().ToHex();

        var asciiMsg = MCMessage.BuildReadBit(DeviceCode.M, 200, 8);
        asciiMsg.DataFormat = MCDataFormat.Ascii;
        var actual = asciiMsg.ToBytes();

        var actualText = System.Text.Encoding.ASCII.GetString(actual);
        Assert.Equal(expectedHexText, actualText);
    }

    [Fact]
    [DisplayName("ASCII 读位请求 Round-trip")]
    public void BuildReadBit_RoundTrip_Ascii()
    {
        var original = MCMessage.BuildReadBit(DeviceCode.M, 200, 16);
        original.DataFormat = MCDataFormat.Ascii;
        var bytes = original.ToBytes();

        var decoded = new MCMessage { DataFormat = MCDataFormat.Ascii };
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
    [DisplayName("ASCII 写字请求 D100 写入3个字 编码验证")]
    public void BuildWriteWord_D100_3Values_Ascii()
    {
        var binaryMsg = MCMessage.BuildWriteWord(DeviceCode.D, 100, new UInt16[] { 1, 2, 3 });
        var expectedHexText = binaryMsg.ToBytes().ToHex();

        var asciiMsg = MCMessage.BuildWriteWord(DeviceCode.D, 100, new UInt16[] { 1, 2, 3 });
        asciiMsg.DataFormat = MCDataFormat.Ascii;
        var actual = asciiMsg.ToBytes();

        var actualText = System.Text.Encoding.ASCII.GetString(actual);
        Assert.Equal(expectedHexText, actualText);
    }

    [Fact]
    [DisplayName("ASCII 写字请求 Round-trip")]
    public void BuildWriteWord_RoundTrip_Ascii()
    {
        var original = MCMessage.BuildWriteWord(DeviceCode.D, 100, new UInt16[] { 1, 2, 3 });
        original.DataFormat = MCDataFormat.Ascii;
        var bytes = original.ToBytes();

        var decoded = new MCMessage { DataFormat = MCDataFormat.Ascii };
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(original.Command, decoded.Command);
        Assert.Equal(original.SubCommand, decoded.SubCommand);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
        Assert.Equal(original.WriteData, decoded.WriteData);
    }

    #endregion

    #region BuildWriteBit

    [Fact]
    [DisplayName("ASCII 写位请求 编码验证")]
    public void BuildWriteBit_Y0_4Bits_Ascii()
    {
        // Y0=0x9D, addr=0, count=4, values=[1,0,1,0]
        // 二进制写位数据：[0x01, 0x01] （每字节低4位=第1点，高4位=第2点，2点/字节)
        var msg = MCMessage.BuildWriteBit(DeviceCode.Y, 0, new UInt16[] { 1, 0, 1, 0 });
        msg.DataFormat = MCDataFormat.Ascii;
        var bytes = msg.ToBytes();

        // 验证 Round-trip
        var decoded = new MCMessage { DataFormat = MCDataFormat.Ascii };
        Assert.True(decoded.Read(new MemoryStream(bytes), null));

        Assert.Equal(msg.Command, decoded.Command);
        Assert.Equal(msg.SubCommand, decoded.SubCommand);
        Assert.Equal(msg.DeviceCode, decoded.DeviceCode);
        Assert.Equal(msg.Count, decoded.Count);
        Assert.Equal(msg.WriteData, decoded.WriteData);
    }

    #endregion

    #region 边界情况

    [Fact]
    [DisplayName("ASCII 模式非 5000 子头应返回 false")]
    public void Read_InvalidSubHeader_ReturnsFalse_Ascii()
    {
        // 使用错误的子头 "6000"，其他字节用有效帧填充
        var hex = "600000FFFF03000C000A000104000000640000A80400";
        var invalidBytes = System.Text.Encoding.ASCII.GetBytes(hex);

        var msg = new MCMessage { DataFormat = MCDataFormat.Ascii };
        var ok = msg.Read(new MemoryStream(invalidBytes), null);

        Assert.False(ok);
    }

    [Fact]
    [DisplayName("ASCII 与二进制模式产生等价的帧内容")]
    public void AsciiAndBinary_EquivalentContent()
    {
        // ASCII 和二进制模式应该有相同的字段值
        var binaryMsg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var asciiMsg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        asciiMsg.DataFormat = MCDataFormat.Ascii;

        var binaryBytes = binaryMsg.ToBytes();
        var asciiBytes = asciiMsg.ToBytes();

        // ASCII 模式应该比二进制模式字节数多
        Assert.True(asciiBytes.Length > binaryBytes.Length);

        // ASCII 模式输出的是 ASCII 十六进制文本，将文本解码后应与二进制模式一致
        var asciiHexString = System.Text.Encoding.ASCII.GetString(asciiBytes);
        var decodedFromAscii = asciiHexString.ToHex();
        Assert.Equal(binaryBytes, decodedFromAscii);
    }

    #endregion
}
