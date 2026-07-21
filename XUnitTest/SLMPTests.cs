using System;
using System.ComponentModel;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>SLMP 协议兼容性测试</summary>
public class SLMPTests
{
    [Fact]
    [DisplayName("SLMP 读字请求与 MC 3E 二进制帧一致")]
    public void ReadWord_Matches_MC3E()
    {
        // SLMP 3C 帧应与 MC 3E 二进制帧产生完全相同的字节
        var slmp = SLMPMessage.BuildReadWord(SLMPDeviceCode.D, 100, 4);
        var mc3e = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);

        var slmpBytes = slmp.ToBytes();
        var mc3eBytes = mc3e.ToBytes();

        Assert.Equal(mc3eBytes, slmpBytes);
    }

    [Fact]
    [DisplayName("SLMP 读位请求 Round-trip")]
    public void ReadBit_RoundTrip()
    {
        var original = SLMPMessage.BuildReadBit(SLMPDeviceCode.M, 200, 8);
        var bytes = original.ToBytes();

        var decoded = new SLMPMessage();
        Assert.True(decoded.Read(bytes));
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
    }

    [Fact]
    [DisplayName("SLMP 写字请求 Round-trip")]
    public void WriteWord_RoundTrip()
    {
        var original = SLMPMessage.BuildWriteWord(SLMPDeviceCode.D, 100, new UInt16[] { 1, 2, 3 });
        var bytes = original.ToBytes();

        var decoded = new SLMPMessage();
        Assert.True(decoded.Read(bytes));
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
        Assert.Equal(original.WriteData, decoded.WriteData);
    }

    [Fact]
    [DisplayName("SLMP 读字响应解析")]
    public void ReadWordResponse()
    {
        var response = SLMPResponse.BuildWordResponse(new UInt16[] { 0x0011, 0x0022, 0x0033, 0x0044 });
        Assert.Equal(0, response.EndCode);
        var words = response.GetWordData();
        Assert.Equal(4, words.Length);
        Assert.Equal((UInt16)0x0011, words[0]);
    }

    [Fact]
    [DisplayName("SLMP 协议码与 MC 3E 兼容")]
    public void DeviceCode_Compatible()
    {
        Assert.Equal((Byte)SLMPDeviceCode.D, (Byte)DeviceCode.D);
        Assert.Equal((Byte)SLMPDeviceCode.M, (Byte)DeviceCode.M);
        Assert.Equal((Byte)SLMPDeviceCode.X, (Byte)DeviceCode.X);
        Assert.Equal((Byte)SLMPDeviceCode.Y, (Byte)DeviceCode.Y);

        // 转换验证
        Assert.Equal(SLMPDeviceCode.D, SLMPDeviceCodeHelper.FromMC3E(DeviceCode.D));
        Assert.Equal(DeviceCode.D, SLMPDeviceCodeHelper.ToMC3E(SLMPDeviceCode.D));
    }
}
