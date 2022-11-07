using System;
using Moq;
using NewLife;
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
                Payload = "12-34-56-78".ToHex()
            });

        var link = mockFxLinks.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Read("BT", 1, "D202", 1));

        // 读取
        var rs = link.Read("BR", 1, "D202", 1);
        Assert.NotNull(rs);
        Assert.Equal(0x1234, rs.ReadUInt16(false));

        rs = link.Read("WR", 1, "D202", 2);
        Assert.NotNull(rs);
        Assert.Equal(0x12345678u, rs.ReadUInt32(false));
    }

    [Fact]
    public void ReadBit()
    {
        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Payload = "12-34".ToHex()
            });

        var link = mockFxLinks.Object;

        // 读取
        var rs = link.ReadBit(1, "D01", 16);
        Assert.NotNull(rs);

        var buf = rs.ReadBytes();
        Assert.Equal(2, buf.Length);
        Assert.Equal(0x1234, buf.ToUInt16(0, false));
    }

    [Fact]
    public void ReadWord()
    {
        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Payload = "12-34-56-78".ToHex()
            });

        var link = mockFxLinks.Object;

        // 读取
        var rs = link.ReadWord(1, "D202", 2);
        Assert.NotNull(rs);

        var buf = rs.ReadBytes();
        Assert.Equal(4, buf.Length);
        Assert.Equal(0x12345678u, buf.ToUInt32(0, false));
    }

    [Fact]
    public void Write()
    {
        // 模拟FxLinks。CallBase 指定调用基类方法
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Write("WT", 1, "D202", new UInt16[] { 1 }));

        var rs = (Int32)link.Write("WW", 1, "D202", new UInt16[] { 1 });
        Assert.NotEqual(-1, rs);
        Assert.Equal(0, rs);
    }

    [Fact]
    public void WriteBit()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteBit(1, "D202", 0xFF00);
        Assert.Equal(0, rs);
    }

    [Fact]
    public void WriteWord()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksMessage
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteWord(1, "D202", 0x1234);
        Assert.Equal(0, rs);
    }
}