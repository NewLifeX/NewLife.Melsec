using System.IO;
using NewLife;
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
        Assert.Equal(01, msg.Payload[0]);
        Assert.Equal(0x32, msg.CheckSum);
        Assert.Equal(0x32, msg.CheckSum2);
        Assert.False(msg.Reply);
        Assert.Equal("WR (D210, 01)", msg.ToString());

        var pk = msg.ToPacket();
        Assert.Equal(dt.ToHex("-"), pk.ToHex(256, "-"));
    }

    //[Fact]
    //public void Test2()
    //{
    //    var str = "01-05-00-02-00-00-6C-0A";
    //    var dt = str.ToHex();

    //    var msg = new FxLinksMessage { Reply = true };
    //    msg.Read(new MemoryStream(dt), null);
    //    Assert.NotNull(msg);

    //    Assert.Equal(1, msg.Host);
    //    Assert.True(msg.Reply);
    //    Assert.Equal(FunctionCodes.WriteCoil, msg.Code);
    //    Assert.Equal((ErrorCodes)0, msg.ErrorCode);
    //    Assert.Equal(0x02, msg.GetAddress());
    //    Assert.Equal(0x0000, msg.Payload.ReadBytes(2, 2).ToUInt16(0, false));
    //    Assert.Equal("WriteCoil 000200006C0A", msg.ToString());

    //    var ms = new MemoryStream();
    //    msg.Write(ms, null);
    //    Assert.Equal(str, ms.ToArray().ToHex("-"));
    //}

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