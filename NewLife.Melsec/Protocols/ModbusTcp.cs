using System.Net.Sockets;
using NewLife.Log;
using NewLife.Net;

namespace NewLife.IoT.Protocols;

/// <summary>ModbusTCP网口通信</summary>
/// <remarks>
/// ADU规定为256
/// </remarks>
public class ModbusTcp : Modbus
{
    #region 属性
    /// <summary>服务端地址。127.0.0.1:502</summary>
    public String Server { get; set; }

    /// <summary>协议标识。默认0</summary>
    public UInt16 ProtocolId { get; set; }

    /// <summary>缓冲区大小。默认256</summary>
    public Int32 BufferSize { get; set; } = 256;

    private Int32 _transactionId;
    private TcpClient _client;
    private NetworkStream _stream;
    #endregion

    #region 构造
    /// <summary>
    /// 销毁
    /// </summary>
    /// <param name="disposing"></param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        _client.TryDispose();
        _client = null;
        _stream = null;
    }
    #endregion

    #region 方法
    /// <summary>初始化。传入配置</summary>
    /// <param name="parameters"></param>
    public override void Init(IDictionary<String, Object> parameters)
    {
        if (parameters.TryGetValue("Server", out var str))
            Server = str + "";
        else if (parameters.TryGetValue("Address", out str))
            Server = str + "";

        if (parameters.TryGetValue("ProtocolId", out str)) ProtocolId = (UInt16)str.ToInt();
    }

    /// <summary>打开</summary>
    public override void Open()
    {
        if (_client == null || !_client.Connected)
        {
            var uri = new NetUri(Server);
            if (uri.Port == 0) uri.Port = 502;

            var client = new TcpClient
            {
                SendTimeout = 3_000,
                ReceiveTimeout = 3_000
            };
            client.Connect(uri.Address, uri.Port);

            _client = client;
            _stream = client.GetStream();

            WriteLog("ModbusTcp.Open {0}:{1}", uri.Host, uri.Port);
        }
    }

    /// <summary>创建消息</summary>
    /// <returns></returns>
    protected override ModbusMessage CreateMessage() => new ModbusTcpMessage
    {
        ProtocolId = ProtocolId,
        TransactionId = (UInt16)Interlocked.Increment(ref _transactionId)
    };

    /// <summary>发送消息并接收返回</summary>
    /// <param name="message">Modbus消息</param>
    /// <returns></returns>
    protected override ModbusMessage SendCommand(ModbusMessage message)
    {
        Open();

        if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("=> {0}", message);
        var cmd = message.ToPacket().ToArray();

        {
            using var span = Tracer?.NewSpan("modbus:SendCommand", cmd.ToHex("-"));
            try
            {
                _stream.Write(cmd, 0, cmd.Length);
            }
            catch (Exception ex)
            {
                span?.SetError(ex, null);
                throw;
            }
        }

        using var span2 = Tracer?.NewSpan("modbus:ReceiveCommand");
        try
        {
            var buf = new Byte[BufferSize];
            var c = _stream.Read(buf, 0, buf.Length);
            buf = buf.ReadBytes(0, c);

            if (span2 != null) span2.Tag = buf.ToHex();

            var rs = ModbusTcpMessage.Read(buf, true);
            if (rs == null) return null;

            if (Log != null && Log.Level <= LogLevel.Debug) WriteLog("<= {0}", rs);

            return rs;
        }
        catch (Exception ex)
        {
            span2?.SetError(ex, null);
            if (ex is TimeoutException) return null;
            throw;
        }
    }
    #endregion
}