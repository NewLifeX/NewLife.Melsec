using System;
using System.ComponentModel;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC协议1E帧编解码与驱动测试</summary>
public class MC1ETests
{
    #region MC1EMessage 请求帧编码

    [Fact]
    [DisplayName("1E 读字请求 D100×4 编码")]
    public void BuildReadWord_D100_4Words()
    {
        // A 系列 1E 帧：读 D100 起 4 个字
        // D=0x40, addr=100=0x0064, count=4
        // 副头(0x00) + PLC号(0xFF) + 监视定时器(0A00) + 起始地址(6400) + 软元件代码(40) + 点数(0400)
        var expected = "00-FF-0A-00-64-00-40-04-00".ToHex();

        var msg = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("1E 读字请求字段属性")]
    public void BuildReadWord_Properties()
    {
        var msg = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);

        Assert.Equal(MC1EMessage.SUB_READ_WORD, msg.SubHeader);
        Assert.Equal(MC1EDeviceCode.D, msg.DeviceCode);
        Assert.Equal(100, msg.StartAddress);
        Assert.Equal(4, msg.Count);
        Assert.Null(msg.WriteData);
    }

    [Fact]
    [DisplayName("1E 读字请求 Round-trip")]
    public void BuildReadWord_RoundTrip()
    {
        var original = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);
        var bytes = original.ToBytes();

        var decoded = new MC1EMessage();
        Assert.True(decoded.Read(bytes));

        Assert.Equal(original.SubHeader, decoded.SubHeader);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
        Assert.Equal(original.WriteData, decoded.WriteData);
    }

    [Fact]
    [DisplayName("1E 读位请求 M200×8 编码")]
    public void BuildReadBit_M200_8Bits()
    {
        // M=0x10, addr=200=0x00C8, count=8
        // 副头(0x01=位读) + PLC号(0xFF) + 监视定时器(0A00) + 起始地址(C800) + 软元件代码(10) + 点数(0800)
        var expected = "01-FF-0A-00-C8-00-10-08-00".ToHex();

        var msg = MC1EMessage.BuildReadBit(MC1EDeviceCode.M, 200, 8);
        var actual = msg.ToBytes();

        Assert.Equal(expected.ToHex("-"), actual.ToHex("-"));
    }

    [Fact]
    [DisplayName("1E 读位请求 Round-trip")]
    public void BuildReadBit_RoundTrip()
    {
        var original = MC1EMessage.BuildReadBit(MC1EDeviceCode.M, 200, 16);
        var bytes = original.ToBytes();

        var decoded = new MC1EMessage();
        Assert.True(decoded.Read(bytes));

        Assert.Equal(original.SubHeader, decoded.SubHeader);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.Count, decoded.Count);
    }

    [Fact]
    [DisplayName("1E 写字请求 D100 写入3个字 编码")]
    public void BuildWriteWord_D100_3Values()
    {
        // 副头(0x02=字写) + 数据 [01,00,02,00,03,00]
        var msg = MC1EMessage.BuildWriteWord(MC1EDeviceCode.D, 100, new UInt16[] { 1, 2, 3 });
        var bytes = msg.ToBytes();

        // 验证 Round-trip
        var decoded = new MC1EMessage();
        Assert.True(decoded.Read(bytes));

        Assert.Equal(msg.SubHeader, decoded.SubHeader);
        Assert.Equal(msg.DeviceCode, decoded.DeviceCode);
        Assert.Equal(msg.StartAddress, decoded.StartAddress);
        Assert.Equal(msg.Count, decoded.Count);
        Assert.Equal(msg.WriteData, decoded.WriteData);
    }

    [Fact]
    [DisplayName("1E 写位请求 编码")]
    public void BuildWriteBit_Y0_4Bits()
    {
        var msg = MC1EMessage.BuildWriteBit(MC1EDeviceCode.Y, 0, new UInt16[] { 1, 0, 1, 0 });
        var bytes = msg.ToBytes();

        var decoded = new MC1EMessage();
        Assert.True(decoded.Read(bytes));

        Assert.Equal(msg.SubHeader, decoded.SubHeader);
        Assert.Equal(msg.DeviceCode, decoded.DeviceCode);
        Assert.Equal(msg.Count, decoded.Count);
        Assert.Equal(msg.WriteData, decoded.WriteData);
    }

    #endregion

    #region MC1EResponse 响应解析

    [Fact]
    [DisplayName("1E 读字成功响应解析")]
    public void ReadWordResponse_Success()
    {
        // 响应：副头(0x00) + 结束码(0x00) + 数据(1100 2200 3300 4400)
        var data = "00-00-11-00-22-00-33-00-44-00".ToHex();

        var response = new MC1EResponse();
        Assert.True(response.Read(data));

        Assert.Equal(0x00, response.SubHeader);
        Assert.Equal(0, response.EndCode);
        var words = response.GetWordData();
        Assert.Equal(4, words.Length);
        Assert.Equal((UInt16)0x0011, words[0]);
        Assert.Equal((UInt16)0x0022, words[1]);
        Assert.Equal((UInt16)0x0033, words[2]);
        Assert.Equal((UInt16)0x0044, words[3]);
    }

    [Fact]
    [DisplayName("1E 读位成功响应解析")]
    public void ReadBitResponse_Success()
    {
        // 位响应 M0×8: [ON, OFF, ON, OFF, ON, OFF, ON, OFF]
        // PackBits → [0x01, 0x01, 0x01, 0x01]
        var data = "00-00-01-01-01-01".ToHex();

        var response = new MC1EResponse();
        Assert.True(response.Read(data));

        Assert.Equal(0, response.EndCode);
        var bits = response.GetBitData(8);
        Assert.Equal(8, bits.Length);
        Assert.True(bits[0]);
        Assert.False(bits[1]);
        Assert.True(bits[2]);
        Assert.False(bits[3]);
        Assert.True(bits[4]);
        Assert.False(bits[5]);
        Assert.True(bits[6]);
        Assert.False(bits[7]);
    }

    [Fact]
    [DisplayName("1E 错误响应解析")]
    public void Read_ErrorResponse()
    {
        // 副头 0x80=读异常，结束码 0x50=错误
        var data = "80-50".ToHex();

        var response = new MC1EResponse();
        Assert.True(response.Read(data));

        Assert.Equal(0x80, response.SubHeader);
        Assert.Equal(0x50, response.EndCode);
        Assert.Null(response.RawData);
    }

    [Fact]
    [DisplayName("1E 写入成功响应解析")]
    public void Read_WriteResponse_Success()
    {
        // 副头 0x00=写入成功，结束码 0x00
        var data = "00-00".ToHex();

        var response = new MC1EResponse();
        Assert.True(response.Read(data));

        Assert.Equal(0, response.SubHeader);
        Assert.Equal(0, response.EndCode);
        Assert.Null(response.RawData);
    }

    [Fact]
    [DisplayName("1E 响应 Round-trip")]
    public void WriteRead_RoundTrip()
    {
        var original = new MC1EResponse
        {
            SubHeader = MC1EResponse.SUB_READ_SUCCESS,
            EndCode = 0,
            RawData = new Byte[] { 0x11, 0x00, 0x22, 0x00, 0x33, 0x00, 0x44, 0x00 },
        };

        var bytes = original.ToBytes();
        var decoded = new MC1EResponse();
        Assert.True(decoded.Read(bytes));

        Assert.Equal(original.EndCode, decoded.EndCode);
        Assert.Equal(original.RawData, decoded.RawData);
    }

    #endregion

    #region 设备代码映射

    [Fact]
    [DisplayName("DeviceCode 3E→1E 映射验证")]
    public void DeviceCode_Mapping_3E_to_1E()
    {
        Assert.Equal(MC1EDeviceCode.D, MC1EDeviceCodeHelper.From3E(DeviceCode.D));
        Assert.Equal(MC1EDeviceCode.M, MC1EDeviceCodeHelper.From3E(DeviceCode.M));
        Assert.Equal(MC1EDeviceCode.X, MC1EDeviceCodeHelper.From3E(DeviceCode.X));
        Assert.Equal(MC1EDeviceCode.Y, MC1EDeviceCodeHelper.From3E(DeviceCode.Y));
    }

    [Fact]
    [DisplayName("DeviceCode 1E→3E 映射验证")]
    public void DeviceCode_Mapping_1E_to_3E()
    {
        Assert.Equal(DeviceCode.D, MC1EDeviceCodeHelper.To3E(MC1EDeviceCode.D));
        Assert.Equal(DeviceCode.M, MC1EDeviceCodeHelper.To3E(MC1EDeviceCode.M));
    }

    [Fact]
    [DisplayName("IsBitDevice 判定")]
    public void IsBitDevice_Check()
    {
        Assert.True(MC1EDeviceCodeHelper.IsBitDevice(MC1EDeviceCode.M));
        Assert.True(MC1EDeviceCodeHelper.IsBitDevice(MC1EDeviceCode.X));
        Assert.True(MC1EDeviceCodeHelper.IsBitDevice(MC1EDeviceCode.Y));
        Assert.False(MC1EDeviceCodeHelper.IsBitDevice(MC1EDeviceCode.D));
        Assert.False(MC1EDeviceCodeHelper.IsBitDevice(MC1EDeviceCode.W));
    }

    #endregion

    #region 边界情况

    [Fact]
    [DisplayName("1E 空数据返回 false")]
    public void Read_NullData_ReturnsFalse()
    {
        var msg = new MC1EMessage();
        Assert.False(msg.Read(null));

        var response = new MC1EResponse();
        Assert.False(response.Read(null));
    }

    [Fact]
    [DisplayName("1E 短数据返回 false")]
    public void Read_ShortData_ReturnsFalse()
    {
        var msg = new MC1EMessage();
        Assert.False(msg.Read(new Byte[] { 0x00 })); // 只有1字节，不够最小头长度
    }

    [Fact]
    [DisplayName("1E 地址上限 16 位验证")]
    public void Address_Max16Bit()
    {
        var msg = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 0xFFFF, 1);
        var bytes = msg.ToBytes();

        var decoded = new MC1EMessage();
        Assert.True(decoded.Read(bytes));
        Assert.Equal(0xFFFF, decoded.StartAddress);
    }

    #endregion
}
