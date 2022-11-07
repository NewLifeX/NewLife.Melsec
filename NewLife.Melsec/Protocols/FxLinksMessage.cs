using System.Runtime.Serialization;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱FxLinks消息</summary>
public class FxLinksMessage : IAccessor
{
    #region 属性
    /// <summary>是否响应</summary>
    [IgnoreDataMember]
    public Boolean Reply { get; set; }

    /// <summary>操作码</summary>
    public String Command { get; set; }

    /// <summary>地址</summary>
    public UInt16 Address { get; set; }

    /// <summary>负载数据</summary>
    [IgnoreDataMember]
    public Packet Payload { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。友好字符串</summary>
    /// <returns></returns>
    public override String ToString() => $"{Command} (0x{Address:X4}, {Payload?.ToHex()})";
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        var stx = binary.ReadByte();
        if (stx != 0x02) return false;

        Command = binary.ReadFixedString(2);

        Address = binary.ReadFixedString(4).ToHex().ToUInt16(0, false);
        var len = binary.ReadFixedString(4).ToHex().ToUInt16(0, false);

        if (len > 0) Payload = binary.ReadBytes(len);

        var ext = binary.ReadByte();
        if (ext != 0x03) return false;

        var check = binary.ReadFixedString(2).ToHex()[0];

        return true;
    }

    /// <summary>解析消息</summary>
    /// <param name="data">数据包</param>
    /// <param name="reply">是否响应</param>
    /// <returns></returns>
    public static FxLinksMessage Read(Packet data, Boolean reply = false)
    {
        var msg = new FxLinksMessage { Reply = reply };
        if (msg.Read(data.GetStream(), null)) return msg;

        return null;
    }

    /// <summary>写入消息到数据流</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public virtual Boolean Write(Stream stream, Object context)
    {
        var binary = context as Binary ?? new Binary { Stream = stream, IsLittleEndian = false };

        binary.Write((Byte)0x02);

        binary.WriteFixedString(Command, 2);

        var addr = Address.GetBytes(false).ToHex();
        binary.WriteFixedString(addr, 4);

        var pk = Payload;
        var len = pk?.Total ?? 0;
        binary.WriteFixedString(((UInt16)len).GetBytes(false).ToHex(), 4);

        if (pk != null) binary.Write(pk.Data, pk.Offset, pk.Count);

        var check = 0;
        binary.WriteFixedString(((Byte)check).ToString("X2"), 2);

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
    public virtual FxLinksMessage CreateReply()
    {
        if (Reply) throw new InvalidOperationException();

        var msg = new FxLinksMessage
        {
            Reply = true,
            Command = Command,
        };

        return msg;
    }
    #endregion
}