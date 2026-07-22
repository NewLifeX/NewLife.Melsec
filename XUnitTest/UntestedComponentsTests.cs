using System;
using System.ComponentModel;
using System.IO;
using NewLife;
using NewLife.IoT.Drivers;
using NewLife.Melsec.Drivers;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>对无专用测试的功能点进行补充测试（SYS-3、SYS-4、FXL-9、MC-7、MC-11）</summary>
public class UntestedComponentsTests
{
    #region SYS-3 SerialPortConfig 串口配置持久化

    [Fact]
    [DisplayName("SerialPortConfig 默认值验证")]
    public void SerialPortConfig_DefaultValues()
    {
        var config = new SerialPortConfig();

        Assert.Equal("COM1", config.PortName);
        Assert.Equal(9600, config.BaudRate);
        Assert.Equal(8, config.DataBits);
        Assert.Equal(System.IO.Ports.StopBits.One, config.StopBits);
        Assert.Equal(System.IO.Ports.Parity.None, config.Parity);
        Assert.Equal("utf-8", config.WebEncoding);
        Assert.False(config.HexShow);
        Assert.False(config.HexSend);
        Assert.False(config.DtrEnable);
        Assert.False(config.RtsEnable);
    }

    [Fact]
    [DisplayName("SerialPortConfig 属性设置与读取")]
    public void SerialPortConfig_PropertySetGet()
    {
        var config = new SerialPortConfig
        {
            PortName = "COM3",
            BaudRate = 19200,
            DataBits = 7,
            StopBits = System.IO.Ports.StopBits.Two,
            Parity = System.IO.Ports.Parity.Even,
            WebEncoding = "us-ascii",
            HexShow = true,
            HexSend = true,
            DtrEnable = true,
            RtsEnable = true,
        };

        Assert.Equal("COM3", config.PortName);
        Assert.Equal(19200, config.BaudRate);
        Assert.Equal(7, config.DataBits);
        Assert.Equal(System.IO.Ports.StopBits.Two, config.StopBits);
        Assert.Equal(System.IO.Ports.Parity.Even, config.Parity);
        Assert.Equal("us-ascii", config.WebEncoding);
        Assert.True(config.HexShow);
        Assert.True(config.HexSend);
        Assert.True(config.DtrEnable);
        Assert.True(config.RtsEnable);
    }

    [Fact]
    [DisplayName("SerialPortConfig WebEncoding 双向转换")]
    public void SerialPortConfig_WebEncoding_RoundTrip()
    {
        var config = new SerialPortConfig();

        // UTF-8
        config.WebEncoding = "utf-8";
        Assert.Equal("utf-8", config.WebEncoding);
        Assert.Equal(System.Text.Encoding.UTF8, config.Encoding);

        // US-ASCII
        config.WebEncoding = "us-ascii";
        Assert.Equal("us-ascii", config.WebEncoding);
        Assert.Equal(System.Text.Encoding.ASCII, config.Encoding);

        // 切回 UTF-8
        config.WebEncoding = "utf-8";
        Assert.Equal("utf-8", config.WebEncoding);
    }

    [Fact]
    [DisplayName("SerialPortConfig Extend 默认空串")]
    public void SerialPortConfig_ExtendDefault()
    {
        var config = new SerialPortConfig();
        Assert.Equal("", config.Extend);

        config.Extend = "test data";
        Assert.Equal("test data", config.Extend);
    }

    #endregion

    #region SYS-4 MelsecNode 节点模型

    [Fact]
    [DisplayName("MelsecNode 属性设置与读取")]
    public void MelsecNode_PropertySetGet()
    {
        var node = new MelsecNode
        {
            Address = "192.168.1.10:6000",
            Host = 1,
        };

        Assert.Equal("192.168.1.10:6000", node.Address);
        Assert.Equal(1, node.Host);
        Assert.Null(node.Driver);
        Assert.Null(node.Device);
        Assert.Null(node.Parameter);
    }

    [Fact]
    [DisplayName("MelsecNode IsConnected 始终为 true")]
    public void MelsecNode_IsConnected_AlwaysTrue()
    {
        var node = new MelsecNode();
        Assert.True(node.IsConnected);
    }

    [Fact]
    [DisplayName("MelsecNode 地址支持串口名")]
    public void MelsecNode_Address_SerialPort()
    {
        var node = new MelsecNode { Address = "COM1", Host = 2 };
        Assert.Equal("COM1", node.Address);
        Assert.Equal(2, node.Host);
    }

    [Fact]
    [DisplayName("MelsecNode 默认值")]
    public void MelsecNode_DefaultValues()
    {
        var node = new MelsecNode();
        Assert.Null(node.Address);
        Assert.Equal(0, node.Host);
        Assert.Null(node.Driver);
    }

    #endregion

    #region FXL-9 FxLinksParameter 驱动参数

    [Fact]
    [DisplayName("FxLinksParameter 默认值验证")]
    public void FxLinksParameter_DefaultValues()
    {
        var param = new FxLinksParameter();

        Assert.Null(param.PortName);
        Assert.Equal(9600, param.Baudrate);
        Assert.Equal(7, param.DataBits);
        Assert.Equal(System.IO.Ports.Parity.Even, param.Parity);
        Assert.Equal(System.IO.Ports.StopBits.One, param.StopBits);
        Assert.Equal(0, param.Host);
        Assert.Equal(3000, param.Timeout);
        Assert.Equal(1, param.BatchStep);
        Assert.Equal(0, param.BatchSize);
        Assert.Equal(0, param.BatchDelay);
    }

    [Fact]
    [DisplayName("FxLinksParameter 属性设置与读取")]
    public void FxLinksParameter_PropertySetGet()
    {
        var param = new FxLinksParameter
        {
            PortName = "COM5",
            Baudrate = 19200,
            DataBits = 8,
            Parity = System.IO.Ports.Parity.None,
            StopBits = System.IO.Ports.StopBits.Two,
            Host = 3,
            Timeout = 5000,
            BatchStep = 2,
            BatchSize = 100,
            BatchDelay = 20,
        };

        Assert.Equal("COM5", param.PortName);
        Assert.Equal(19200, param.Baudrate);
        Assert.Equal(8, param.DataBits);
        Assert.Equal(System.IO.Ports.Parity.None, param.Parity);
        Assert.Equal(System.IO.Ports.StopBits.Two, param.StopBits);
        Assert.Equal(3, param.Host);
        Assert.Equal(5000, param.Timeout);
        Assert.Equal(2, param.BatchStep);
        Assert.Equal(100, param.BatchSize);
        Assert.Equal(20, param.BatchDelay);
    }

    [Fact]
    [DisplayName("FxLinksParameter GetKey 返回串口名")]
    public void FxLinksParameter_GetKey()
    {
        var param = new FxLinksParameter { PortName = "COM3" };
        Assert.Equal("COM3", param.GetKey());
    }

    [Fact]
    [DisplayName("FxLinksParameter FxLinks 串口参数符合规范")]
    public void FxLinksParameter_FxLinkDefaults()
    {
        // FxLinks 规范要求：9600, 7, Even, One
        var param = new FxLinksParameter();
        Assert.Equal(9600, param.Baudrate);
        Assert.Equal(7, param.DataBits);
        Assert.Equal(System.IO.Ports.Parity.Even, param.Parity);
        Assert.Equal(System.IO.Ports.StopBits.One, param.StopBits);
    }

    #endregion

    #region MC-7 MCParameter 驱动参数

    [Fact]
    [DisplayName("MCParameter 默认值验证")]
    public void MCParameter_DefaultValues()
    {
        var param = new MCParameter();

        Assert.Null(param.Address);
        Assert.Equal(MCFrameType.Frame3E, param.FrameType);
        Assert.Equal(0, param.NetworkNo);
        Assert.Equal(MCDataFormat.Binary, param.DataFormat);
        Assert.Equal(5000, param.Timeout);
        Assert.Equal(1, param.BatchStep);
        Assert.Equal(0, param.BatchSize);
        Assert.Equal(0, param.BatchDelay);
    }

    [Fact]
    [DisplayName("MCParameter 属性设置与读取")]
    public void MCParameter_PropertySetGet()
    {
        var param = new MCParameter
        {
            Address = "192.168.1.100:6000",
            FrameType = MCFrameType.Frame4E,
            NetworkNo = 1,
            DataFormat = MCDataFormat.Ascii,
            Timeout = 10000,
            BatchStep = 2,
            BatchSize = 200,
            BatchDelay = 50,
        };

        Assert.Equal("192.168.1.100:6000", param.Address);
        Assert.Equal(MCFrameType.Frame4E, param.FrameType);
        Assert.Equal(1, param.NetworkNo);
        Assert.Equal(MCDataFormat.Ascii, param.DataFormat);
        Assert.Equal(10000, param.Timeout);
        Assert.Equal(2, param.BatchStep);
        Assert.Equal(200, param.BatchSize);
        Assert.Equal(50, param.BatchDelay);
    }

    [Fact]
    [DisplayName("MCParameter GetKey 返回地址")]
    public void MCParameter_GetKey()
    {
        var param = new MCParameter { Address = "10.0.0.1:6000" };
        Assert.Equal("10.0.0.1:6000", param.GetKey());
    }

    [Fact]
    [DisplayName("MCParameter 支持三种帧类型")]
    public void MCParameter_FrameTypeEnum()
    {
        // 验证枚举存在且可赋值
        Assert.Equal(0, (Int32)MCFrameType.Frame3E);
        Assert.Equal(1, (Int32)MCFrameType.Frame1E);
        Assert.Equal(2, (Int32)MCFrameType.Frame4E);

        var param = new MCParameter();
        param.FrameType = MCFrameType.Frame3E;
        Assert.Equal(MCFrameType.Frame3E, param.FrameType);

        param.FrameType = MCFrameType.Frame1E;
        Assert.Equal(MCFrameType.Frame1E, param.FrameType);

        param.FrameType = MCFrameType.Frame4E;
        Assert.Equal(MCFrameType.Frame4E, param.FrameType);
    }

    [Fact]
    [DisplayName("MCParameter Binary/Ascii 模式切换")]
    public void MCParameter_DataFormat()
    {
        var param = new MCParameter();
        Assert.Equal(MCDataFormat.Binary, param.DataFormat);

        param.DataFormat = MCDataFormat.Ascii;
        Assert.Equal(MCDataFormat.Ascii, param.DataFormat);
    }

    #endregion

    #region MC-11 MCDriver 1E 模式切换

    [Fact]
    [DisplayName("MCFrameTypeHelper.Is1E 正确判断 1E 帧")]
    public void MCFrameTypeHelper_Is1E()
    {
        Assert.False(MCFrameType.Frame3E.Is1E());
        Assert.True(MCFrameType.Frame1E.Is1E());
        Assert.False(MCFrameType.Frame4E.Is1E());
    }

    [Fact]
    [DisplayName("MCFrameTypeHelper.Is4E 正确判断 4E 帧")]
    public void MCFrameTypeHelper_Is4E()
    {
        Assert.False(MCFrameType.Frame3E.Is4E());
        Assert.False(MCFrameType.Frame1E.Is4E());
        Assert.True(MCFrameType.Frame4E.Is4E());
    }

    [Fact]
    [DisplayName("MCFrameTypeHelper.Is3EOr4E 正确判断 3E/4E 帧")]
    public void MCFrameTypeHelper_Is3EOr4E()
    {
        Assert.True(MCFrameType.Frame3E.Is3EOr4E());
        Assert.False(MCFrameType.Frame1E.Is3EOr4E());
        Assert.True(MCFrameType.Frame4E.Is3EOr4E());
    }

    [Fact]
    [DisplayName("MC1EMessage 读字请求 BuildReadWord 格式正确")]
    public void MC1EMessage_BuildReadWord_Format()
    {
        // 1E 帧字读请求：副头=0x00 + PLC号=0xFF + 定时器 + 起始地址 + 代码 + 点数
        var msg = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);
        var bytes = msg.ToBytes();

        // 固定头长度 = 9 字节
        Assert.Equal(9, bytes.Length);
        Assert.Equal(0x00, bytes[0]); // 字读副头
        Assert.Equal(0xFF, bytes[1]); // PLC号
        Assert.Equal(4, msg.Count);
    }

    [Fact]
    [DisplayName("MC1EMessage 读位请求 BuildReadBit 副头正确")]
    public void MC1EMessage_BuildReadBit_SubHeader()
    {
        var msg = MC1EMessage.BuildReadBit(MC1EDeviceCode.M, 200, 8);
        Assert.Equal(MC1EMessage.SUB_READ_BIT, msg.SubHeader);
        Assert.Equal(MC1EDeviceCode.M, msg.DeviceCode);
        Assert.Equal(200, msg.StartAddress);
        Assert.Equal(8, msg.Count);
    }

    [Fact]
    [DisplayName("MC1EMessage 写字请求 BuildWriteWord 格式正确")]
    public void MC1EMessage_BuildWriteWord_Format()
    {
        var values = new UInt16[] { 0x1234, 0x5678 };
        var msg = MC1EMessage.BuildWriteWord(MC1EDeviceCode.D, 100, values);
        var bytes = msg.ToBytes();

        // 固定头 9 字节 + 数据 4 字节 = 13 字节
        Assert.Equal(13, bytes.Length);
        Assert.Equal(MC1EMessage.SUB_WRITE_WORD, msg.SubHeader);
        Assert.Equal(0xFF, bytes[1]);
        Assert.Equal(2, msg.Count);
    }

    [Fact]
    [DisplayName("MC1EMessage 序列化/反序列化 Round-trip")]
    public void MC1EMessage_RoundTrip()
    {
        var original = MC1EMessage.BuildReadWord(MC1EDeviceCode.D, 100, 4);
        var bytes = original.ToBytes();

        var decoded = new MC1EMessage();
        var ok = decoded.Read(bytes);

        Assert.True(ok);
        Assert.Equal(original.SubHeader, decoded.SubHeader);
        Assert.Equal(original.PCNo, decoded.PCNo);
        Assert.Equal(original.StartAddress, decoded.StartAddress);
        Assert.Equal(original.DeviceCode, decoded.DeviceCode);
        Assert.Equal(original.Count, decoded.Count);
    }

    [Fact]
    [DisplayName("MC1EResponse 字读响应解析正确")]
    public void MC1EResponse_ReadWordResponse()
    {
        // 1E 响应：副头(1) + 结束码(1) + 数据(N)
        // D100=0x1234, D101=0x5678
        var responseData = new Byte[] { 0xD0, 0x00, 0x34, 0x12, 0x78, 0x56 };
        var response = new MC1EResponse();
        var ok = response.Read(responseData);

        Assert.True(ok);
        Assert.Equal(0xD0, response.SubHeader);
        Assert.Equal(0, response.EndCode);
        Assert.NotNull(response.RawData);
        Assert.Equal(4, response.RawData.Length);
    }

    [Fact]
    [DisplayName("MC1EResponse 错误响应解析")]
    public void MC1EResponse_ErrorResponse()
    {
        // 结束码非零
        var responseData = new Byte[] { 0xD0, 0x52 }; // 0x52 = 错误
        var response = new MC1EResponse();
        var ok = response.Read(responseData);

        Assert.True(ok);
        Assert.Equal(0x52, response.EndCode);
        Assert.Null(response.RawData);
    }

    #endregion
}
