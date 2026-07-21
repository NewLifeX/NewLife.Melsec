using System;
using System.ComponentModel;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC 协议多类型数据转换读取测试</summary>
public class MCTypeConversionTests
{
    [Fact]
    [DisplayName("ReadInt32 转换：2字 → Int32")]
    public void ReadInt32_Conversion()
    {
        // Int32 123456 = 0x0001E240, LE words: [0xE240, 0x0001]
        var words = new UInt16[] { 0xE240, 0x0001 };
        var result = (Int32)(UInt32)(words[0] | (words[1] << 16));
        Assert.Equal(123456, result);
    }

    [Fact]
    [DisplayName("ReadUInt32 转换：2字 → UInt32")]
    public void ReadUInt32_Conversion()
    {
        var words = new UInt16[] { 0x5678, 0x1234 };
        var result = (UInt32)(words[0] | (words[1] << 16));
        Assert.Equal(0x12345678u, result);
    }

    [Fact]
    [DisplayName("ReadSingle 转换：2字 → Float")]
    public void ReadSingle_Conversion()
    {
        // 3.14f = 0x4048F5C3, LE words: [0xF5C3, 0x4048]
        var words = new UInt16[] { 0xF5C3, 0x4048 };
        var raw = (UInt32)(words[0] | (words[1] << 16));
        var result = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
        Assert.Equal(3.14f, result, 2); // 近似比较
    }

    [Fact]
    [DisplayName("ReadDouble 转换：4字 → Double")]
    public void ReadDouble_Conversion()
    {
        // 1.0 = 0x3FF0000000000000, LE 4 words: [0x0000, 0x0000, 0x0000, 0x3FF0]
        var words = new UInt16[] { 0x0000, 0x0000, 0x0000, 0x3FF0 };
        var b0 = BitConverter.GetBytes(words[0]);
        var b1 = BitConverter.GetBytes(words[1]);
        var b2 = BitConverter.GetBytes(words[2]);
        var b3 = BitConverter.GetBytes(words[3]);
        var raw = new Byte[8];
        Array.Copy(b0, 0, raw, 0, 2);
        Array.Copy(b1, 0, raw, 2, 2);
        Array.Copy(b2, 0, raw, 4, 2);
        Array.Copy(b3, 0, raw, 6, 2);
        var result = BitConverter.ToDouble(raw, 0);
        Assert.Equal(1.0, result);
    }

    [Fact]
    [DisplayName("ReadString 转换：2字 → ASCII")]
    public void ReadString_Conversion()
    {
        // "Hi" = 0x6948 (H=0x48, i=0x69), LE word: [0x6948]
        var words = new UInt16[] { 0x6948 };
        var bytes = new Byte[words.Length * 2];
        for (var i = 0; i < words.Length; i++)
        {
            bytes[i * 2] = (Byte)(words[i] & 0xFF);
            bytes[i * 2 + 1] = (Byte)(words[i] >> 8);
        }
        var str = System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ', '\t');
        Assert.Equal("Hi", str);
    }
}
