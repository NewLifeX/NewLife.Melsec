using System.Runtime.Serialization;
using System.Text;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱FxLinks消息</summary>
/// <remarks>
/// 功能码：
/// ENQ 05
/// STX 02
/// ETX 03
/// NAK H15
/// </remarks>
public class FxLinksMessage : IAccessor
{
    #region 属性
    /// <summary>控制码。02/03/05/15</summary>
    public ControlCodes Code { get; set; }

    /// <summary>站号</summary>
    public Byte Station { get; set; }

    /// <summary>PLC号</summary>
    public Byte PLC { get; set; }

    /// <summary>操作码</summary>
    public String Command { get; set; }

    /// <summary>等待字符</summary>
    public Byte Wait { get; set; }

    /// <summary>地址</summary>
    public String Address { get; set; }

    /// <summary>负载数据</summary>
    public String Payload { get; set; }

    /// <summary>校验和。读取出来</summary>
    public Byte CheckSum { get; set; }

    /// <summary>校验和。计算出来</summary>
    public Byte CheckSum2 { get; set; }
    #endregion

    #region 构造
    /// <summary>已重载。友好字符串</summary>
    /// <returns></returns>
    public override String ToString()
    {
        if (Code == ControlCodes.ENQ)
            return $"{Command} ({Address}, {Payload})";
        else
            return $"{Code} ({Payload})";
    }
    #endregion

    #region 方法
    const Int32 HEADER05 = 2 + 2 + 2 + 1 + 5 + 2;

    /// <summary>读取</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        Code = (ControlCodes)stream.ReadByte();
        switch (Code)
        {
            case ControlCodes.ENQ:
                {
                    // 05FFWR0D02100132
                    var hex = stream.ReadBytes(-1).ToStr();

                    Station = hex.ToByte(0);
                    PLC = hex.ToByte(2);

                    Command = hex[4..6];
                    Wait = Convert.ToByte(hex[6..7], 16);

                    // 注意点位Y0
                    Address = hex[7] + hex[8..12].TrimStart('0');
                    if (Address.Length == 1) Address += '0';

                    var len = hex.Length - HEADER05;
                    if (len > 0)
                    {
                        Payload = hex.Substring(12, len);
                        //var str = hex.Substring(12, len);
                        //if (Command == "BW")
                        //    Payload = str.ToArray().Select(e => Convert.ToByte(e + "", 16)).ToArray();
                        //else
                        //    Payload = str.ToHex();
                    }

                    CheckSum = hex[^2..].ToByte(0);
                    CheckSum2 = (Byte)hex.ToArray().Take(hex.Length - 2).Sum(e => e);

                    break;
                }

            default:
                return false;
        }

        return true;
    }

    /// <summary>写入消息到数据流</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public virtual Boolean Write(Stream stream, Object context)
    {
        stream.Write((Byte)Code);

        switch (Code)
        {
            case ControlCodes.ENQ:
                {
                    // 05FFWR0D02100132
                    var sb = new StringBuilder(64);

                    sb.Append(Station.ToString("X2"));
                    sb.Append(PLC.ToString("X2"));
                    sb.Append(Command);
                    sb.Append(Wait.ToString("X"));

                    var addr = Address[0] + Address[1..].PadLeft(4, '0');
                    sb.Append(addr);

                    var pk = Payload;
                    if (pk != null)
                    {
                        //var buf = pk.ReadBytes();
                        //for (var i = 0; i < buf.Length; i++)
                        //{
                        //    sb.Append((Char)buf[i]);
                        //}
                        sb.Append(pk);

                        //if (Command == "BW")
                        //{
                        //    var buf = pk.ReadBytes();
                        //    for (var i = 0; i < buf.Length; i++)
                        //    {
                        //        //sb.Append(Convert.ToString(buf[i], 16));
                        //        sb.Append((Char)buf[i]);
                        //    }
                        //}
                        //else
                        //    sb.Append(pk.ToHex(256));
                    }

                    var sum = 0;
                    for (var i = 0; i < sb.Length; i++)
                    {
                        sum += sb[i];
                    }
                    CheckSum2 = (Byte)sum;
                    sb.Append(CheckSum2.ToString("X2"));

                    var hex = sb.ToString();
                    stream.Write(hex.GetBytes());

                    break;
                }

            default:
                return false;
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
    public virtual FxLinksResponse CreateReply()
    {
        var msg = new FxLinksResponse
        {
            Station = Station,
            PLC = PLC,
            Command = Command,
        };

        return msg;
    }

    /// <summary>获取指令的HEX字符串形式</summary>
    /// <param name="msg"></param>
    /// <returns></returns>
    public static String GetHex(Byte[] msg)
    {
        if (msg == null || msg.Length == 0) return null;

        var str = msg.ToStr();

        var sb = new StringBuilder();
        for (var i = 0; i < str.Length; i++)
        {
            var ch = str[i];
            if (ch == 0x02)
                sb.Append("STX-");
            else if (ch == 0x03)
                sb.Append("-ETX-");
            else if (ch == 0x05)
                sb.Append("ENQ-");
            else if (ch == 0x06)
                sb.Append("ACK-");
            else if (ch == 0x15)
                sb.Append("NAK-");
            else
                sb.Append(ch);
        }

        return sb.ToString();
    }
    #endregion
}