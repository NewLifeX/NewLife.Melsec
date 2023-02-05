using System;
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
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("BR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        Assert.Equal("01", msg.Payload);
        Assert.Equal(0x27, msg.CheckSum);
        Assert.Equal(0x27, msg.CheckSum2);
        Assert.Equal("BR (M103, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "02 30 35 46 46 31 03 32 35";
        dt = str.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FF1\u000325", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal("1", rs.Payload);
        Assert.Equal(0x25, rs.CheckSum);
        Assert.Equal(0x25, rs.CheckSum2);
        Assert.Equal("STX (1)", rs.ToString());

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
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        Assert.Equal("01", msg.Payload);
        Assert.Equal(0x3C, msg.CheckSum);
        Assert.Equal(0x3C, msg.CheckSum2);
        Assert.Equal("WR (M103, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "02 30 35 46 46 31 03 32 35";
        dt = str.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FF1\u000325", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal("1", rs.Payload);
        Assert.Equal(0x25, rs.CheckSum);
        Assert.Equal(0x25, rs.CheckSum2);
        Assert.Equal("STX (1)", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Theory]
    [InlineData("05 30 35 46 46 42 57 30 4D 30 31 30 33 30 31 31 35 44", "05FFBW0M01030115D", 1)]
    [InlineData("05 30 35 46 46 42 57 30 4D 30 31 30 33 30 31 30 35 43", "05FFBW0M01030105C", 0)]
    public void WriteBitM103(String str, String hex, Int32 result)
    {
        //var str = "05 30 35 46 46 42 57 30 4D 30 31 30 33 30 31 31 35 44";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FFBW0M01030115D", v);
        Assert.Equal(hex, v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("BW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);

        if (result == 1)
        {
            Assert.Equal("011", msg.Payload);
            Assert.Equal(0x5D, msg.CheckSum);
            Assert.Equal(0x5D, msg.CheckSum2);
            Assert.Equal("BW (M103, 011)", msg.ToString());
        }
        else
        {
            Assert.Equal("010", msg.Payload);
            Assert.Equal(0x5C, msg.CheckSum);
            Assert.Equal(0x5C, msg.CheckSum2);
            Assert.Equal("BW (M103, 010)", msg.ToString());
        }

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "06 30 35 46 46";
        dt = str.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FF", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ACK, rs.Code);
        Assert.Equal("ACK ()", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Theory]
    [InlineData("05 30 35 46 46 57 57 30 4D 30 31 30 33 30 31 30 30 30 31 30 32", "05FFWW0M010301000102", 1)]
    [InlineData("05 30 35 46 46 57 57 30 4D 30 31 30 33 30 31 30 30 30 30 30 31", "05FFWW0M010301000001", 0)]
    public void WriteWordM103(String str, String hex, Int32 result)
    {
        //var str = "05 30 35 46 46 57 57 30 4D 30 31 30 33 30 31 30 30 30 31 30 32";
        var dt = str.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FFWW0M010301000102", v);
        Assert.Equal(hex, v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("M103", msg.Address);
        if (result == 1)
        {
            Assert.Equal("010001", msg.Payload);
            Assert.Equal(0x02, msg.CheckSum);
            Assert.Equal(0x02, msg.CheckSum2);
            Assert.Equal("WW (M103, 010001)", msg.ToString());
        }
        else
        {
            Assert.Equal("010000", msg.Payload);
            Assert.Equal(0x01, msg.CheckSum);
            Assert.Equal(0x01, msg.CheckSum2);
            Assert.Equal("WW (M103, 010000)", msg.ToString());
        }

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        str = "06 30 35 46 46";
        dt = str.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FF", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ACK, rs.Code);
        Assert.Equal("ACK ()", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Theory]
    [InlineData("M103", 4, "05 30 35 46 46 42 52 30 4D 30 31 30 33 30 34 32 41", "02 30 35 46 46 30 30 30 30 03 42 34", new Byte[] { 0, 0, 0, 0 })]
    [InlineData("X2", 4, "05 30 35 46 46 42 52 30 58 30 30 30 32 30 34 33 33", "02 30 35 46 46 30 30 30 30 03 42 34", new Byte[] { 0, 0, 0, 0 })]
    [InlineData("Y1", 7, "05 30 35 46 46 42 52 30 59 30 30 30 31 30 37 33 36", "02 30 35 46 46 30 30 30 30 30 30 30 03 34 34", new Byte[] { 0, 0, 0, 0, 0, 0, 0 })]
    public void ReadBit(String address, UInt16 count, String request, String response, Byte[] result)
    {
        var dt = request.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FFBR0M01030127", v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("BR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal(address, msg.Address);
        Assert.Equal($"0{count}", msg.Payload);
        Assert.Equal($"BR ({address}, 0{count})", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        dt = response.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FF1\u000325", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        var resultStr = result.Join("");
        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal(resultStr, rs.Payload);
        Assert.Equal($"STX ({resultStr})", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Theory]
    [InlineData("M103", 4, "05 30 35 46 46 57 52 30 4D 30 31 30 33 30 34 33 46", "02 30 35 46 46 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 30 03 46 34", new UInt16[] { 0, 0, 0, 0 })]
    [InlineData("D232", 1, "05 30 35 46 46 57 52 30 44 30 32 33 32 30 31 33 36", "02 30 35 46 46 30 30 30 35 03 42 39", new UInt16[] { 5 })]
    [InlineData("D232", 2, "05 30 35 46 46 57 52 30 44 30 32 33 32 30 32 33 37", "02 30 35 46 46 30 30 30 35 30 30 30 35 03 37 45", new UInt16[] { 5, 5 })]
    [InlineData("D230", 4, "05 30 35 46 46 57 52 30 44 30 32 33 30 30 34 33 37", "02 30 35 46 46 30 30 30 30 30 30 30 30 30 30 30 38 30 30 30 39 03 30 35", new UInt16[] { 0, 0, 8, 9 })]
    public void ReadWord(String address, UInt16 count, String request, String response, UInt16[] result)
    {
        var dt = request.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FFWR0M0103013C", v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal(address, msg.Address);
        Assert.Equal($"0{count}", msg.Payload);
        Assert.Equal($"WR ({address}, 0{count})", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        dt = response.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        var str = rs.Payload;
        var us = new UInt16[str.Length / 4];
        for (var i = 0; i < us.Length; i++)
        {
            us[i] = str.Substring(i * 4, 4).ToHex().ToUInt16(0, false);
        }

        var resultStr = result.Join("", e => e.ToString().PadLeft(4, '0'));
        Assert.Equal(ControlCodes.STX, rs.Code);
        Assert.Equal(resultStr, rs.Payload);
        Assert.Equal(result.Join("-"), us.Join("-"));
        Assert.Equal($"STX ({resultStr})", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Theory]
    [InlineData("D232", new UInt16[] { 7 }, "05 30 35 46 46 57 57 30 44 30 32 33 32 30 31 30 30 30 37 30 32", "05FFWW0D023201000702")]
    [InlineData("D232", new UInt16[] { 8, 9 }, "05 30 35 46 46 57 57 30 44 30 32 33 32 30 32 30 30 30 38 30 30 30 39 43 44", "05FFWW0D02320200080009CD")]
    [InlineData("D230", new UInt16[] { 1, 2, 6, 7 }, "05 30 35 46 46 57 57 30 44 30 32 33 30 30 34 30 30 30 31 30 30 30 32 30 30 30 36 30 30 30 37 34 43", "05FFWW0D02300400010002000600074C")]
    public void WriteWord(String address, UInt16[] values, String request, String hex)
    {
        var dt = request.ToHex();
        var v = dt.ReadBytes(1, -1).ToStr();
        //Assert.Equal("05FFWW0M010301000102", v);
        Assert.Equal(hex, v);

        var msg = new FxLinksMessage();
        var r = msg.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Station);
        Assert.Equal(0xFF, msg.PLC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal(address, msg.Address);

        var valueStr = values.Length.ToString().PadLeft(2, '0');
        valueStr += values.Join("", e => e.ToString().PadLeft(4, '0'));

        Assert.Equal(valueStr, msg.Payload);
        Assert.Equal($"WW ({address}, {valueStr})", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));

        var str = "06 30 35 46 46";
        dt = str.ToHex();
        v = dt.ReadBytes(1, -1).ToStr();
        Assert.Equal("05FF", v);

        var rs = msg.CreateReply();
        r = rs.Read(new MemoryStream(dt), null);
        Assert.True(r);

        Assert.Equal(ControlCodes.ACK, rs.Code);
        Assert.Equal("ACK ()", rs.ToString());

        pk = rs.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }
}