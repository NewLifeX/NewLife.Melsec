using System;
using System.ComponentModel;
using NewLife.IoT.ThingModels;
using NewLife.IoT.ThingSpecification;
using NewLife.Melsec.Drivers;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>测试用子类，暴露 protected ConvertToWords</summary>
internal sealed class TestMCDriver : MCDriver
{
    public UInt16[] ConvertToWordsPublic(Object data, IPoint point, ThingSpec spec)
        => ConvertToWords(data, point, spec);
}

/// <summary>MCDriver.ConvertToWords 数据类型转换测试</summary>
public class MCDriverConvertTests
{
    private readonly TestMCDriver _driver = new();

    private static PointModel WordPoint(String address, String type) =>
        new() { Name = address, Address = address, Type = type };

    #region Bool

    [Fact]
    [DisplayName("ConvertToWords Bool true -> [1]")]
    public void ConvertToWords_Bool_True()
    {
        var result = _driver.ConvertToWordsPublic(true, WordPoint("D0", "bool"), null);
        Assert.Equal([(UInt16)1], result);
    }

    [Fact]
    [DisplayName("ConvertToWords Bool false -> [0]")]
    public void ConvertToWords_Bool_False()
    {
        var result = _driver.ConvertToWordsPublic(false, WordPoint("D0", "bool"), null);
        Assert.Equal([(UInt16)0], result);
    }

    #endregion

    #region Int16 / UInt16

    [Fact]
    [DisplayName("ConvertToWords Int16 0x1234 -> [0x1234]")]
    public void ConvertToWords_Int16()
    {
        var result = _driver.ConvertToWordsPublic((Int32)0x1234, WordPoint("D0", "short"), null);
        Assert.Single(result);
        Assert.Equal((UInt16)0x1234, result[0]);
    }

    [Fact]
    [DisplayName("ConvertToWords UInt16 0xABCD -> [0xABCD]")]
    public void ConvertToWords_UInt16()
    {
        var result = _driver.ConvertToWordsPublic((Int32)0xABCD, WordPoint("D0", "ushort"), null);
        Assert.Single(result);
        Assert.Equal((UInt16)0xABCD, result[0]);
    }

    #endregion

    #region Int32 / UInt32

    [Fact]
    [DisplayName("ConvertToWords Int32 0x12345678 -> LE两字 [Lo, Hi]")]
    public void ConvertToWords_Int32_LittleEndian()
    {
        var result = _driver.ConvertToWordsPublic((Int32)0x12345678, WordPoint("D0", "int"), null);
        Assert.Equal(2, result.Length);
        // MCDriver: low word first
        Assert.Equal((UInt16)0x5678, result[0]);
        Assert.Equal((UInt16)0x1234, result[1]);
    }

    [Fact]
    [DisplayName("ConvertToWords UInt32 0x00010002 -> [0x0002, 0x0001]")]
    public void ConvertToWords_UInt32()
    {
        var result = _driver.ConvertToWordsPublic((Int32)0x00010002, WordPoint("D0", "uint"), null);
        Assert.Equal(2, result.Length);
        Assert.Equal((UInt16)0x0002, result[0]);
        Assert.Equal((UInt16)0x0001, result[1]);
    }

    #endregion

    #region Int64 / UInt64

    [Fact]
    [DisplayName("ConvertToWords Int64 4个字 LE排列")]
    public void ConvertToWords_Int64()
    {
        var val = 0x0001_0002_0003_0004L;
        var result = _driver.ConvertToWordsPublic(val, WordPoint("D0", "long"), null);
        Assert.Equal(4, result.Length);
        Assert.Equal((UInt16)0x0004, result[0]);
        Assert.Equal((UInt16)0x0003, result[1]);
        Assert.Equal((UInt16)0x0002, result[2]);
        Assert.Equal((UInt16)0x0001, result[3]);
    }

    #endregion

    #region Single

    [Fact]
    [DisplayName("ConvertToWords Single 1.0f -> 2字 IEEE 754 LE")]
    public void ConvertToWords_Single_One()
    {
        // 1.0f IEEE754 = 0x3F800000; LE: [0x0000, 0x3F80]
        var bytes = BitConverter.GetBytes(1.0f);
        var expected0 = (UInt16)(bytes[0] | (bytes[1] << 8));
        var expected1 = (UInt16)(bytes[2] | (bytes[3] << 8));

        var result = _driver.ConvertToWordsPublic(1.0f, WordPoint("D0", "float"), null);
        Assert.Equal(2, result.Length);
        Assert.Equal(expected0, result[0]);
        Assert.Equal(expected1, result[1]);
    }

    #endregion

    #region Double

    [Fact]
    [DisplayName("ConvertToWords Double 1.0 -> 4字 IEEE 754 LE")]
    public void ConvertToWords_Double_One()
    {
        var bytes = BitConverter.GetBytes(1.0);
        var e0 = (UInt16)(bytes[0] | (bytes[1] << 8));
        var e1 = (UInt16)(bytes[2] | (bytes[3] << 8));
        var e2 = (UInt16)(bytes[4] | (bytes[5] << 8));
        var e3 = (UInt16)(bytes[6] | (bytes[7] << 8));

        var result = _driver.ConvertToWordsPublic(1.0, WordPoint("D0", "double"), null);
        Assert.Equal(4, result.Length);
        Assert.Equal(e0, result[0]);
        Assert.Equal(e1, result[1]);
        Assert.Equal(e2, result[2]);
        Assert.Equal(e3, result[3]);
    }

    #endregion
}
