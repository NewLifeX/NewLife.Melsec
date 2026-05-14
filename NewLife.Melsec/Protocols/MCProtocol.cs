using System.Net.Sockets;
using NewLife.Log;
using NewLife.Melsec.Protocols;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱MC协议3E帧协议栈（TCP长连接）</summary>
/// <remarks>
/// 负责管理与 PLC 的 TCP 连接，提供字/位软元件的批量读写。
/// 连接断开时自动重连；所有操作均加锁保证线程安全。
/// </remarks>
public class MCProtocol : DisposeBase
{
    #region 属性

    /// <summary>名称</summary>
    public String Name { get; set; }

    /// <summary>PLC地址。格式：IP:端口，如 192.168.1.10:6000</summary>
    public String Address { get; set; }

    /// <summary>网络号。通常 0x00</summary>
    public Byte NetworkNo { get; set; } = 0x00;

    /// <summary>PC号。通常 0xFF</summary>
    public Byte PCNo { get; set; } = 0xFF;

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
        _client?.Dispose();
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

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    #endregion

    #region 底层通信

    /// <summary>发送命令并接收响应（线程安全）</summary>
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

                // 读取固定头：子头(2)+网络号(1)+PC号(1)+IO单元号(2)+通道号(1)+数据长度(2) = 9字节
                var header = new Byte[MCResponse.FIXED_HEADER_LEN];
                ReadFully(_stream, header, 0, header.Length);

                // 从头部偏移 7~8 读取数据长度
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
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                // 连接可能已断开，下次重连
                Close();
                throw;
            }
        }
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
