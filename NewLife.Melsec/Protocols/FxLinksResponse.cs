using System.Runtime.Serialization;
using System.Text;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Melsec.Protocols;

/// <summary>三菱FxLinks响应</summary>
/// <remarks>
/// 功能码：
/// ENQ 05
/// STX 02
/// ETX 03
/// NAK H15
/// </remarks>
public class FxLinksResponse : IAccessor
{
    #region 属性
    /// <summary>控制码。02/03/05/15</summary>
    public ControlCodes Code { get; set; }

    /// <summary>站号</summary>
    public Byte Host { get; set; }

    /// <summary>PC号</summary>
    public Byte PC { get; set; }

    /// <summary>操作码。解析数据前设置，仅用于判断如何解析数据</summary>
    public String Command { get; set; }

    /// <summary>负载数据</summary>
    [IgnoreDataMember]
    public Packet Payload { get; set; }

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
        //if (Code == ControlCodes.STX)
        //    return $"{Command} ({Payload?.ToHex()})";
        //else
        return $"{Code} ({Payload?.ToHex()})";
    }
    #endregion

    #region 方法
    /// <summary>读取</summary>
    /// <param name="stream">数据流</param>
    /// <param name="context">上下文</param>
    /// <returns></returns>
    public virtual Boolean Read(Stream stream, Object context)
    {
        Code = (ControlCodes)stream.ReadByte();
        switch (Code)
        {
            case ControlCodes.STX:
                {
                    // 05FF0001\03B5
                    // STX-05FF0-ETX-24
                    var p = stream.Position;
                    var hex = stream.ReadBytes(4).ToStr();

                    Host = Convert.ToByte(hex[..2], 16);
                    PC = Convert.ToByte(hex[2..4], 16);

                    var retain = (Int32)(stream.Length - stream.Position);
                    if (retain < 3) return false;

                    var len = retain - 3;
                    if (len > 0)
                    {
                        var str = stream.ReadBytes(len).ToStr();
                        // 位读取时，每个点位占1个字符；字读取时，每个点位占4个字符
                        if (len == 1)
                            Payload = new Byte[] { Convert.ToByte(str, 16) };
                        else if (Command == "BR")
                            Payload = str.ToArray().Select(e => Convert.ToByte(e + "", 16)).ToArray();
                        else
                            Payload = str.ToHex();
                    }

                    var b = (ControlCodes)stream.ReadByte();
                    if (b != ControlCodes.ETX) return false;

                    len = (Int32)(stream.Position - p);
                    stream.Position = p;
                    CheckSum2 = (Byte)stream.ReadBytes(len).Sum(e => e);

                    hex = stream.ReadBytes(2).ToStr();
                    CheckSum = Convert.ToByte(hex, 16);
                    break;
                }

            case ControlCodes.ETX:
                return false;
            case ControlCodes.ACK:
            case ControlCodes.NAK:
                {
                    // 05FF
                    var hex = stream.ReadBytes(-1).ToStr();

                    Host = Convert.ToByte(hex[..2], 16);
                    PC = Convert.ToByte(hex[2..4], 16);

                    break;
                }

            case ControlCodes.EOT:
                return false;
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
            case ControlCodes.STX:
                {
                    // 05FF0001\03B5
                    var sb = new StringBuilder(64);

                    sb.Append(Host.ToString("X2"));
                    sb.Append(PC.ToString("X2"));

                    var pk = Payload;
                    if (pk != null && pk.Total > 0)
                    {
                        // 位读取时，每个点位占1个字符；字读取时，每个点位占4个字符
                        if (pk.Total == 1)
                            sb.Append(pk[0].ToString("X"));
                        else if (Command == "BR")
                        {
                            var buf = pk.ReadBytes();
                            for (var i = 0; i < buf.Length; i++)
                            {
                                sb.Append(Convert.ToString(buf[i], 16));
                            }
                        }
                        else
                            sb.Append(pk.ToHex(256));
                    }

                    sb.Append((Char)ControlCodes.ETX);

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

            case ControlCodes.ETX:
            case ControlCodes.EOT:
                return false;

            case ControlCodes.ACK:
            case ControlCodes.NAK:
                {
                    // 05FF
                    var sb = new StringBuilder(64);

                    sb.Append(Host.ToString("X2"));
                    sb.Append(PC.ToString("X2"));

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
    #endregion
}