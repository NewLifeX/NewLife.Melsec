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
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = "1234ABCD"
            });

        var link = mockFxLinks.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Read("BT", 1, "D202", 1));

        // 读取
        var rs = link.Read("BR", 1, "D202", 1) as Byte[];
        Assert.NotNull(rs);
        Assert.Equal(1, rs[0]);
        Assert.Equal(2, rs[1]);
        Assert.Equal(3, rs[2]);
        Assert.Equal(4, rs[3]);

        var rs2 = link.Read("WR", 1, "D202", 2) as UInt16[];
        Assert.NotNull(rs2);
        Assert.Equal(0x1234u, rs2[0]);
        Assert.Equal(0xabcdu, rs2[1]);
    }

    [Fact]
    public void ReadBit()
    {
        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = "1234"
            });

        var link = mockFxLinks.Object;

        // 读取
        var rs = link.ReadBit(1, "D01", 16);
        Assert.NotNull(rs);

        Assert.Equal(4, rs.Length);
        Assert.Equal(1, rs[0]);
        Assert.Equal(2, rs[1]);
        Assert.Equal(3, rs[2]);
        Assert.Equal(4, rs[3]);
    }

    [Fact]
    public void ReadWord()
    {
        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = "1234ABCD"
            });

        var link = mockFxLinks.Object;

        // 读取
        var rs = link.ReadWord(1, "D202", 2);
        Assert.NotNull(rs);

        Assert.Equal(2, rs.Length);
        Assert.Equal(0x1234u, rs[0]);
        Assert.Equal(0xabcdu, rs[1]);
    }

    [Fact]
    public void Write()
    {
        // 模拟FxLinks。CallBase 指定调用基类方法
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        Assert.Equal("FxLinksProxy", link.Name);

        Assert.Throws<NotSupportedException>(() => link.Write("WT", 1, "D202", new UInt16[] { 1 }));

        var rs = (Int32)link.Write("WW", 1, "D202", new UInt16[] { 1 });
        Assert.NotEqual(-1, rs);
        Assert.Equal(1, rs);
    }

    [Fact]
    public void WriteBit()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteBit(1, "D202", 0xFF00);
        Assert.Equal(1, rs);
    }

    [Fact]
    public void WriteWord()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        mb.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Code = ControlCodes.ACK,
            });

        var link = mb.Object;

        // 读取
        var rs = link.WriteWord(1, "D202", 0x1234);
        Assert.Equal(1, rs);
    }
}