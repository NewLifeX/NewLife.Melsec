﻿using NewLife;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

public class FxLinksMessageTests
{
    [Fact]
    public void Test1()
    {
        // 05FFWR0D02100132
        var str = "05 30 35 46 46 57 52 30 44 30 32 31 30 30 31 33 32";
        var dt = str.ToHex();

        var msg = FxLinksMessage.Read(dt, false);
        Assert.NotNull(msg);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("WR", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D210", msg.Address);
        Assert.Equal("01", msg.Payload.ToHex());
        Assert.Equal(0x32, msg.CheckSum);
        Assert.Equal(0x32, msg.CheckSum2);
        Assert.False(msg.Reply);
        Assert.Equal("WR (D210, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void Test2()
    {
        // 05FF0001\03B5
        var str = "02 30 35 46 46 30 30 30 31 03 42 35";
        var dt = str.ToHex();

        var msg = FxLinksMessage.Read(dt, false);
        Assert.NotNull(msg);

        Assert.Equal(ControlCodes.STX, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Null(msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Null(msg.Address);
        Assert.Equal("0001", msg.Payload.ToHex());
        Assert.Equal(0xB5, msg.CheckSum);
        Assert.Equal(0xB5, msg.CheckSum2);
        Assert.False(msg.Reply);
        Assert.Equal("STX (0001)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    [Fact]
    public void Test3()
    {
        // 05FFWW0D0210010001F8
        var str = "05 30 35 46 46 57 57 30 44 30 32 31 30 30 31 30 30 30 31 46 38";
        var dt = str.ToHex();

        var msg = FxLinksMessage.Read(dt, false);
        Assert.NotNull(msg);

        Assert.Equal(ControlCodes.ENQ, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Equal("WW", msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Equal("D210", msg.Address);
        Assert.Equal("010001", msg.Payload.ToHex());
        Assert.Equal(0xF8, msg.CheckSum);
        Assert.Equal(0xF8, msg.CheckSum2);
        Assert.False(msg.Reply);
        Assert.Equal("WW (D210, 010001)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }
    [Fact]
    public void Test4()
    {
        // 05FF
        var str = "06 30 35 46 46";
        var dt = str.ToHex();

        var msg = FxLinksMessage.Read(dt, false);
        Assert.NotNull(msg);

        Assert.Equal(ControlCodes.ACK, msg.Code);
        Assert.Equal(5, msg.Host);
        Assert.Equal(0xFF, msg.PC);
        Assert.Null(msg.Command);
        Assert.Equal(0, msg.Wait);
        Assert.Null(msg.Address);
        Assert.Null(msg.Payload);
        Assert.Equal(0, msg.CheckSum);
        Assert.Equal(0, msg.CheckSum2);
        Assert.True(msg.Reply);
        Assert.Equal("WR (D210, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }


    [Fact]
    public void CreateReply()
    {
        var msg = new FxLinksMessage { Command = "WW" };
        var rs = msg.CreateReply();

        Assert.True(rs.Reply);
        Assert.Equal(msg.Command, rs.Command);
    }

    //[Fact]
    //public void Set()
    //{
    //    var msg = new FxLinksMessage { Code = FunctionCodes.WriteRegister };

    //    msg.SetRequest(0x0002, 0xABCD);

    //    Assert.Equal("00-02-AB-CD", msg.Payload.ToHex(256, "-"));
    //}
}