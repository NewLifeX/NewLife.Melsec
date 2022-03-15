using System.Runtime.Serialization;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.IoT.Protocols;

/// <summary>Modbus消息</summary>
public class ModbusMessage : IAccessor
{
    #region 属性
    /// <summary>是否响应</summary>
    [IgnoreDataMember]
    public Boolean Reply { get; set; }

    /// <summary>站号</summary>
    public Byte Host { get; set; }

    /// <summary>操作码</summary>
    public FunctionCodes Code { get; set; }

    /// <summary>地址。请求数据，地址与负载；响应数据没有地址只有负载</summary>
    public UInt16 Address { get; set; }

    ///// <summary>数据（数值/个数）。常用字段，优先Payload</summary>
    //public UInt16 Value { get; set; }

    /// <summary>负载数据</summary>
    [IgnoreDataMember]
    public Packet Payload { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。友好字符串</summary>
    /// <returns></returns>
    public override String ToString()
    {
        if (!Reply) return $"{Code} ({Address}, {Payload?.ToHex()})";

        return $"{Code} {Payload?.ToHex()}";
    }
    #endregion

    #region 快速方法
    ///// <summary>设置地址和数据（数值/个数）</summary>
    ///// <param name="address"></param>
    ///// <param name="value"></param>
    //public virtual void Set(UInt16 address, UInt16 value)
    //{
    //    var buf = new Byte[4];
    //    buf.Write(address, 0, false);
    //    buf.Write(value, 2, false);

    //    Payload = buf;
    //}

    ///// <summary>获取地址和数据（数值/个数）</summary>
    ///// <returns></returns>
    //public virtual (UInt16 address, UInt16 value) Get()
    //{
    //    var buf = Payload?.ReadBytes(0, 4);
    //    if (buf == null || buf.Length < 4) return (0, 0);

    //    var addr = buf.ToUInt16(0, false);
    //    var value = buf.ToUInt16(2, false);

    //    return (addr, value);
    //}

    ///// <summary>获取地址和数据（数值/个数）</summary>
    ///// <param name="address"></param>
    ///// <param name="value"></param>
    ///// <returns></returns>
    //public virtual Boolean TryGet(out UInt16 address, out UInt16 value)
    //{
    //    address = value = 0;

    //    var buf = Payload?.ReadBytes(0, 4);
    //    if (buf == null || buf.Length < 4) return false;

    //    address = buf.ToUInt16(0, false);
    //    value = buf.ToUInt16(2, false);

    //    return true;
    //}
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        Host = binary.ReadByte();
        Code = (FunctionCodes)binary.ReadByte();

        var len = (Int32)(stream.Length - stream.Position);
        if (len <= 0) return false;

        if (!Reply)
        {
            // 请求数据，地址和负载
            Address = binary.Read<UInt16>();
            Payload = binary.ReadBytes(len - 2);
        }
        else if (len >= 1)
        {
            // 响应数据，长度和负载
            var len2 = binary.ReadByte();
            if (len2 <= len - 1) Payload = binary.ReadBytes(len2);
        }

        return true;
    }

    /// <summary>解析消息</summary>
    /// <param name="pk"></param>
    /// <param name="reply"></param>
    /// <returns></returns>
    public static ModbusMessage Read(Packet pk, Boolean reply = false)
    {
        var msg = new ModbusMessage { Reply = reply };
        if (msg.Read(pk.GetStream(), null)) return msg;

        return null;
    }

    /// <summary>写入消息到数据流</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual Boolean Write(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        binary.Write(Host);
        binary.Write((Byte)Code);

        var pk = Payload;
        if (!Reply)
        {
            // 请求数据，地址和负载
            binary.Write(Address);
            if (pk != null) binary.Write(pk.Data, pk.Offset, pk.Count);
        }
        else
        {
            var len2 = (pk?.Total ?? 0);
            binary.Write((Byte)len2);
            if (pk != null) binary.Write(pk.Data, pk.Offset, pk.Count);
        }

        return true;
    }

    /// <summary>消息转数据包</summary>
    /// <returns></returns>
    public Packet ToPacket()
    {
        var ms = new MemoryStream();
        Write(ms, null);

        ms.Position = 0;
        return new Packet(ms);
    }

    /// <summary>创建响应</summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public virtual ModbusMessage CreateReply()
    {
        if (Reply) throw new InvalidOperationException();

        var msg = new ModbusMessage
        {
            Reply = true,
            Host = Host,
            Code = Code,
        };

        return msg;
    }
    #endregion
}