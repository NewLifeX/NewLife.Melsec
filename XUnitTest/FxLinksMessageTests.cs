using System.IO;
using NewLife;
using NewLife.Data;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

public class FxLinksMessageTests
{
    [Fact]
    public void ReadWord()
    {
        // 05FFWR0D02100132
        var str = "05 30 35 46 46 57 52 30 44 30 32 31 30 30 31 33 32";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FFWR0D02100132", v);

        var msg = new FxLinksMessage();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D210", msg.Address);
        Assert.Equal("01", msg.Payload.ToHex());
        Assert.Equal(0x32, msg.CheckSum);
        Assert.Equal(0x32, msg.CheckSum2);
        Assert.Equal("WR (D210, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void ReadWordResponse()
    {
        // 05FF0001\03B5
        var str = "02 30 35 46 46 30 30 30 31 03 42 35";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1, dt.Length - 1).ToStr();
        Assert.Equal("05FF0001\u0003B5", v);

        var msg = new FxLinksResponse();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.STX, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("0001", msg.Payload.ToHex());
        Assert.Equal(0xB5, msg.CheckSum);
        Assert.Equal(0xB5, msg.CheckSum2);
        Assert.Equal("STX (0001)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void WriteBit()
    {
        // 05FFBW0M0103000101ED
        var str = "05-30-35-46-46-42-57-30-4D-30-31-30-33-30-30-30-31-30-31-45-44";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FFBW0M0103000101ED", v);

        var msg = new FxLinksMessage();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("BW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        Assert.Equal("000101", msg.Payload.ToHex());
        Assert.Equal(0xED, msg.CheckSum);
        Assert.Equal(0xED, msg.CheckSum2);
        Assert.Equal("BW (M103, 000101)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    //[Fact]
    //public void WriteBitResponse()
    //{
    //    // 05FF0
    //    var str = "02-30-35-7F-5E-77-6C-23-7F";
    //    var dt = str.ToHex();
    //    var v = dt.ReadBytes(1).ToStr();
    //    Assert.Equal("05\u007Fw0001\u0003B5", v);

    //    var msg = new FxLinksResponse();
    //    var rs = msg.Read(new MemoryStream(dt), null);
    //    Assert.True(rs);

    //    Assert.Equal(ControlCodes.ACK, msg.Code);
    //    Assert.Equal(5, msg.Host);
    //    Assert.Equal(0xFF, msg.PC);
    //    Assert.Null(msg.Payload);
    //    Assert.Equal(0, msg.CheckSum);
    //    Assert.Equal(0, msg.CheckSum2);
    //    Assert.Equal("ACK ()", msg.ToString());

    //    var pk = msg.ToPacket();
    //    Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    //}

    [Fact]
    public void WriteWord()
    {
        // 05FFWW0D0210010001F8
        var str = "05 30 35 46 46 57 57 30 44 30 32 31 30 30 31 30 30 30 31 46 38";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FFWW0D0210010001F8", v);

        var msg = new FxLinksMessage();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D210", msg.Address);
        Assert.Equal("010001", msg.Payload.ToHex());
        Assert.Equal(0xF8, msg.CheckSum);
        Assert.Equal(0xF8, msg.CheckSum2);
        Assert.Equal("WW (D210, 010001)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void WriteWordResponse()
    {
        // 05FF
        var str = "06 30 35 46 46";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FF", v);

        var msg = new FxLinksResponse();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ACK, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Null(msg.Payload);
        Assert.Equal(0, msg.CheckSum);
        Assert.Equal(0, msg.CheckSum2);
        Assert.Equal("ACK ()", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void WriteWord2()
    {
        // 05FFWW0D0240000100015B
        var str = "05-30-35-46-46-57-57-30-44-30-32-34-30-30-30-30-31-30-30-30-31-35-42";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FFWW0D0240000100015B", v);

        var msg = new FxLinksMessage();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D240", msg.Address);
        Assert.Equal("00010001", msg.Payload.ToHex());
        Assert.Equal(0x5B, msg.CheckSum);
        Assert.Equal(0x5B, msg.CheckSum2);
        Assert.Equal("WW (D240, 00010001)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    //[Fact]
    //public void WriteWord2Response()
    //{
    //    var str = "02-30-35-46-46-30-30-30-32-23-5F-7F";
    //    var dt = str.ToHex();
    //    var v = dt.ReadBytes(1).ToStr();
    //    Assert.Equal("05FF0002\u0003B5", v);

    //    var msg = new FxLinksResponse();
    //    var rs = msg.Read(new MemoryStream(dt), null);
    //    Assert.True(rs);

    //    Assert.Equal(ControlCodes.STX, msg.Code);
    //    Assert.Equal(5, msg.Host);
    //    Assert.Equal(0xFF, msg.PC);
    //    Assert.Equal("0002", msg.Payload.ToHex());
    //    Assert.Equal(0, msg.CheckSum);
    //    Assert.Equal(0, msg.CheckSum2);
    //    Assert.Equal("ACK ()", msg.ToString());

    //    var pk = msg.ToPacket();
    //    Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    //}

    [Fact]
    public void WriteWord3()
    {
        // 05FFWW0D02120001003C6F
        var str = "05-30-35-46-46-57-57-30-44-30-32-31-32-30-30-30-31-30-30-33-43-36-46";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FFWW0D02120001003C6F", v);

        var msg = new FxLinksMessage();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D212", msg.Address);
        Assert.Equal("0001003C", msg.Payload.ToHex());
        Assert.Equal(0x6F, msg.CheckSum);
        Assert.Equal(0x6F, msg.CheckSum2);
        Assert.Equal("WW (D212, 0001003C)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    //[Fact]
    //public void WriteWord3Response()
    //{
    //    // 05FF0
    //    var str = "02-30-35-46-46-30-5F-7D";
    //    var dt = str.ToHex();
    //    var v = dt.ReadBytes(1).ToStr();
    //    Assert.Equal("05FF0\u0003B5", v);

    //    var msg = new FxLinksResponse();
    //    var rs = msg.Read(new MemoryStream(dt), null);
    //    Assert.True(rs);

    //    Assert.Equal(ControlCodes.STX, msg.Code);
    //    Assert.Equal(5, msg.Host);
    //    Assert.Equal(0xFF, msg.PC);
    //    Assert.Null(msg.Payload);
    //    Assert.Equal(0, msg.CheckSum);
    //    Assert.Equal(0, msg.CheckSum2);
    //    Assert.Equal("ACK ()", msg.ToString());

    //    var pk = msg.ToPacket();
    //    Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    //}

    [Fact]
    public void ReadBitResponse()
    {
        // STX-05FF0-ETX-24
        var str = "02-30-35-46-46-30-03-32-34";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1).ToStr();
        Assert.Equal("05FF0\u000324", v);

        var msg = new FxLinksResponse();
        var rs = msg.Read(new MemoryStream(dt), null);
        Assert.True(rs);

        Assert.Equal(ControlCodes.STX, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("00", msg.Payload.ToHex());
        Assert.Equal(0x24, msg.CheckSum);
        Assert.Equal(0x24, msg.CheckSum2);
        Assert.Equal("STX (00)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void CreateReply()
    {
        var msg = new FxLinksMessage { Command = "WW" };
        var rs = msg.CreateReply();

        //Assert.True(rs.Reply);
        //Assert.Equal(msg.Command, rs.Command);
        Assert.NotNull(rs);
        Assert.Equal(msg.PLC, rs.PC);
        Assert.Equal(msg.Station, rs.Host);
    }

    //[Fact]
    //public void Set()
    //{
    //    var msg = new FxLinksMessage { Code = FunctionCodes.WriteRegister };

    //    msg.SetRequest(0x0002, 0xABCD);

    //    Assert.Equal("00-02-AB-CD", msg.Payload.ToHex(256, "-"));
    //}
}