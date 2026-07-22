using System.Net.Sockets;
using NewLife.Log;

namespace NewLife.Melsec.Protocols;

/// <summary>MC协议传输模式</summary>
public enum MCTransportMode
{
    /// <summary>TCP 传输（默认）</summary>
    Tcp = 0,

    /// <summary>UDP 传输</summary>
    Udp = 1,

    /// <summary>串口传输（RS-232/RS-485）。帧格式与 3E/1E 相同，通过 SerialPort 传输</summary>
    Serial = 2,
}

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

    /// <summary>PLC地址。格式：IP:端口（如 192.168.1.10:6000）或串口名（如 COM3）</summary>
    public String Address { get; set; }

    /// <summary>帧类型。3E 帧（默认）、1E 帧或 4E 帧</summary>
    public MCFrameType FrameType { get; set; } = MCFrameType.Frame3E;

    /// <summary>4E 帧序列号计数器。每次发送 4E 帧请求时递增</summary>
    private UInt16 _serialNumber;

    /// <summary>传输模式。TCP（默认）、UDP 或 Serial（串口）</summary>
    public MCTransportMode TransportMode { get; set; } = MCTransportMode.Tcp;

    /// <summary>网络号（仅 3E 帧）。通常 0x00</summary>
    public Byte NetworkNo { get; set; } = 0x00;

    /// <summary>PC号（仅 3E 帧）。通常 0xFF</summary>
    public Byte PCNo { get; set; } = 0xFF;

    /// <summary>数据格式（仅 3E 帧）。默认二进制模式，可切换为 ASCII 十六进制模式</summary>
    public MCDataFormat DataFormat { get; set; } = MCDataFormat.Binary;

    /// <summary>网络超时（毫秒）。默认 5000ms</summary>
    public Int32 Timeout { get; set; } = 5000;

    /// <summary>波特率（串口模式）。默认 9600</summary>
    public Int32 Baudrate { get; set; } = 9600;

    /// <summary>数据位（串口模式）。默认 8</summary>
    public Int32 DataBits { get; set; } = 8;

    /// <summary>奇偶校验（串口模式）。默认 None</summary>
    public System.IO.Ports.Parity Parity { get; set; } = System.IO.Ports.Parity.None;

    /// <summary>停止位（串口模式）。默认 One</summary>
    public System.IO.Ports.StopBits StopBits { get; set; } = System.IO.Ports.StopBits.One;

    /// <summary>性能追踪器</summary>
    public ITracer Tracer { get; set; }

    /// <summary>日志</summary>
    public ILog Log { get; set; }

    private TcpClient _client;
    private NetworkStream _stream;
    private UdpClient _udp;
    private System.IO.Ports.SerialPort _serialPort;
    private System.Net.IPEndPoint _remoteEndPoint;
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

    /// <summary>打开连接（TCP/UDP/Serial）</summary>
    public void Open()
    {
        if (TransportMode == MCTransportMode.Serial)
        {
            if (_serialPort?.IsOpen == true) return;

            var portName = Address ?? throw new InvalidOperationException("Address 未设置（串口模式需指定 COM 口）");

            _serialPort = new System.IO.Ports.SerialPort(portName, Baudrate, Parity, DataBits, StopBits)
            {
                ReadTimeout = Timeout,
                WriteTimeout = Timeout,
            };
            _serialPort.Open();

            WriteLog("MCProtocol.Open Serial {0} {1},{2},{3},{4}", portName, Baudrate, DataBits, Parity, StopBits);
        }
        else if (TransportMode == MCTransportMode.Udp)
        {
            if (_udp != null) return;

            var addr = Address ?? throw new InvalidOperationException("Address 未设置");
            var colonIdx = addr.LastIndexOf(':');
            var host = colonIdx > 0 ? addr[..colonIdx] : addr;
            var port = colonIdx > 0 ? Int32.Parse(addr[(colonIdx + 1)..]) : 6000;

            _udp = new UdpClient(host, port);
            _udp.Client.ReceiveTimeout = Timeout;
            _udp.Client.SendTimeout = Timeout;

            WriteLog("MCProtocol.Open UDP {0}", Address);
        }
        else
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

            WriteLog("MCProtocol.Open TCP {0}", Address);
        }
    }

    /// <summary>关闭连接</summary>
    public void Close()
    {
        _stream?.Dispose();
        _stream = null;
        _client?.TryDispose();
        _client = null;
        if (_udp != null) { ((IDisposable)_udp).Dispose(); _udp = null; }
        if (_serialPort != null)
        {
            if (_serialPort.IsOpen) _serialPort.Close();
            _serialPort.Dispose();
            _serialPort = null;
        }
    }

    /// <summary>确保连接有效，断线则重连</summary>
    protected void EnsureConnect()
    {
        if (TransportMode == MCTransportMode.Serial)
        {
            if (_serialPort?.IsOpen != true) Open();
        }
        else if (TransportMode == MCTransportMode.Udp)
        {
            Open();
        }
        else if (_client?.Connected != true)
        {
            Open();
        }
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
        msg.FrameType = FrameType;

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
        msg.FrameType = FrameType;

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
        msg.FrameType = FrameType;

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
        msg.FrameType = FrameType;

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

    #region 类型转换读取

    /// <summary>批量读取 Int32 类型数据（每值 2 字，带符号）</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public virtual Int32[] ReadInt32(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var words = ReadWords(devCode, startAddr, count * 2);
        var result = new Int32[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = (Int32)(UInt32)(words[i * 2] | (words[i * 2 + 1] << 16));
        }
        return result;
    }

    /// <summary>批量读取 UInt32 类型数据（每值 2 字）</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public virtual UInt32[] ReadUInt32(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var words = ReadWords(devCode, startAddr, count * 2);
        var result = new UInt32[count];
        for (var i = 0; i < count; i++)
        {
            result[i] = (UInt32)(words[i * 2] | (words[i * 2 + 1] << 16));
        }
        return result;
    }

    /// <summary>批量读取 Single（Float）类型数据（每值 2 字，IEEE 754）</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public virtual Single[] ReadSingle(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var words = ReadWords(devCode, startAddr, count * 2);
        var result = new Single[count];
        for (var i = 0; i < count; i++)
        {
            var raw = (UInt32)(words[i * 2] | (words[i * 2 + 1] << 16));
            result[i] = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
        }
        return result;
    }

    /// <summary>批量读取 Double 类型数据（每值 4 字，IEEE 754）</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="count">点数</param>
    public virtual Double[] ReadDouble(DeviceCode devCode, Int32 startAddr, Int32 count)
    {
        var words = ReadWords(devCode, startAddr, count * 4);
        var result = new Double[count];
        for (var i = 0; i < count; i++)
        {
            var b0 = BitConverter.GetBytes(words[i * 4]);
            var b1 = BitConverter.GetBytes(words[i * 4 + 1]);
            var b2 = BitConverter.GetBytes(words[i * 4 + 2]);
            var b3 = BitConverter.GetBytes(words[i * 4 + 3]);
            var raw = new Byte[8];
            Array.Copy(b0, 0, raw, 0, 2);
            Array.Copy(b1, 0, raw, 2, 2);
            Array.Copy(b2, 0, raw, 4, 2);
            Array.Copy(b3, 0, raw, 6, 2);
            result[i] = BitConverter.ToDouble(raw, 0);
        }
        return result;
    }

    /// <summary>读取字符串类型数据</summary>
    /// <param name="devCode">软元件代码</param>
    /// <param name="startAddr">起始地址</param>
    /// <param name="wordLength">字数</param>
    /// <returns>ASCII 编码字符串（去除末尾空白和空字符）</returns>
    public virtual String ReadString(DeviceCode devCode, Int32 startAddr, Int32 wordLength)
    {
        var words = ReadWords(devCode, startAddr, wordLength);
        var bytes = new Byte[wordLength * 2];
        for (var i = 0; i < wordLength; i++)
        {
            bytes[i * 2] = (Byte)(words[i] & 0xFF);
            bytes[i * 2 + 1] = (Byte)(words[i] >> 8);
        }
        var str = System.Text.Encoding.ASCII.GetString(bytes).TrimEnd('\0', ' ', '\t');
        return str;
    }

    /// <summary>1E 帧批量读取 Int32 类型数据</summary>
    public virtual Int32[] ReadInt32_1E(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count)
    {
        var words = ReadWords1E(devCode, startAddr, count * 2);
        var result = new Int32[count];
        for (var i = 0; i < count; i++)
            result[i] = (Int32)(UInt32)(words[i * 2] | (words[i * 2 + 1] << 16));
        return result;
    }

    /// <summary>1E 帧批量读取 Single 类型数据</summary>
    public virtual Single[] ReadSingle_1E(MC1EDeviceCode devCode, UInt16 startAddr, Int32 count)
    {
        var words = ReadWords1E(devCode, startAddr, count * 2);
        var result = new Single[count];
        for (var i = 0; i < count; i++)
        {
            var raw = (UInt32)(words[i * 2] | (words[i * 2 + 1] << 16));
            result[i] = BitConverter.ToSingle(BitConverter.GetBytes(raw), 0);
        }
        return result;
    }

    #endregion

    #region 随机读取（Random Read）

    /// <summary>随机读取多个字软元件（命令 0403h）</summary>
    /// <remarks>
    /// 单次请求读取多个不连续地址的字软元件值。
    /// 每个地址返回 1 个字（2 字节）。
    /// </remarks>
    /// <param name="items">随机读取项列表（软元件代码 + 起始地址）</param>
    /// <returns>字数据数组，按请求顺序排列</returns>
    public virtual UInt16[] ReadRandomWords(params (DeviceCode code, Int32 addr)[] items)
    {
        var raw = BuildRandomReadRequestData(items);
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_RANDOM_READ,
            SubCommand = MCMessage.SUBCMD_WORD,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetWordData();
    }

    /// <summary>随机读取多个位软元件（命令 0403h）</summary>
    /// <param name="items">随机读取项列表（软元件代码 + 起始地址）</param>
    /// <returns>位数据数组，按请求顺序排列</returns>
    public virtual Boolean[] ReadRandomBits(params (DeviceCode code, Int32 addr)[] items)
    {
        var raw = BuildRandomReadRequestData(items);
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_RANDOM_READ,
            SubCommand = MCMessage.SUBCMD_BIT,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);

        return response.GetBitData(items.Length);
    }

    /// <summary>构建随机读取请求数据</summary>
    private static Byte[] BuildRandomReadRequestData((DeviceCode code, Int32 addr)[] items)
    {
        using var ms = new MemoryStream();
        // 点数（1 字节）
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

    #region 远程控制（RUN/STOP）

    /// <summary>远程 RUN（运行）</summary>
    /// <param name="clearMode">清除模式：0=不清除，1=清除</param>
    public virtual void RemoteRun(Byte clearMode = 0)
    {
        var raw = new Byte[] { clearMode };
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_RUN,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>远程 STOP（停止）</summary>
    public virtual void RemoteStop()
    {
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_STOP,
            SubCommand = 0x0000,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>远程密码解锁。发送密码解锁 PLC，使其允许远程操作</summary>
    /// <remarks>
    /// MC 协议命令 1630h。密码为 ASCII 字符串，发送格式为：
    /// 密码长度(2B LE) + 密码 ASCII(N 字节)。
    /// 解锁后 PLC 允许远程 RUN/STOP 等操作。
    /// </remarks>
    /// <param name="password">密码。ASCII 字符串</param>
    public virtual void RemoteUnlock(String password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (password.Length == 0) throw new ArgumentException("密码不能为空", nameof(password));

        var raw = BuildPasswordRequestData(password);
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_UNLOCK,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>远程密码锁定。发送密码重新锁定 PLC，禁止远程操作</summary>
    /// <remarks>
    /// MC 协议命令 1631h。与 RemoteUnlock 使用相同密码格式。
    /// 锁定后 PLC 拒绝远程 RUN/STOP 等操作。
    /// </remarks>
    /// <param name="password">密码。ASCII 字符串</param>
    public virtual void RemoteLock(String password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (password.Length == 0) throw new ArgumentException("密码不能为空", nameof(password));

        var raw = BuildPasswordRequestData(password);
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_REMOTE_LOCK,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>构建远程密码锁定/解锁请求数据</summary>
    /// <param name="password">ASCII 密码</param>
    /// <returns>请求数据：密码长度(2B LE) + 密码 ASCII</returns>
    private static Byte[] BuildPasswordRequestData(String password)
    {
        var pwdBytes = System.Text.Encoding.ASCII.GetBytes(password);
        if (pwdBytes.Length > 255) throw new ArgumentOutOfRangeException(nameof(password), "密码过长，最多 255 字节");

        using var ms = new MemoryStream();
        MCMessage.WriteUInt16LE(ms, (UInt16)pwdBytes.Length);
        ms.Write(pwdBytes, 0, pwdBytes.Length);
        return ms.ToArray();
    }

    /// <summary>监视注册。注册多个字/双字软元件地址，PLC 将定期更新这些地址的值</summary>
    /// <remarks>
    /// MC 协议命令 0801h。注册后可调用 <see cref="MonitorRead"/> 读取已注册地址的最新值。
    /// 单次最多监视 192 个字地址 + 192 个双字地址（或取决于 PLC 系列）。
    /// </remarks>
    /// <param name="wordDevices">字软元件列表（软元件代码 + 起始地址）</param>
    /// <param name="doubleWordDevices">双字软元件列表（软元件代码 + 起始地址）</param>
    public virtual void MonitorRegist(
        (DeviceCode code, Int32 addr)[] wordDevices,
        (DeviceCode code, Int32 addr)[] doubleWordDevices)
    {
        if (wordDevices == null) throw new ArgumentNullException(nameof(wordDevices));
        if (doubleWordDevices == null) throw new ArgumentNullException(nameof(doubleWordDevices));
        if (wordDevices.Length + doubleWordDevices.Length == 0) throw new ArgumentException("至少需要一个监视地址");

        var raw = BuildMonitorRegistRequestData(wordDevices, doubleWordDevices);
        var msg = new MCMessage
        {
            Command = MCMessage.CMD_MONITOR_REGIST,
            SubCommand = 0x0000,
            RawRequestData = raw,
        };
        msg.NetworkNo = NetworkNo;
        msg.PCNo = PCNo;
        msg.DataFormat = DataFormat;
        msg.FrameType = FrameType;

        var response = SendCommand(msg);
        if (response.EndCode != 0)
            throw new MCException(response.EndCode);
    }

    /// <summary>构建监视注册请求数据</summary>
    private static Byte[] BuildMonitorRegistRequestData(
        (DeviceCode code, Int32 addr)[] wordDevices,
        (DeviceCode code, Int32 addr)[] doubleWordDevices)
    {
        if (wordDevices.Length > 255) throw new ArgumentOutOfRangeException(nameof(wordDevices), "字设备数量不能超过 255");
        if (doubleWordDevices.Length > 255) throw new ArgumentOutOfRangeException(nameof(doubleWordDevices), "双字设备数量不能超过 255");

        using var ms = new MemoryStream();
        // 字设备数量（1 字节）
        ms.WriteByte((Byte)wordDevices.Length);
        // 双字设备数量（1 字节）
        ms.WriteByte((Byte)doubleWordDevices.Length);
        // 保留（2 字节）
        ms.WriteByte(0x00);
        ms.WriteByte(0x00);

        // 字设备地址列表：地址(3B LE) + 代码(1B)
        foreach (var (code, addr) in wordDevices)
        {
            ms.WriteByte((Byte)(addr & 0xFF));
            ms.WriteByte((Byte)((addr >> 8) & 0xFF));
            ms.WriteByte((Byte)((addr >> 16) & 0xFF));
            ms.WriteByte((Byte)code);
        }

        // 双字设备地址列表：地址(3B LE) + 代码(1B)
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

    #region 底层通信

    /// <summary>发送命令并接收响应（线程安全，3E/4E 帧）</summary>
    /// <param name="request">请求消息</param>
    /// <returns>响应消息</returns>
    internal protected virtual MCResponse SendCommand(MCMessage request)
    {
        lock (_lock)
        {
            EnsureConnect();

            // 4E 帧自动分配序列号
            if (request.FrameType.Is4E())
            {
                request.SerialNumber = ++_serialNumber;
            }

            var buf = request.ToBytes();
            using var span = Tracer?.NewSpan("mc:SendCommand", buf.ToHex("-"));

            Log?.Debug("{0}=> {1}", Address, buf.ToHex("-", 64));

            try
            {
                if (TransportMode == MCTransportMode.Serial)
                {
                    _serialPort.Write(buf, 0, buf.Length);
                    return ReceiveSerialResponse(span);
                }
                else if (TransportMode == MCTransportMode.Udp)
                {
                    _udp.Send(buf, buf.Length);

                    MCResponse response;
                    if (DataFormat.IsAscii())
                    {
                        response = ReceiveAsciiResponseUdp(span);
                    }
                    else
                    {
                        response = ReceiveBinaryResponseUdp(span);
                    }

                    return response;
                }
                else
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
                if (TransportMode == MCTransportMode.Serial)
                {
                    _serialPort.Write(buf, 0, buf.Length);
                    return ReceiveSerialResponse1E(request, span);
                }
                else if (TransportMode == MCTransportMode.Udp)
                {
                    _udp.Send(buf, buf.Length);
                    var udpResult = _udp.Receive(ref _remoteEndPoint);
                    var udpResponse = new MC1EResponse();
                    udpResponse.Read(udpResult);
                    return udpResponse;
                }

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
        // 先读子头 2 字节判断帧类型
        var subHeader = new Byte[2];
        ReadFully(_stream, subHeader, 0, 2);

        var is4E = (subHeader[0] == 0xD4 && subHeader[1] == 0x00);
        var headerLen = is4E ? MCResponse.FIXED_HEADER_4E_LEN : MCResponse.FIXED_HEADER_LEN;

        // 读取剩余固定头
        var remainingHeader = new Byte[headerLen - 2];
        ReadFully(_stream, remainingHeader, 0, remainingHeader.Length);

        // 合并完整固定头
        var header = new Byte[headerLen];
        Array.Copy(subHeader, 0, header, 0, 2);
        Array.Copy(remainingHeader, 0, header, 2, remainingHeader.Length);

        // 从头部读取数据长度
        // 3E: 偏移 7~8；4E: 偏移 11~12（因多了 4 字节序列号）
        var dataLengthOffset = is4E ? 11 : 7;
        var dataLength = header[dataLengthOffset] | (header[dataLengthOffset + 1] << 8);

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

    /// <summary>串口接收二进制/ASCII 模式响应</summary>
    private MCResponse ReceiveSerialResponse(ISpan span)
    {
        // 串口没有消息边界，等待一段时间后读取所有可用字节
        var totalTimeout = Timeout;
        var elapsed = 0;
        var waitBetween = 50;

        // 等待数据到达
        while (elapsed < totalTimeout && _serialPort.BytesToRead < 2)
        {
            System.Threading.Thread.Sleep(waitBetween);
            elapsed += waitBetween;
        }

        if (_serialPort.BytesToRead == 0)
            throw new TimeoutException("MC 串口响应超时");

        // 读取可用字节
        var available = _serialPort.BytesToRead;
        var result = new Byte[available];
        _serialPort.Read(result, 0, available);

        if (span != null) span.Tag += Environment.NewLine + result.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, result.ToHex("-", 64));

        var response = new MCResponse { DataFormat = DataFormat };
        response.Read(new MemoryStream(result), null);
        return response;
    }

    /// <summary>串口接收 1E 帧响应</summary>
    private MC1EResponse ReceiveSerialResponse1E(MC1EMessage request, ISpan span)
    {
        var totalTimeout = Timeout;
        var elapsed = 0;
        var waitBetween = 50;

        while (elapsed < totalTimeout && _serialPort.BytesToRead < MC1EResponse.FIXED_HEADER_LEN)
        {
            System.Threading.Thread.Sleep(waitBetween);
            elapsed += waitBetween;
        }

        if (_serialPort.BytesToRead == 0)
            throw new TimeoutException("MC 1E 串口响应超时");

        var available = _serialPort.BytesToRead;
        var result = new Byte[available];
        _serialPort.Read(result, 0, available);

        if (span != null) span.Tag += Environment.NewLine + result.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, result.ToHex("-", 64));

        var response = new MC1EResponse();
        response.Read(result);
        return response;
    }

    /// <summary>UDP 接收二进制模式响应</summary>
    private MCResponse ReceiveBinaryResponseUdp(ISpan span)
    {
        var result = _udp.Receive(ref _remoteEndPoint);
        var response = new MCResponse();
        response.Read(new MemoryStream(result), null);
        if (span != null) span.Tag += Environment.NewLine + result.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, result.ToHex("-", 64));
        return response;
    }

    /// <summary>UDP 接收 ASCII 模式响应</summary>
    private MCResponse ReceiveAsciiResponseUdp(ISpan span)
    {
        var result = _udp.Receive(ref _remoteEndPoint);
        var response = new MCResponse { DataFormat = DataFormat };
        response.Read(new MemoryStream(result), null);
        if (span != null) span.Tag += Environment.NewLine + result.ToHex("-", 64);
        Log?.Debug("{0}<= {1}", Address, result.ToHex("-", 64));
        return response;
    }

    /// <summary>接收 ASCII 模式响应</summary>
    private MCResponse ReceiveAsciiResponse(ISpan span)
    {
        // ASCII 模式：每个字节用 2 个 ASCII 十六进制字符表示
        // 先读子头 "D000" 或 "D400" (4 ASCII chars) 判断帧类型
        var subHeaderBuf = new Byte[4];
        ReadFully(_stream, subHeaderBuf, 0, 4);
        var subHeaderHex = System.Text.Encoding.ASCII.GetString(subHeaderBuf);
        var subHeader = subHeaderHex.ToHex();

        var is4E = (subHeader[0] == 0xD4 && subHeader[1] == 0x00);
        var headerAsciiLen = is4E ? MCResponse.FIXED_HEADER_4E_ASCII_LEN : MCResponse.FIXED_HEADER_ASCII_LEN;

        // 读取剩余固定头
        var remainingAsciiLen = headerAsciiLen - 4;
        var remainingBuf = new Byte[remainingAsciiLen];
        if (remainingAsciiLen > 0)
            ReadFully(_stream, remainingBuf, 0, remainingAsciiLen);

        var remainingHex = System.Text.Encoding.ASCII.GetString(remainingBuf);
        var remaining = remainingHex.ToHex();

        // 合并完整固定头（二进制）
        var header = new Byte[subHeader.Length + remaining.Length];
        Array.Copy(subHeader, 0, header, 0, subHeader.Length);
        Array.Copy(remaining, 0, header, subHeader.Length, remaining.Length);

        // 从头部读取数据长度
        // 3E: 偏移 7~8；4E: 偏移 11~12
        var dataLengthOffset = is4E ? 11 : 7;
        var dataLength = header[dataLengthOffset] | (header[dataLengthOffset + 1] << 8);

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
