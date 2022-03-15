using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.IoT.Protocols;

/// <summary>
/// ModbusTcp消息
/// </summary>
public class ModbusTcpMessage : ModbusMessage
{
    #region 属性
    /// <summary>事务编号</summary>
    public UInt16 TransactionId { get; set; }

    /// <summary>协议</summary>
    public UInt16 ProtocolId { get; set; }
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Boolean Read(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        TransactionId = binary.Read<UInt16>();
        ProtocolId = binary.Read<UInt16>();

        var len = binary.Read<UInt16>();
        if (len < 1 + 1 + 1 || stream.Position + len > stream.Length) return false;

        return base.Read(stream, context ?? binary);
    }

    /// <summary>解析消息</summary>
    /// <param name="pk"></param>
    /// <param name="reply"></param>
    /// <returns></returns>
    public new static ModbusTcpMessage Read(Packet pk, Boolean reply = false)
    {
        var msg = new ModbusTcpMessage { Reply = reply };
        if (msg.Read(pk.GetStream(), null)) return msg;

        return null;
    }

    /// <summary>写入消息到数据流</summary>
    /// <param name="stream"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public override Boolean Write(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        binary.Write(TransactionId);
        binary.Write(ProtocolId);

        var pk = Payload;
        var len = 1 + 1;
        if (!Reply)
            len += 2 + (pk?.Total ?? 0);
        else
            len += 1 + (pk?.Total ?? 0);
        binary.Write((UInt16)len);

        return base.Write(stream, context ?? binary);

        //var ms = new MemoryStream();
        //if (!base.Write(ms, null)) return false;

        //var buf = ms.ToArray();
        //binary.Write((Byte)buf.Length);
        //binary.Write(buf, 0, buf.Length);

        //return true;
    }

    /// <summary>创建响应</summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public override ModbusMessage CreateReply()
    {
        if (Reply) throw new InvalidOperationException();

        var msg = new ModbusTcpMessage
        {
            Reply = true,
            TransactionId = TransactionId,
            ProtocolId = ProtocolId,
            Host = Host,
            Code = Code,
        };

        return msg;
    }
    #endregion
}