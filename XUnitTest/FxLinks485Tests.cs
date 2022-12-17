using System.IO;
using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

public class FxLinks485Tests
{
    [Fact]
    public void ReadBitM103()
    {
        var str = "05 30 35 46 46 42 52 30 4D 30 31 30 33 30 31 32 37";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FFBR0M01030127", v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("BR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        Assert.Equal("01", msg.Payload.ToHex());
        Assert.Equal(0x27, msg.CheckSum);
        Assert.Equal(0x27, msg.CheckSum2);
        Assert.Equal("BR (M103, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "02 30 35 46 46 31 03 32 35";
        dt = str.ToHex();
        v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FF1\u000325", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal("01", rs.Payload.ToHex());
        Assert.Equal(0x25, rs.CheckSum);
        Assert.Equal(0x25, rs.CheckSum2);
        Assert.Equal("STX (01)", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void ReadWordM103()
    {
        var str = "05 30 35 46 46 57 52 30 4D 30 31 30 33 30 31 33 43";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FFWR0M0103013C", v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("WR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        Assert.Equal("01", msg.Payload.ToHex());
        Assert.Equal(0x3C, msg.CheckSum);
        Assert.Equal(0x3C, msg.CheckSum2);
        Assert.Equal("WR (M103, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "02 30 35 46 46 31 03 32 35";
        dt = str.ToHex();
        v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FF1\u000325", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal("01", rs.Payload.ToHex());
        Assert.Equal(0x25, rs.CheckSum);
        Assert.Equal(0x25, rs.CheckSum2);
        Assert.Equal("STX (01)", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }
}