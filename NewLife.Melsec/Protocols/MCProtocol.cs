using System.Net.Sockets;
using NewLife.Log;
using NewLife.Melsec.Protocols;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱MC协议栈（TCP长连接，支持 3E/1E 帧）</summary>
/// <remarks>
/// 负责管理与 PLC 的 TCP 连接，提供字/位软元件的批量读写。
/// 支持 3E 帧（Qna 兼容）和 1E 帧（A 系列兼容）两种帧格式。
/// 3E 帧支持二进制模式和 ASCII 十六进制模式两种数据格式。
/// 连接断开时自动重连；所有操作均加锁保证线程安全。
/// </remarks>
public class MCProtocol : DisposeBase
{
    #region 属性

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>PLC地址。格式：IP:端口，如 192.168.1.10:6000</summary>
    public String Address { get; set; }

    /// <summary>帧类型。3E 帧（默认）或 1E 帧</summary>
    public MCFrameType FrameType { get; set; } = MCFrameType.Frame3E;

    /// <summary>网络号（仅 3E 帧）。通常 0x00</summary>
    public Byte NetworkNo { get; set; } = 0x00;

    /// <summary>PC号（仅 3E 帧）。通常 0xFF</summary>
    public Byte PCNo { get; set; } = 0xFF;

    /// <summary>数据格式（仅 3E 帧）。默认二进制模式，可切换为 ASCII 十六进制模式</summary>
    public MCDataFormat DataFormat { get; set; } = MCDataFormat.Binary;

    /// <summary>网络超时（毫秒）。默认 5000ms</summary>
    public Int32 Timeout { get; set; } = 5000;

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; }

    private TcpClient _client;
    private NetworkStream _stream;
    private readonly Object _lock = new();

    #endregion

    #region 构造

    /// <summary>实例化</summary>
    public MCProtocol() => Name = GetType().Name;

    /// <summary>销毁</summary>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        Close();
    }

    #endregion

    #region 连接管理

    /// <summary>打开TCP连接</summary>
    public void Open()
    {
        if (_client?.Connected == true) return;

        var addr = Address ?? throw new InvalidOperationException("Address 未设置");
        var colonIdx = addr.LastIndexOf(':');
        var host = colonIdx > 0 ? addr[..colonIdx] : addr;
        var port = colonIdx > 0 ? Int32.Parse(addr[(colonIdx + 1)..]) : 6000;

        var client = new TcpClient
        {
            ReceiveTimeout = Timeout,
            SendTimeout = Timeout,
        };
        client.Connect(host, port);
        _client = client;
        _stream = client.GetStream();

        WriteLog("MCProtocol.Open {0}", Address);
    }

    /// <summary>关闭TCP连接</summary>
    public void Close()
    {
        _stream?.Dispose();
        _stream = null;
        _client?.TryDispose();
        _client = null;
    }

    /// <summary>确保连接有效，断线则重连</summary>
    protected void EnsureConnect()
    {
        if (_client?.Connected != true) Open();
    }

    #endregion

    #region 读写接口

    /// <summary>批量读取字软元件</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数（最大 960）</param>
    /// <returns>字数据数组</returns>
    public virtual UInt16[] ReadWords(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var msg = MCMessage.BuildReadWord(devCode, startAddr, count);
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetWordData();
    }

    /// <summary>批量读取位软元件</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数（最大 7168）</param>
    /// <returns>位数据数组（true=ON，false=OFF）</returns>
    public virtual Boolean[] ReadBits(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var msg = MCMessage.BuildReadBit(devCode, startAddr, count);
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetBitData(count);
    }

    /// <summary>批量写入字软元件</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="values">字数据（最大 960 字）</param>
    public virtual void WriteWords(DeviceCode devCode, Int32 startAddr, UInt16[] values)
    {
        var msg = MCMessage.BuildWriteWord(devCode, startAddr, values);
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>批量写入位软元件</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="values">位数据（true=ON，false=OFF）</param>
    public virtual void WriteBits(DeviceCode devCode, Int32 startAddr, Boolean[] values)
    {
        var ushorts = Array.ConvertAll(values, v => v ? (UInt16)1 : (UInt16)0);
        var msg = MCMessage.BuildWriteBit(devCode, startAddr, ushorts);
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    #endregion

    #region 1E 帧读写

    /// <summary>1E 帧批量读取字软元件</summary>
    /// <param name="devCode">软元件代码（A 系列兼容）</param>
    /// <param name="startAddr">起始地址（16 位）</param>
    /// <param name="count">点数</param>
    /// <returns>字数据数组</returns>
    public virtual UInt16[] ReadWords1E(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count)
    {
        var msg = MC1EMessage.BuildReadWord(devCode, startAddr, count);
        var response = SendCommand1E(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetWordData();
    }

    /// <summary>1E 帧批量读取位软元件</summary>
    /// <param name="devCode">软元件代码（A 系列兼容）</param>
    /// <param name="startAddr">起始地址（16 位）</param>
    /// <param name="count">点数</param>
    /// <returns>位数据数组（true=ON，false=OFF）</returns>
    public virtual Boolean[] ReadBits1E(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count)
    {
        var msg = MC1EMessage.BuildReadBit(devCode, startAddr, count);
        var response = SendCommand1E(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetBitData(count);
    }

    /// <summary>1E 帧批量写入字软元件</summary>
    /// <param name="devCode">软元件代码（A 系列兼容）</param>
    /// <param name="startAddr">起始地址（16 位）</param>
    /// <param name="values">字数据</param>
    public virtual void WriteWords1E(MC1EDeviceCode devCode, UInt16 startAddr, UInt16[] values)
    {
        var msg = MC1EMessage.BuildWriteWord(devCode, startAddr, values);
        var response = SendCommand1E(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>1E 帧批量写入位软元件</summary>
    /// <param name="devCode">软元件代码（A 系列兼容）</param>
    /// <param name="startAddr">起始地址（16 位）</param>
    /// <param name="values">位数据（true=ON，false=OFF）</param>
    public virtual void WriteBits1E(MC1EDeviceCode devCode, UInt16 startAddr, Boolean[] values)
    {
        var ushorts = Array.ConvertAll(values, v => v ? (UInt16)1 : (UInt16)0);
        var msg = MC1EMessage.BuildWriteBit(devCode, startAddr, ushorts);
        var response = SendCommand1E(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    #endregion

    #region 底层通信

    /// <summary>发送命令并接收响应（线程安全，3E 帧）</summary>
    /// <param name="request">请求消息</param>
    /// <returns>响应消息</returns>
    internal protected virtual MCResponse SendCommand(MCMessage request)
    {
        lock (_lock)
        {
            EnsureConnect();

            var buf = request.ToBytes();
            using var span = Tracer?.NewSpan("mc:SendCommand", buf.ToHex("-"));

            Log?.Debug("{0}=> {1}", Address, buf.ToHex("-", 64));

            try
            {
                _stream.Write(buf, 0, buf.Length);

                MCResponse response;
                if (DataFormat.IsAscii())
                {
                    response = ReceiveAsciiResponse(span);
                }
                else
                {
                    response = ReceiveBinaryResponse(span);
                }

                return response;
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                // 连接可能已断开，下次重连
                Close();
                throw;
            }
        }
    }

    /// <summary>发送 1E 帧命令并接收响应（线程安全）</summary>
    /// <param name="request">1E 请求消息</param>
    /// <returns>1E 响应消息</returns>
    internal protected virtual MC1EResponse SendCommand1E(MC1EMessage request)
    {
        lock (_lock)
        {
            EnsureConnect();

            var buf = request.ToBytes();
            using var span = Tracer?.NewSpan("mc:SendCommand1E", buf.ToHex("-"));

            Log?.Debug("{0}=> {1}", Address, buf.ToHex("-", 64));

            try
            {
                _stream.Write(buf, 0, buf.Length);

                // 1E 响应固定头：副头(1) + 结束码(1) = 2 字节
                var header = new Byte[MC1EResponse.FIXED_HEADER_LEN];
                ReadFully(_stream, header, 0, header.Length);

                // 从响应数据计算剩余长度
                // 字读：每字 2 字节；位读：每 2 点 1 字节
                // 固定头之后即为响应数据，无长度字段，根据请求时的 count 推算
                // 实际上，1E 帧也没有数据长度字段，需要根据读写类型和 count 推算
                var response = new MC1EResponse();
                response.SubHeader = header[0];
                response.EndCode = header[1];

                if (response.EndCode == 0)
                {
                    // 根据请求的操作类型和数据点数推算剩余数据长度
                    var dataLen = CalcResponseDataLength(request);
                    if (dataLen > 0)
                    {
                        var data = new Byte[dataLen];
                        ReadFully(_stream, data, 0, dataLen);
                        response.RawData = data;
                    }
                }

                if (span != null) span.Tag += Environment.NewLine + response.ToBytes().ToHex("-", 64);
                Log?.Debug("{0}<= {1}", Address, response.ToBytes().ToHex("-", 64));

                return response;
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                Close();
                throw;
            }
        }
    }

    /// <summary>计算 1E 帧响应数据长度</summary>
    private static Int32 CalcResponseDataLength(MC1EMessage request)
    {
        var isBit = request.SubHeader == MC1EMessage.SUB_READ_BIT ||
                    request.SubHeader == MC1EMessage.SUB_WRITE_BIT;
        var count = request.Count;

        if (isBit)
        {
            // 位模式：每 2 点 1 字节
            return (count + 1) / 2;
        }
        else
        {
            // 字模式：每字 2 字节
            return count * 2;
        }
    }

    /// <summary>接收二进制模式响应</summary>
    private MCResponse ReceiveBinaryResponse(ISpan span)
    {
        // 读取固定头：子头(2)+网络号(1)+PC号(1)+IO单元号(2)+通道号(1)+数据长度(2) = 9字节
        var header = new Byte[MCResponse.FIXED_HEADER_LEN];
        ReadFully(_stream, header, 0, header.Length);

        // 从头部偏移 7~8 读取数据长度（LE）
        var dataLength = header[7] | (header[8] << 8);

        // 读取可变部分：结束码(2) + 响应数据(N)
        var data = new Byte[dataLength];
        ReadFully(_stream, data, 0, dataLength);

        // 合并解析
        var all = new Byte[header.Length + data.Length];
        Array.Copy(header, 0, all, 0, header.Length);
        Array.Copy(data, 0, all, header.Length, data.Length);

        if (span != null) span.Tag += Environment.NewLine + all.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, all.ToHex("-", 64));

        var response = new MCResponse();
        response.Read(new MemoryStream(all), null);
        return response;
    }

    /// <summary>接收 ASCII 模式响应</summary>
    private MCResponse ReceiveAsciiResponse(ISpan span)
    {
        // ASCII 模式：每个字节用 2 个 ASCII 十六进制字符表示
        // 固定头 9 字节 → 18 ASCII 字符
        var headerLen = MCResponse.FIXED_HEADER_ASCII_LEN;
        var headerBuf = new Byte[headerLen];
        ReadFully(_stream, headerBuf, 0, headerLen);

        // 将 ASCII 十六进制字符转换为二进制字节
        var headerHex = System.Text.Encoding.ASCII.GetString(headerBuf);
        var header = headerHex.ToHex();

        // 从二进制头部偏移 7~8 读取数据长度（LE）
        var dataLength = header[7] | (header[8] << 8);

        // 读取可变部分：结束码(2) + 响应数据(N) → dataLength 字节 → dataLength * 2 ASCII 字符
        var dataAsciiLen = dataLength * 2;
        var dataBuf = new Byte[dataAsciiLen];
        ReadFully(_stream, dataBuf, 0, dataAsciiLen);
        var dataHex = System.Text.Encoding.ASCII.GetString(dataBuf);
        var data = dataHex.ToHex();

        // 合并解析
        var all = new Byte[header.Length + data.Length];
        Array.Copy(header, 0, all, 0, header.Length);
        Array.Copy(data, 0, all, header.Length, data.Length);

        if (span != null) span.Tag += Environment.NewLine + all.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, all.ToHex("-", 64));

        var response = new MCResponse { DataFormat = DataFormat };
        response.Read(new MemoryStream(all), null);
        return response;
    }

    private static void ReadFully(Stream stream, Byte[] buffer, Int32 offset, Int32 count)
    {
        var totalRead = 0;
        while (totalRead < count)
        {
            var read = stream.Read(buffer, offset + totalRead, count - totalRead);
            if (read == 0) throw new IOException("TCP连接已关闭，无法读取MC协议响应");
            totalRead += read;
        }
    }

    #endregion

    #region 日志

    /// <summary>日志</summary>
    protected void WriteLog(String format, params Object[] args)
    {
        if (Log != null && Log != Logger.Null)
            Log.Info(format, args);
    }

    #endregion
}
