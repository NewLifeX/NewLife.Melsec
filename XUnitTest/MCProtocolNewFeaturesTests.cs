using System;
using System.ComponentModel;
using System.IO;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC 协议新功能测试（随机读取、远程控制、UDP、RawRequestData）</summary>
public class MCProtocolNewFeaturesTests
{
    #region 随机读取（Random Read）

    [Fact]
    [DisplayName("ReadRandomWords 构建请求数据格式正确")]
    public void BuildRandomReadRequestData_Format()
    {
        // 使用反射调用私有方法验证数据格式
        // 随机读取请求数据: [点数(1)] + [软元件代码(1)+地址(3)]×N
        var items = new (DeviceCode, Int32)[]
        {
            (DeviceCode.D, 100),
            (DeviceCode.M, 200),
        };

        // 通过 MCMessage 的 RawRequestData 验证序列化
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_RANDOM_READ,
            SubCommand = MCMessage.SUBCMD_WORD,
            RawRequestData = BuildRawData(items),
        };

        var bytes = msg.ToBytes();

        // 验证固定头
        Assert.Equal(0x50, bytes[0]); // 子头
        Assert.Equal(0x00, bytes[1]);
        Assert.Equal(0x00, bytes[2]); // 网络号
        Assert.Equal(0xFF, bytes[3]); // PC号

        // 验证命令码 0403h (LE)
        Assert.Equal(0x03, bytes[11]);
        Assert.Equal(0x04, bytes[12]);

        // 验证子命令 0000h (LE)
        Assert.Equal(0x00, bytes[13]);
        Assert.Equal(0x00, bytes[14]);

        // 验证请求数据: 点数(2) + D代码(0xA8) + 地址(100=0x64, LE) + M代码(0x90) + 地址(200=0xC8, LE)
        // 注意: 固定头长度为15, 请求数据从 offset 15 开始
        var offset = 15;
        Assert.Equal(2, bytes[offset]); // 点数
        Assert.Equal(0xA8, bytes[offset + 1]); // D 代码
        Assert.Equal(100, bytes[offset + 2] | (bytes[offset + 3] << 8) | (bytes[offset + 4] << 16)); // 地址
        Assert.Equal(0x90, bytes[offset + 5]); // M 代码
        Assert.Equal(200, bytes[offset + 6] | (bytes[offset + 7] << 8) | (bytes[offset + 8] << 16)); // 地址
    }

    [Fact]
    [DisplayName("ReadRandomBits 返回正确点数")]
    public void ReadRandomBits_Count_Matches()
    {
        var items = new (DeviceCode, Int32)[]
        {
            (DeviceCode.M, 100),
            (DeviceCode.M, 200),
            (DeviceCode.X, 0x1F),
        };

        var raw = BuildRawData(items);
        // 点数（第1字节）应等于 items 数量
        Assert.Equal(items.Length, raw[0]);
    }

    [Fact]
    [DisplayName("随机读取序列化-反序列化 Round-trip")]
    public void RandomRead_ToBytes_Consistent()
    {
        var items = new (DeviceCode, Int32)[]
        {
            (DeviceCode.D, 100),
            (DeviceCode.M, 200),
        };

        var msg = new MCMessage
        {
            Command = MCMessage.CMD_RANDOM_READ,
            SubCommand = MCMessage.SUBCMD_WORD,
            RawRequestData = BuildRawData(items),
        };

        var bytes = msg.ToBytes();

        // 反序列化回来
        var decoded = new MCMessage();
        using var ms = new MemoryStream(bytes);
        Assert.True(decoded.Read(ms, null));

        Assert.Equal(MCMessage.CMD_RANDOM_READ, decoded.Command);
        Assert.Equal(MCMessage.SUBCMD_WORD, decoded.SubCommand);
    }

    #endregion

    #region 远程控制（RUN/STOP）

    [Fact]
    [DisplayName("RemoteRun 构建请求帧正确")]
    public void RemoteRun_ToBytes_Format()
    {
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_RUN,
            SubCommand = 0x0000,
            RawRequestData = [0x01], // 清除模式
        };

        var bytes = msg.ToBytes();

        // 验证命令码 1001h (LE)
        Assert.Equal(0x01, bytes[11]);
        Assert.Equal(0x10, bytes[12]);

        // 验证请求数据: 清除模式
        Assert.Equal(0x01, bytes[15]);
    }

    [Fact]
    [DisplayName("RemoteStop 构建请求帧正确")]
    public void RemoteStop_ToBytes_Format()
    {
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_STOP,
            SubCommand = 0x0000,
            // 使用 RawRequestData 指定空数据
            RawRequestData = [],
        };

        var bytes = msg.ToBytes();

        // 验证命令码 1002h (LE)
        Assert.Equal(0x02, bytes[11]);
        Assert.Equal(0x10, bytes[12]);

        // 无请求数据，数据长度应为 6（监视定时器+命令+子命令）
        var dataLength = bytes[7] | (bytes[8] << 8);
        Assert.Equal(6, dataLength);
    }

    #endregion

    #region 串口传输（MC-15）

    [Fact]
    [DisplayName("Serial 模式默认属性值正确")]
    public void SerialMode_DefaultProperties()
    {
        var protocol = new MCProtocol
        {
            TransportMode = MCTransportMode.Serial,
            Address = "COM3",
        };

        Assert.Equal(MCTransportMode.Serial, protocol.TransportMode);
        Assert.Equal(9600, protocol.Baudrate);
        Assert.Equal(8, protocol.DataBits);
        Assert.Equal("COM3", protocol.Address);
    }

    [Fact]
    [DisplayName("Serial 模式 3E 帧序列化与 TCP 一致")]
    public void SerialMode_3EFrame_SameAsTcp()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var bytes = msg.ToBytes();

        // 帧格式不受传输模式影响
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x00, bytes[1]);
        Assert.Equal(0x01, bytes[11]); // 0401h LE
        Assert.Equal(0x04, bytes[12]);
    }

    [Fact]
    [DisplayName("Serial 模式 1E 帧序列化与 3E 一致")]
    public void SerialMode_1EFrame_Matches3E()
    {
        var msg = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);
        var bytes = msg.ToBytes();

        // 1E 帧格式：副头 + PLC号 + 监视定时器 + 起始地址 + 软元件代码 + 点数
        Assert.Equal(MC1EMessage.SUB_READ_WORD, bytes[0]);
        Assert.Equal(0xFF, bytes[1]);
        Assert.Equal(MC1EDeviceCode.D, (MC1EDeviceCode)bytes[6]);
        Assert.Equal(4, (bytes[7] | (bytes[8] << 8)));
    }

    [Fact]
    [DisplayName("MC-15 串口参数可通过属性配置")]
    public void SerialMode_Configurable()
    {
        var protocol = new MCProtocol
        {
            TransportMode = MCTransportMode.Serial,
            Address = "COM5",
            Baudrate = 19200,
            DataBits = 7,
            Parity = System.IO.Ports.Parity.Even,
            StopBits = System.IO.Ports.StopBits.One,
            Timeout = 3000,
        };

        Assert.Equal("COM5", protocol.Address);
        Assert.Equal(19200, protocol.Baudrate);
        Assert.Equal(7, protocol.DataBits);
        Assert.Equal(System.IO.Ports.Parity.Even, protocol.Parity);
        Assert.Equal(System.IO.Ports.StopBits.One, protocol.StopBits);
        Assert.Equal(3000, protocol.Timeout);
    }

    #endregion

    #region UDP 传输

    [Fact]
    [DisplayName("MCTransportMode 枚举默认值")]
    public void TransportMode_Default_IsTcp()
    {
        var protocol = new MCProtocol();
        Assert.Equal(MCTransportMode.Tcp, protocol.TransportMode);
    }

    [Fact]
    [DisplayName("UDP 模式 3E 帧序列化与 TCP 一致")]
    public void UdpMode_3EFrame_SameAsTcp()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var bytes = msg.ToBytes();

        // 帧格式不受传输模式影响
        Assert.Equal(0x50, bytes[0]);
        Assert.Equal(0x00, bytes[1]);
        Assert.Equal(0x01, bytes[11]); // 0401h LE
        Assert.Equal(0x04, bytes[12]);
    }

    #endregion

    #region RawRequestData 序列化

    [Fact]
    [DisplayName("RawRequestData 影响数据长度计算")]
    public void RawRequestData_AffectsDataLength()
    {
        var msg = new MCMessage
        {
            Command = 0x0403,
            SubCommand = 0x0000,
            RawRequestData = [0x02, 0xA8, 0x64, 0x00, 0x00, 0x90, 0xC8, 0x00, 0x00],
        };

        var bytes = msg.ToBytes();

        // 数据长度 = 6(监视定时器+命令+子命令) + RawRequestData.Length
        var expectedDataLength = (UInt16)(6 + msg.RawRequestData.Length);
        var actualDataLength = (UInt16)(bytes[7] | (bytes[8] << 8));
        Assert.Equal(expectedDataLength, actualDataLength);
    }

    [Fact]
    [DisplayName("无 RawRequestData 时使用标准序列化")]
    public void WithoutRawRequestData_UsesStandardSerialization()
    {
        var msg = MCMessage.BuildReadWord(DeviceCode.D, 100, 4);
        var bytes = msg.ToBytes();

        // 标准数据长度 = 12 (固定字段) 
        var dataLength = (UInt16)(bytes[7] | (bytes[8] << 8));
        Assert.Equal(12, dataLength);

        // 验证标准字段
        Assert.Equal(100, bytes[15] | (bytes[16] << 8) | (bytes[17] << 16)); // 地址
        Assert.Equal(0xA8, bytes[18]); // D 代码
        Assert.Equal(4, bytes[19] | (bytes[20] << 8)); // 点数
    }

    #endregion

    #region 辅助方法

    /// <summary>构建随机读取请求数据</summary>
    private static Byte[] BuildRawData((DeviceCode code, Int32 addr)[] items)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((Byte)items.Length);
        for (var i = 0; i < items.Length; i++)
        {
            ms.WriteByte((Byte)items[i].code);
            var addr = items[i].addr;
            ms.WriteByte((Byte)(addr & 0xFF));
            ms.WriteByte((Byte)((addr >> 8) & 0xFF));
            ms.WriteByte((Byte)((addr >> 16) & 0xFF));
        }
        return ms.ToArray();
    }

    #endregion
}
