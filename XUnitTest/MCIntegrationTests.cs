using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NewLife.IoT.ThingModels;
using NewLife.Melsec.Drivers;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

/// <summary>MC协议端到端集成测试（内嵌模拟 TCP 服务端）</summary>
public class MCIntegrationTests : IDisposable
{
    #region 模拟 TCP 服务端

    private readonly TcpListener _listener;
    private readonly Int32 _port;
    private readonly CancellationTokenSource _cts = new();

    /// <summary>每次收到请求后调用的处理函数，返回响应字节</summary>
    private Func<Byte[], Byte[]> _handler;

    public MCIntegrationTests()
    {
        _listener = new TcpListener(IPAddress.Loopback, 0);
        _listener.Start();
        _port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        StartAcceptLoop();
    }

    /// <summary>设置本次请求的响应处理器</summary>
    private void SetHandler(Func<Byte[], Byte[]> handler) => _handler = handler;

    private void StartAcceptLoop()
    {
        Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync(_cts.Token);
                }
                catch
                {
                    break;
                }

                _ = Task.Run(() => HandleClient(client), _cts.Token);
            }
        }, _cts.Token);
    }

    private void HandleClient(TcpClient client)
    {
        using (client)
        using (var ns = client.GetStream())
        {
            try
            {
                while (client.Connected)
                {
                    // 读请求帧头 9 字节
                    var header = ReadFully(ns, MCResponse.FIXED_HEADER_LEN);
                    if (header == null) break;

                    // 从字节 7~8 读数据长度（实际是请求帧 DataLength）
                    // 请求帧格式 [7~8] = DataLength（监视定时器(2)+命令(2)+子命令(2)+地址(3)+代码(1)+点数(2) = 12 + 写数据）
                    var dataLength = header[7] | (header[8] << 8);
                    var data = ReadFully(ns, dataLength);
                    if (data == null) break;

                    var all = new Byte[header.Length + data.Length];
                    Array.Copy(header, 0, all, 0, header.Length);
                    Array.Copy(data, 0, all, header.Length, data.Length);

                    var handler = _handler;
                    if (handler == null) break;

                    var response = handler(all);
                    if (response == null) break;

                    ns.Write(response, 0, response.Length);
                    ns.Flush();
                }
            }
            catch
            {
                // 连接断开，正常退出
            }
        }
    }

    private static Byte[] ReadFully(Stream stream, Int32 count)
    {
        var buf = new Byte[count];
        var total = 0;
        while (total < count)
        {
            var n = stream.Read(buf, total, count - total);
            if (n <= 0) return null;
            total += n;
        }
        return buf;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _listener.Stop();
    }

    #endregion

    #region MCProtocol 集成测试

    [Fact]
    [DisplayName("ReadWords D100×4 完整请求响应流程")]
    public void ReadWords_D100_4Words_EndToEnd()
    {
        UInt16[] expectedValues = [0x0011, 0x0022, 0x0033, 0x0044];

        SetHandler(request =>
        {
            // 验证请求帧格式
            Assert.Equal(0x50, request[0]);
            Assert.Equal(0x00, request[1]);

            // 返回 4 个字的响应
            return MCResponse.BuildWordResponse(expectedValues).ToBytes();
        });

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        var words = protocol.ReadWords(DeviceCode.D, 100, 4);

        Assert.Equal(4, words.Length);
        Assert.Equal(0x0011, words[0]);
        Assert.Equal(0x0022, words[1]);
        Assert.Equal(0x0033, words[2]);
        Assert.Equal(0x0044, words[3]);
    }

    [Fact]
    [DisplayName("ReadBits M200×8 完整请求响应流程")]
    public void ReadBits_M200_8Bits_EndToEnd()
    {
        Boolean[] expectedBits = [true, false, true, true, false, false, true, false];

        SetHandler(request =>
        {
            Assert.Equal(0x50, request[0]);
            return MCResponse.BuildBitResponse(expectedBits).ToBytes();
        });

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        var bits = protocol.ReadBits(DeviceCode.M, 200, 8);

        Assert.Equal(8, bits.Length);
        Assert.Equal(expectedBits, bits);
    }

    [Fact]
    [DisplayName("WriteWords D100 完整请求响应流程")]
    public void WriteWords_D100_EndToEnd()
    {
        UInt16[] valuesToWrite = [0x1234, 0xABCD];

        SetHandler(request =>
        {
            // 验证是写命令
            // 帧结构: header(9) + MonTimer(2) + Cmd(2) + SubCmd(2) + ...
            // offset 9 = MonTimer, 11 = Cmd low byte
            var cmdLow = request[11];
            var cmdHigh = request[12];
            var cmd = (UInt16)(cmdLow | (cmdHigh << 8));
            Assert.Equal(MCMessage.CMD_WRITE, cmd);

            return MCResponse.BuildWriteResponse().ToBytes();
        });

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        // 不抛出异常即通过
        protocol.WriteWords(DeviceCode.D, 100, valuesToWrite);
    }

    [Fact]
    [DisplayName("WriteBits M0 完整请求响应流程")]
    public void WriteBits_M0_EndToEnd()
    {
        Boolean[] bitsToWrite = [true, false, true];

        SetHandler(request =>
        {
            Assert.Equal(0x50, request[0]);
            return MCResponse.BuildWriteResponse().ToBytes();
        });

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        protocol.WriteBits(DeviceCode.M, 0, bitsToWrite);
    }

    [Fact]
    [DisplayName("服务端返回错误码时 ReadWords 抛出 MCException")]
    public void ReadWords_ErrorResponse_ThrowsMCException()
    {
        SetHandler(_ => MCResponse.BuildErrorResponse(0xC056).ToBytes());

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        var ex = Assert.Throws<MCException>(() => protocol.ReadWords(DeviceCode.D, 100, 4));
        Assert.Equal(0xC056, ex.EndCode);
    }

    [Fact]
    [DisplayName("断线后重连 再次 ReadWords 成功")]
    public void Reconnect_AfterDisconnect_ReadWordsSucceeds()
    {
        var callCount = 0;

        SetHandler(request =>
        {
            callCount++;
            return MCResponse.BuildWordResponse([0x0001]).ToBytes();
        });

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        // 第一次读取
        var words1 = protocol.ReadWords(DeviceCode.D, 0, 1);
        Assert.Equal((UInt16)0x0001, words1[0]);

        // 手动关闭连接模拟断线
        protocol.Close();
        Assert.Equal(1, callCount);

        // 重新打开（模拟重连）
        protocol.Open();

        var words2 = protocol.ReadWords(DeviceCode.D, 0, 1);
        Assert.Equal((UInt16)0x0001, words2[0]);
        Assert.Equal(2, callCount);
    }

    #endregion

    #region MCDriver E2E 测试

    [Fact]
    [DisplayName("MCDriver Read 通过真实 TCP 连接读取字软元件")]
    public void MCDriver_Read_WordDevices_EndToEnd()
    {
        UInt16[] serverValues = [0x0100, 0x0200, 0x0300];

        SetHandler(_ => MCResponse.BuildWordResponse(serverValues).ToBytes());

        var driver = new MCDriver();
        var p = new MCParameter { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        var node = driver.Open(null, p);

        var points = new IPoint[]
        {
            new PointModel { Name = "D100", Address = "D100" },
            new PointModel { Name = "D101", Address = "D101" },
            new PointModel { Name = "D102", Address = "D102" },
        };

        var rs = driver.Read(node, points);

        Assert.Equal(3, rs.Count);
        Assert.Equal((UInt16)0x0100, rs["D100"]);
        Assert.Equal((UInt16)0x0200, rs["D101"]);
        Assert.Equal((UInt16)0x0300, rs["D102"]);

        driver.Close(node);
    }

    [Fact]
    [DisplayName("MCDriver Write 通过真实 TCP 连接写字软元件")]
    public void MCDriver_Write_WordDevice_EndToEnd()
    {
        SetHandler(_ => MCResponse.BuildWriteResponse().ToBytes());

        var driver = new MCDriver();
        var p = new MCParameter { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        var node = driver.Open(null, p);

        var pt = new PointModel { Name = "D100", Address = "D100", Type = "short" };
        var result = driver.Write(node, pt, (Int32)0x1234);

        Assert.Equal((Int32)0x1234, result);

        driver.Close(node);
    }

    [Fact]
    [DisplayName("MCDriver 先读字D段再读位M段 两次请求均成功")]
    public void MCDriver_Read_MixedWordAndBit_EndToEnd()
    {
        // 服务端按请求次序返回不同响应：第1次返回D段字数据，第2次返回M段位数据
        var callCount = 0;
        SetHandler(request =>
        {
            callCount++;
            // 判断子命令区分字/位请求 (offset 13 = SubCmd low byte)
            var subCmd = request[13] | (request[14] << 8);
            if (subCmd == MCMessage.SUBCMD_WORD)
                return MCResponse.BuildWordResponse([0xAAAA, 0xBBBB]).ToBytes();
            else
                return MCResponse.BuildBitResponse([true, false, true]).ToBytes();
        });

        var driver = new MCDriver();
        var p = new MCParameter { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        var node = driver.Open(null, p);

        var points = new IPoint[]
        {
            new PointModel { Name = "D0",  Address = "D0"  },
            new PointModel { Name = "D1",  Address = "D1"  },
            new PointModel { Name = "M0",  Address = "M0"  },
            new PointModel { Name = "M1",  Address = "M1"  },
            new PointModel { Name = "M2",  Address = "M2"  },
        };

        var rs = driver.Read(node, points);

        Assert.Equal(2, callCount);
        Assert.Equal((UInt16)0xAAAA, rs["D0"]);
        Assert.Equal((UInt16)0xBBBB, rs["D1"]);
        Assert.Equal(true,  rs["M0"]);
        Assert.Equal(false, rs["M1"]);
        Assert.Equal(true,  rs["M2"]);

        driver.Close(node);
    }

    [Fact]
    [DisplayName("WriteWords 服务端返回错误码时抛出 MCException")]
    public void MCDriver_WriteWords_ErrorResponse_ThrowsMCException()
    {
        SetHandler(_ => MCResponse.BuildErrorResponse(0xC059).ToBytes());

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        var ex = Assert.Throws<MCException>(() => protocol.WriteWords(DeviceCode.D, 0, [0x1234]));
        Assert.Equal(0xC059, ex.EndCode);
    }

    [Fact]
    [DisplayName("WriteBits 服务端返回错误码时抛出 MCException")]
    public void MCDriver_WriteBits_ErrorResponse_ThrowsMCException()
    {
        SetHandler(_ => MCResponse.BuildErrorResponse(0xC058).ToBytes());

        using var protocol = new MCProtocol { Address = $"127.0.0.1:{_port}", Timeout = 3000 };
        protocol.Open();

        var ex = Assert.Throws<MCException>(() => protocol.WriteBits(DeviceCode.M, 0, [true]));
        Assert.Equal(0xC058, ex.EndCode);
    }

    #endregion
}
