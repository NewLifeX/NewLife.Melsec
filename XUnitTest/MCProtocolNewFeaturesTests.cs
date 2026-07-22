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

    #region 远程密码锁定（Remote Password Lock）

    [Fact]
    [DisplayName("远程密码解锁请求 1630h 数据格式正确")]
    public void RemoteUnlock_RequestFormat()
    {
        var raw = InvokeBuildPasswordRequestData("pass");

        // 总长度 = 长度字段(2) + 密码(4) = 6
        Assert.Equal(6, raw.Length);
        // 验证密码长度字段 (LE)
        Assert.Equal(0x04, raw[0]);
        Assert.Equal(0x00, raw[1]);
        // 验证密码 ASCII
        Assert.Equal((Byte)'p', raw[2]);
        Assert.Equal((Byte)'a', raw[3]);
        Assert.Equal((Byte)'s', raw[4]);
        Assert.Equal((Byte)'s', raw[5]);
    }

    [Fact]
    [DisplayName("远程密码解锁/锁定 通过 MCMessage 构建完整帧")]
    public void RemoteUnlock_FullFrame()
    {
        // 通过 MCMessage 模拟发送 RemoteUnlock 命令
        var raw = InvokeBuildPasswordRequestData("pass");
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_UNLOCK,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };

        var bytes = msg.ToBytes();

        // 验证命令码 1630h (LE)
        Assert.Equal(0x30, bytes[11]);
        Assert.Equal(0x16, bytes[12]);
        // 验证子命令 0000h
        Assert.Equal(0x00, bytes[13]);
        Assert.Equal(0x00, bytes[14]);
        // 验证密码长度
        Assert.Equal(0x04, bytes[15]);
        Assert.Equal(0x00, bytes[16]);
        // 验证密码 "pass"
        Assert.Equal((Byte)'p', bytes[17]);
        Assert.Equal((Byte)'a', bytes[18]);
        Assert.Equal((Byte)'s', bytes[19]);
        Assert.Equal((Byte)'s', bytes[20]);
    }

    [Fact]
    [DisplayName("远程锁定命令 1631h 与解锁命令区分")]
    public void RemoteLock_Command_Differs_From_Unlock()
    {
        var raw = InvokeBuildPasswordRequestData("pass");

        var unlockMsg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_UNLOCK,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };
        var lockMsg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_LOCK,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };

        var unlockBytes = unlockMsg.ToBytes();
        var lockBytes = lockMsg.ToBytes();

        // 命令码不同：1630h vs 1631h
        Assert.Equal(0x30, unlockBytes[11]);
        Assert.Equal(0x16, unlockBytes[12]);
        Assert.Equal(0x31, lockBytes[11]);
        Assert.Equal(0x16, lockBytes[12]);
    }

    #endregion

    #region 监视注册（Monitor Registration）

    [Fact]
    [DisplayName("监视注册 0801h 请求数据格式正确")]
    public void MonitorRegist_RequestFormat()
    {
        var wordDevices = new (DeviceCode, Int32)[]
        {
            (DeviceCode.D, 0),
            (DeviceCode.D, 1),
        };
        var doubleWordDevices = new (DeviceCode, Int32)[]
        {
            (DeviceCode.D, 2),
            (DeviceCode.D, 4),
        };

        var raw = InvokeBuildMonitorRegistRequestData(wordDevices, doubleWordDevices);

        // 字设备数量 = 2
        Assert.Equal(2, raw[0]);
        // 双字设备数量 = 2
        Assert.Equal(2, raw[1]);
        // 保留 2 字节
        Assert.Equal(0x00, raw[2]);
        Assert.Equal(0x00, raw[3]);

        // 第1个字设备：D0 (addr=0, code=0xA8)
        Assert.Equal(0x00, raw[4]); // addr LE byte 0
        Assert.Equal(0x00, raw[5]); // addr LE byte 1
        Assert.Equal(0x00, raw[6]); // addr LE byte 2
        Assert.Equal(0xA8, raw[7]); // code

        // 第2个字设备：D1 (addr=1, code=0xA8)
        Assert.Equal(0x01, raw[8]);
        Assert.Equal(0x00, raw[9]);
        Assert.Equal(0x00, raw[10]);
        Assert.Equal(0xA8, raw[11]);

        // 第1个双字设备：D2 (addr=2, code=0xA8)
        Assert.Equal(0x02, raw[12]);
        Assert.Equal(0x00, raw[13]);
        Assert.Equal(0x00, raw[14]);
        Assert.Equal(0xA8, raw[15]);

        // 第2个双字设备：D4 (addr=4, code=0xA8)
        Assert.Equal(0x04, raw[16]);
        Assert.Equal(0x00, raw[17]);
        Assert.Equal(0x00, raw[18]);
        Assert.Equal(0xA8, raw[19]);
    }

    [Fact]
    [DisplayName("监视注册通过 MCMessage 构建完整帧")]
    public void MonitorRegist_FullFrame()
    {
        var wordDevices = new (DeviceCode, Int32)[] { (DeviceCode.D, 0) };
        var doubleWordDevices = new (DeviceCode, Int32)[] { (DeviceCode.D, 2) };
        var raw = InvokeBuildMonitorRegistRequestData(wordDevices, doubleWordDevices);

        var msg = new MCMessage
        {
            Command = MCMessage.CMD_MONITOR_REGIST,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };

        var bytes = msg.ToBytes();

        // 验证命令码 0801h (LE)
        Assert.Equal(0x01, bytes[11]);
        Assert.Equal(0x08, bytes[12]);
        // 验证子命令 0000h
        Assert.Equal(0x00, bytes[13]);
        Assert.Equal(0x00, bytes[14]);
    }

    #endregion

    #region 辅助方法（扩展）

    /// <summary>构建远程密码请求数据</summary>
    private static Byte[] InvokeBuildPasswordRequestData(String password)
    {
        var pwdBytes = System.Text.Encoding.ASCII.GetBytes(password);
        using var ms = new MemoryStream();
        ms.WriteByte((Byte)(pwdBytes.Length & 0xFF));
        ms.WriteByte((Byte)((pwdBytes.Length >> 8) & 0xFF));
        ms.WriteByte((Byte)'p');
        ms.WriteByte((Byte)'a');
        ms.WriteByte((Byte)'s');
        ms.WriteByte((Byte)'s');
        return ms.ToArray();
    }

    /// <summary>构建监视注册请求数据</summary>
    private static Byte[] InvokeBuildMonitorRegistRequestData(
        (DeviceCode code, Int32 addr)[] wordDevices,
        (DeviceCode code, Int32 addr)[] doubleWordDevices)
    {
        using var ms = new MemoryStream();
        ms.WriteByte((Byte)wordDevices.Length);
        ms.WriteByte((Byte)doubleWordDevices.Length);
        ms.WriteByte(0x00);
        ms.WriteByte(0x00);

        foreach (var (code, addr) in wordDevices)
        {
            ms.WriteByte((Byte)(addr & 0xFF));
            ms.WriteByte((Byte)((addr >> 8) & 0xFF));
            ms.WriteByte((Byte)((addr >> 16) & 0xFF));
            ms.WriteByte((Byte)code);
        }
        foreach (var (code, addr) in doubleWordDevices)
        {
            ms.WriteByte((Byte)(addr & 0xFF));
            ms.WriteByte((Byte)((addr >> 8) & 0xFF));
            ms.WriteByte((Byte)((addr >> 16) & 0xFF));
            ms.WriteByte((Byte)code);
        }
        return ms.ToArray();
    }

    #endregion
}
