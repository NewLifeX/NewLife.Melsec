using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NewLife;
using NewLife.IoT.Drivers;
using NewLife.IoT.ThingModels;
using NewLife.Melsec.Drivers;
using NewLife.Melsec.Protocols;
using NewLife.Security;
using Xunit;

namespace XUnitTest;

public class FxLinksDriverTests
{
    [Fact]
    public void OpenTest()
    {
        var driver = new FxLinksDriver();

        var p = new FxLinksParameter();
        Rand.Fill(p);
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);

        var node2 = node as MelsecNode;
        Assert.NotNull(node2);
        Assert.Null(node2.Device);
    }

    [Fact]
    public void CloseTest()
    {
        var driver = new FxLinksDriver();

        var p = new FxLinksParameter();
        Rand.Fill(p);
        var dic = p.ToDictionary();

        var node1 = driver.Open(null, dic) as MelsecNode;

        var node2 = driver.Open(null, dic) as MelsecNode;
        Assert.NotEqual(node1, node2);
        Assert.Equal(node1.Driver, node2.Driver);

        driver.Close(node1);
        driver.Close(node2);
    }

    [Fact]
    public void ReadTest()
    {
        var driver = new FxLinksDriver();

        var p = driver.CreateParameter(null) as FxLinksParameter;
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);

        // Mock FxLinks
        var mb = new Mock<FxLinks>();
        mb.Setup(e => e.ReadWord(1, "D0", It.IsAny<Byte>()))
            .Returns(new UInt16[] { 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd });
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
        {
            points.Add(new PointModel { Name = "p" + i, Address = "D" + i, Length = 2 });
        }

        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Points.Length);

        for (var i = 0; i < 10; i++)
            Assert.NotNull(rs.GetValue("p" + i));
    }

    [Fact]
    public void ReadBitTest()
    {
        var driver = new FxLinksDriver();

        var p = new FxLinksParameter { Host = 5 };
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);

        // Mock FxLinks
        var mb = new Mock<FxLinks> { CallBase = true };
        mb.Setup(e => e.SendCommand("BR", 5, "Y0", "08"))
            .Returns(new FxLinksResponse { Code = ControlCodes.ACK, Payload = "11111010" });
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 8; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "Y" + i });

        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(8, rs.Points.Length);

        for (var i = 0; i < 8; i++)
            Assert.NotNull(rs.GetValue("p" + i));

        Assert.Equal((Byte)1, rs.GetValue("p0"));
        Assert.Equal((Byte)1, rs.GetValue("p1"));
        Assert.Equal((Byte)1, rs.GetValue("p2"));
        Assert.Equal((Byte)1, rs.GetValue("p3"));
        Assert.Equal((Byte)1, rs.GetValue("p4"));
        Assert.Equal((Byte)0, rs.GetValue("p5"));
        Assert.Equal((Byte)1, rs.GetValue("p6"));
        Assert.Equal((Byte)0, rs.GetValue("p7"));
    }

    [Fact]
    public void ReadRegister()
    {
        var driver = new FxLinksDriver();

        var p = driver.CreateParameter(null) as FxLinksParameter;

        var node = driver.Open(null, p);

        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = e.Address == "D100" ? "1234" : "abCD",
            });

        driver.Link = mockFxLinks.Object;

        var points = new List<IPoint>
        {
            new PointModel { Name = "调节池运行时间", Address = "D100", Length = 2 },
            new PointModel { Name = "调节池停止时间", Address = "D102", Length = 2 }
        };

        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(2, rs.Points.Length);

        Assert.Equal(0x1234, (UInt16)rs.GetValue("调节池运行时间"));
        Assert.Equal(0xabcd, (UInt16)rs.GetValue("调节池停止时间"));
    }

    [Fact]
    public void Write()
    {
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse { Code = ControlCodes.ACK });

        var driver = new FxLinksDriver();
        driver.Link = mockFxLinks.Object;

        var node = driver.Open(null, new FxLinksParameter());

        var pt = new PointModel { Name = "调节池运行时间", Address = "D100", Type = "short", Length = 2 };

        var rs = driver.Write(node, pt, "15");
        Assert.True(rs.IsSuccess);
        Assert.Equal(1, rs.AffectedCount);
    }

    [Fact]
    public void BuildSegments()
    {
        var driver = new FxLinksDriver();

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "X" + i, Length = 2 });

        var segs = driver.BuildSegments(points, new FxLinksParameter());
        Assert.Equal(1, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(10, segs[0].Count);

        segs = driver.BuildSegments(points, new FxLinksParameter { BatchSize = 4 });
        Assert.Equal(3, segs.Count);
        Assert.Equal(4, segs[1].Address);
        Assert.Equal(4, segs[1].Count);
    }

    [Fact]
    public void BuildSegmentsOnBit()
    {
        var driver = new FxLinksDriver();

        var points = new List<IPoint>
        {
            new PointModel { Name = "p0",  Address = "Y0"  },
            new PointModel { Name = "p2",  Address = "Y2"  },
            new PointModel { Name = "p4",  Address = "Y4"  },
            new PointModel { Name = "p8",  Address = "Y8"  },
            new PointModel { Name = "p16", Address = "Y10" },
            new PointModel { Name = "p20", Address = "Y14" }
        };

        var segs = driver.BuildSegments(points, new FxLinksParameter());
        Assert.Equal(6, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(1, segs[0].Count);

        segs = driver.BuildSegments(points, new FxLinksParameter { BatchStep = 4 });
        Assert.Equal(1, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(15, segs[0].Count);

        segs = driver.BuildSegments(points, new FxLinksParameter { BatchStep = 4, BatchSize = 4 });
        Assert.Equal(2, segs.Count);
        Assert.Equal(10, segs[1].Address);
        Assert.Equal(5, segs[1].Count);
    }

    [Fact]
    public void ReadWithBatch()
    {
        var driver = new FxLinksDriver();

        var p = driver.CreateParameter(null) as FxLinksParameter;
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);
        p = node.Parameter as FxLinksParameter;

        var mb = new Mock<FxLinks>();
        mb.Setup(e => e.ReadBit(1, "M0", 8))
            .Returns("12-34-56-78-90-12-34-56-78-90-12-34-56-78-90-12".ToHex());
        mb.Setup(e => e.ReadBit(1, "M8", 2))
            .Returns("78-90-12-34-56-78-90-12".ToHex());
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "M" + i });

        p.BatchSize = 8;

        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Points.Length);

        for (var i = 0; i < 10; i++)
            Assert.NotNull(rs.GetValue("p" + i));
    }

    [Fact]
    public void ReadWithBatch2()
    {
        var driver = new FxLinksDriver();

        var p = driver.CreateParameter(null) as FxLinksParameter;
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);
        p = node.Parameter as FxLinksParameter;

        var mb = new Mock<FxLinks>();
        mb.Setup(e => e.ReadWord(1, "D0", It.IsAny<Byte>()))
            .Returns(new UInt16[] { 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd });
        mb.Setup(e => e.ReadWord(1, "D4", It.IsAny<Byte>()))
            .Returns(new UInt16[] { 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd });
        mb.Setup(e => e.ReadWord(1, "D8", It.IsAny<Byte>()))
            .Returns(new UInt16[] { 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd });
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "D" + i, Length = 2 });

        p.BatchSize = 4;

        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Points.Length);

        for (var i = 0; i < 10; i++)
            Assert.NotNull(rs.GetValue("p" + i));
    }

    [Fact]
    public void WriteBitDevice_MAddress_CallsWriteBit()
    {
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns(new FxLinksResponse { Code = ControlCodes.ACK });

        var driver = new FxLinksDriver();
        driver.Link = mockFxLinks.Object;

        var node = driver.Open(null, new FxLinksParameter { Host = 1 });

        var pt = new PointModel { Name = "M5", Address = "M5", Type = "bool" };
        var result = driver.Write(node, pt, true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AffectedCount);
        mockFxLinks.Verify(e => e.SendCommand(It.Is<FxLinksMessage>(m => m.Command == "BW")), Times.Once);
    }

    [Fact]
    public void WriteBitDevice_YAddress_CallsWriteBit()
    {
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns(new FxLinksResponse { Code = ControlCodes.ACK });

        var driver = new FxLinksDriver();
        driver.Link = mockFxLinks.Object;

        var node = driver.Open(null, new FxLinksParameter { Host = 1 });

        var pt = new PointModel { Name = "Y0", Address = "Y0", Type = "bool" };
        var result = driver.Write(node, pt, false);

        Assert.True(result.IsSuccess);
        mockFxLinks.Verify(e => e.SendCommand(It.Is<FxLinksMessage>(m => m.Command == "BW")), Times.Once);
    }

    [Fact]
    public void WriteWordDevice_DAddress_CallsWriteWord()
    {
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns(new FxLinksResponse { Code = ControlCodes.ACK });

        var driver = new FxLinksDriver();
        driver.Link = mockFxLinks.Object;

        var node = driver.Open(null, new FxLinksParameter { Host = 1 });

        var pt = new PointModel { Name = "D10", Address = "D10", Type = "short" };
        var result = driver.Write(node, pt, (Int32)0x5A5A);

        Assert.True(result.IsSuccess);
        mockFxLinks.Verify(e => e.SendCommand(It.Is<FxLinksMessage>(m => m.Command == "WW")), Times.Once);
    }
}
