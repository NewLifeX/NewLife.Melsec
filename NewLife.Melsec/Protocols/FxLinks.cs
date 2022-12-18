using System.Diagnostics;
using System.IO.Ports;
using System.Runtime.CompilerServices;
using System.Text;
using NewLife.Data;
using NewLife.Log;

[assembly: InternalsVisibleTo("XUnitTest, PublicKey=00240000048000001401000006020000002400005253413100080000010001000d41eb3bdab5c2150958b46c95632b7e4dcb0af77ed8637bd8543875bc2443d01273143bb46655a48a92efa76251adc63ccca6d0e9cef2e0ce93e32b5043bea179a6c710981be4a71703a03e10960643f7df091f499cf60183ef0e4e4e2eebf26e25cea0eebf87c8a6d7f8130c283fc3f747cb90623f0aaa619825e3fcd82f267a0f4bfd26c9f2a6b5a62a6b180b4f6d1d091fce6bd60a9aa9aa5b815b833b44e0f2e58b28a354cb20f52f31bb3b3a7c54f515426537e41f9c20c07e51f9cab8abc311daac19a41bd473a51c7386f014edf1863901a5c29addc89da2f2659c9c1e95affd6997396b9680e317c493e974a813186da277ff9c1d1b30e33cb5a2f6")]

namespace NewLife.Melsec.Protocols;

/// <summary>三菱PLC计算机链路协议</summary>
public class FxLinks : DisposeBase
{
    #region 属性
    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>端口</summary>
    public String PortName { get; set; }

    /// <summary>波特率</summary>
    public Int32 Baudrate { get; set; } = 9600;

    /// <summary>数据位长度。默认7</summary>
    public Int32 DataBits { get; set; } = 7;

    /// <summary>奇偶校验位。默认Even偶校验</summary>
    public Parity Parity { get; set; } = Parity.Even;

    /// <summary>停止位。默认One</summary>
    public StopBits StopBits { get; set; } = StopBits.One;

    /// <summary>缓冲区大小。默认256</summary>
    public Int32 BufferSize { get; set; } = 256;

    /// <summary>网络超时。发起请求后等待响应的超时时间，默认3000ms</summary>
    public Int32 Timeout { get; set; } = 3000;

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    private SerialPort _port;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public FxLinks() => Name = GetType().Name;

    /// <summary>销毁</summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        Close();
    }
    #endregion

    #region 方法
    /// <summary>打开连接</summary>
    public void Open()
    {
        if (_port == null)
        {
            var p = new SerialPort(PortName, Baudrate)
            {
                DataBits = DataBits,
                Parity = Parity,
                StopBits = StopBits,

                ReadTimeout = Timeout,
                WriteTimeout = Timeout
            };
            //if (DataBits > 0) p.DataBits = DataBits;
            //if (Parity > 0) p.Parity = Parity;
            //if (StopBits > 0) p.StopBits = StopBits;
            p.Open();
            _port = p;

            WriteLog("FxLinks.Open {0} Baudrate={1} DataBits={2} Parity={3} StopBits={4}", PortName, Baudrate, p.DataBits, p.Parity, p.StopBits);
        }
    }

    /// <summary>关闭连接</summary>
    public void Close()
    {
        _port.TryDispose();
        _port = null;
    }

    /// <summary>发送命令，并接收返回</summary>
    /// <param name="command">功能码</param>
    /// <param name="host">主机。一般是1</param>
    /// <param name="address">地址。例如0x0002</param>
    /// <param name="data">数据</param>
    /// <returns>返回响应消息的负载部分</returns>
    public virtual FxLinksResponse SendCommand(String command, Byte host, String address, String data)
    {
        var msg = new FxLinksMessage
        {
            Code = ControlCodes.ENQ,
            Station = host,
            PLC = 0xFF,
            Command = command,
            Address = address,

            Payload = data
        };

        var rs = SendCommand(msg);

        return rs;
    }

    /// <summary>发送消息并接收返回</summary>
    /// <param name="message">Modbus消息</param>
    /// <returns></returns>
    internal protected virtual FxLinksResponse SendCommand(FxLinksMessage message)
    {
        Open();

        // 清空缓冲区
        _port.DiscardInBuffer();

        Log?.Debug("=> {0}", message);

        var cmd = message.ToPacket();
        var buf = cmd.ToArray();

        using var span = Tracer?.NewSpan("fxlinks:SendCommand", buf.ToHex("-"));

        Log?.Debug("{0}=> {1} ({2})", PortName, buf.ToHex("-"), FxLinksMessage.GetHex(buf));

        _port.Write(buf, 0, buf.Length);

        //Thread.Sleep(ByteTimeout);

        // 串口速度较慢，等待收完数据
        WaitMore(_port, 1 + 1 + 2);

        //using var span = Tracer?.NewSpan("fxlinks:ReceiveCommand");
        buf = new Byte[BufferSize];
        try
        {
            var count = _port.Read(buf, 0, buf.Length);
            var pk = new Packet(buf, 0, count);
            Log?.Debug("{0}<= {1} ({2})", PortName, pk.ToHex(32, "-"), FxLinksMessage.GetHex(pk.ReadBytes()));

            if (span != null) span.Tag += Environment.NewLine + pk.ToHex(64, "-");

            var len = pk.Total - 2;
            if (len < 2) return null;

            var rs = message.CreateReply();
            if (!rs.Read(pk.GetStream(), message)) return null;

            // 校验
            if (rs.CheckSum != rs.CheckSum2) WriteLog("CheckSum Error {0:X2}!={1:X2} !", rs.CheckSum, rs.CheckSum2);

            Log?.Debug("<= {0}", rs);

            // 检查功能码
            if (rs.Code == ControlCodes.NAK)
            {
                var str = rs.Payload;
                var code = str != null && str.Length >= 2 ? str.ToByte(0) : 0;
                throw new FxLinksException((ErrorCodes)code, $"{message} occure error");
            }

            return rs;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            if (ex is TimeoutException) return null;
            throw;
        }
    }

    private void WaitMore(SerialPort sp, Int32 minLength)
    {
        var count = sp.BytesToRead;
        if (count >= minLength) return;

        var ms = Timeout;
        var sw = Stopwatch.StartNew();
        while (sp.IsOpen && sw.ElapsedMilliseconds < ms)
        {
            //Thread.SpinWait(1);
            Thread.Sleep(10);
            if (count != sp.BytesToRead)
            {
                count = sp.BytesToRead;
                if (count >= minLength) break;

                sw.Restart();
            }
        }
    }
    #endregion

    #region 读取
    /// <summary>按功能码读取。用于IoT标准库</summary>
    /// <param name="command">功能码</param>
    /// <param name="host">主机</param>
    /// <param name="address">逻辑地址</param>
    /// <param name="count">个数。寄存器个数或线圈个数</param>
    /// <returns></returns>
    /// <exception cref="NotSupportedException"></exception>
    public virtual Object Read(String command, Byte host, String address, Byte count)
    {
        switch (command)
        {
            case "BR": return ReadBit(host, address, count);
            case "WR": return ReadWord(host, address, count);
            default:
                break;
        }

        throw new NotSupportedException($"FxLinksRead不支持[{command}]");
    }

    /// <summary>位单元读取，BR</summary>
    /// <param name="host">主机。一般是1</param>
    /// <param name="address">地址。例如0x0002</param>
    /// <param name="count">线圈数量。一般要求8的倍数</param>
    /// <returns>线圈状态字节数组</returns>
    public virtual Byte[] ReadBit(Byte host, String address, Byte count)
    {
        using var span = Tracer?.NewSpan("fxlinks:ReadBit", $"host={host} address={address} count={count}");
        try
        {
            var rs = SendCommand("BR", host, address, count.ToHexChars());
            if (rs == null || rs.Payload.IsNullOrEmpty()) return null;

            var result = rs.Payload.ToBytes();

            span?.AppendTag(result.Join(","));

            return result;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>字单元读取，WR</summary>
    /// <param name="host">主机。一般是1</param>
    /// <param name="address">地址。例如0x0002</param>
    /// <param name="count">输入数量。一般要求8的倍数</param>
    /// <returns>输入状态字节数组</returns>
    public virtual UInt16[] ReadWord(Byte host, String address, Byte count)
    {
        using var span = Tracer?.NewSpan("fxlinks:ReadWord", $"host={host} address={address} count={count}");
        try
        {
            var rs = SendCommand("WR", host, address, count.ToHexChars());
            if (rs == null || rs.Payload.IsNullOrEmpty()) return null;

            var str = rs.Payload;
            var us = new UInt16[str.Length / 4];
            for (var i = 0; i < us.Length; i++)
            {
                us[i] = str.Substring(i * 4, 4).ToHex().ToUInt16(0, false);
            }

            span?.AppendTag(us);

            return us;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region 写入
    /// <summary>按功能码写入。用于IoT标准库</summary>
    /// <param name="command">功能码</param>
    /// <param name="host">主机</param>
    /// <param name="address">逻辑地址</param>
    /// <param name="values">待写入数值</param>
    /// <returns></returns>
    public virtual Object Write(String command, Byte host, String address, UInt16[] values)
    {
        switch (command)
        {
            case "BW": return WriteBit(host, address, values);
            case "WW": return WriteWord(host, address, values);
            default:
                break;
        }

        throw new NotSupportedException($"FxLinksWrite不支持[{command}]");
    }

    /// <summary>位单元写入，BW</summary>
    /// <param name="host">主机。一般是1</param>
    /// <param name="address">地址。例如0x0002</param>
    /// <param name="values">输出值。一般是 0xFF00/0x0000</param>
    /// <returns>输出值</returns>
    public Int32 WriteBit(Byte host, String address, params UInt16[] values)
    {
        using var span = Tracer?.NewSpan("fxlinks:WriteBit", $"host={host} address={address} value=({values.Join(",")})");
        try
        {
            // 1字节（2字符）的点位数
            // 后续每个点位1个字符
            var sb = new StringBuilder();
            sb.Append(((Byte)values.Length).ToHexChars());
            for (var i = 0; i < values.Length; i++)
            {
                sb.Append(values[i] != 0 ? '1' : '0');
            }

            var rs = SendCommand("BW", host, address, sb.ToString());
            if (rs == null) return -1;
            if (rs.Code == ControlCodes.NAK) throw new Exception($"WriteBit({address}, {values.Join(",")}) get {rs.Code}");
            if (rs.Code != ControlCodes.ACK) return -1;

            return values.Length;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }

    /// <summary>字单元写入，WW</summary>
    /// <param name="host">主机。一般是1</param>
    /// <param name="address">地址。例如0x0002</param>
    /// <param name="values">数值</param>
    /// <returns>寄存器值</returns>
    public Int32 WriteWord(Byte host, String address, params UInt16[] values)
    {
        using var span = Tracer?.NewSpan("fxlinks:WriteWord", $"host={host} address={address} value=({values.Join(",")})");
        try
        {
            // 1字节（2字符）的点位数
            // 后续每个点位4个字符
            var sb = new StringBuilder();
            sb.Append(((Byte)values.Length).ToHexChars());
            for (var i = 0; i < values.Length; i++)
            {
                sb.Append(values[i].ToString("X4"));
            }

            var rs = SendCommand("WW", host, address, sb.ToString());
            if (rs == null) return -1;
            if (rs.Code == ControlCodes.NAK) throw new Exception($"WriteWord({address}, {values.Join(",")}) get {rs.Code}");
            if (rs.Code != ControlCodes.ACK) return -1;

            return values.Length;
        }
        catch (Exception ex)
        {
            span?.SetError(ex, null);
            throw;
        }
    }
    #endregion

    #region 日志
    /// <summary>日志</summary>
    public ILog Log { get; set; }

    /// <summary>写日志</summary>
    /// <param name="format"></param>
    /// <param name="args"></param>
    public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
    #endregion
}