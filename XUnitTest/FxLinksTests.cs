using System;
using Moq;
using NewLife;
using NewLife.Data;
using NewLife.IoT.Protocols;
using NewLife.Melsec.Protocols;
using Xunit;

namespace XUnitTest;

public class FxLinksTests
{
    [Fact]
    public void Read()
    {
        // 模拟FxLinks。CallBase 指定调用基类方法
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Command = e.Command,
                Address = e.Address,
                Payload = e.Payload?.Slice(0, 2).Append(e.Payload.Slice(2, 2))
            });

        var link = mockFxLinks.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Read("BT", 1, "D202", 1));

        // 读取
        var rs = link.Read("BR", 1, "D202", 1);
        Assert.NotNull(rs);
        Assert.Equal(0x0200, rs.ReadBytes().ToUInt16(0, false));

        rs = link.Read("WR", 1, "D202", 2);
        Assert.NotNull(rs);
        Assert.Equal(0x01020304u, rs.ReadBytes().ToUInt32(0, false));
    }

    [Fact]
    public void ReadBit()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadCoil, 1, "D202", 2))
        //    .Returns("02-12-34-56-78".ToHex());

        var link = mb.Object;

        //// 读取
        //var rs = link.ReadCoil(1, 100, 2);
        //Assert.NotNull(rs);

        //var buf = rs.ReadBytes();
        //Assert.Equal(2, buf.Length);
        //Assert.Equal(0x1234, buf.ToUInt16(0, false));
    }

    [Fact]
    public void ReadWord()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadDiscrete, 1, "D202", 2))
        //    .Returns("02-12-34-56-78".ToHex());

        var link = mb.Object;

        // 读取
        var rs = link.ReadWord(1, "D202", 2);
        Assert.NotNull(rs);

        var buf = rs.ReadBytes();
        Assert.Equal(2, buf.Length);
        Assert.Equal(0x1234, buf.ToUInt16(0, false));
    }

    [Fact]
    public void Write()
    {
        // 模拟FxLinks。CallBase 指定调用基类方法
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Host = e.Host,
                Address = e.Address,
                Payload = e.Payload.Slice(0, 2).Append("03-04".ToHex())
            });

        var link = mb.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Write("WT", 1, "D202", new UInt16[] { 1 }));

        var rs = (Int32)link.Write("WW", 1, "D202", new UInt16[] { 1 });
        Assert.NotEqual(-1, rs);
        Assert.Equal(0x0304, rs);
    }

    [Fact]
    public void WriteBit()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Address = e.Address,
                Payload = e.Payload.Slice(0, 4)
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteBit(1, "D202", 0xFF00);
        Assert.Equal(0xFF00, rs);
    }

    [Fact]
    public void WriteWord()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Address = e.Address,
                Payload = e.Payload.Slice(0, 2).Append(e.Payload.Slice(2, 2))
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteWord(1, "D202", 0x1234);
        Assert.Equal(0x1234, rs);
    }
}