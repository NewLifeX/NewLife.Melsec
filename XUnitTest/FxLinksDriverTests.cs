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

        // 模拟FxLinks
        var mb = new Mock<FxLinks>();
        mb.Setup(e => e.ReadWord(1, "D0", It.IsAny<Byte>()))
            .Returns(new UInt16[] { 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd, 0x1234, 0x5678, 0xabcd, 0x90cd });
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = "D" + i,
                Length = 2
            };

            points.Add(pt);
        }

        // 读取
        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Count);

        for (var i = 0; i < 10; i++)
        {
            var name = "p" + i;
            Assert.True(rs.ContainsKey(name));
        }
    }

    [Fact]
    public void ReadBitTest()
    {
        var driver = new FxLinksDriver();

        var p = new FxLinksParameter { Host = 5 };
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);

        // 模拟FxLinks
        var mb = new Mock<FxLinks> { CallBase = true };
        mb.Setup(e => e.SendCommand("BR", 5, "Y0", "08"))
            .Returns(new FxLinksResponse { Code = ControlCodes.ACK, Payload = "11111010" });
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 8; i++)
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = "Y" + i,
            };

            points.Add(pt);
        }

        // 读取
        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(8, rs.Count);

        for (var i = 0; i < 8; i++)
        {
            var name = "p" + i;
            Assert.True(rs.ContainsKey(name));
        }

        Assert.Equal((Byte)1, rs["p0"]);
        Assert.Equal((Byte)1, rs["p1"]);
        Assert.Equal((Byte)1, rs["p2"]);
        Assert.Equal((Byte)1, rs["p3"]);
        Assert.Equal((Byte)1, rs["p4"]);
        Assert.Equal((Byte)0, rs["p5"]);
        Assert.Equal((Byte)1, rs["p6"]);
        Assert.Equal((Byte)0, rs["p7"]);
    }

    [Fact]
    public void ReadRegister()
    {
        var driver = new FxLinksDriver();

        var p = driver.CreateParameter(null) as FxLinksParameter;

        var node = driver.Open(null, p);

        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = e.Address == "D100" ? "1234" : "abCD",
            });

        driver.Link = mockFxLinks.Object;

        var points = new List<IPoint>
        {
            new PointModel
            {
                Name = "调节池运行时间",
                Address = "D100",
                Length = 2
            },
            new PointModel
            {
                Name = "调节池停止时间",
                Address = "D102",
                Length = 2
            }
        };

        // 读取
        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(2, rs.Count);

        Assert.Equal(0x1234, (UInt16)rs["调节池运行时间"]);
        Assert.Equal(0xabcd, (UInt16)rs["调节池停止时间"]);
    }

    [Fact]
    public void Write()
    {
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Code = ControlCodes.ACK,
            });

        var driver = new FxLinksDriver();
        driver.Link = mockFxLinks.Object;

        var node = driver.Open(null, new FxLinksParameter());

        var pt = new PointModel
        {
            Name = "调节池运行时间",
            Address = "D100",
            Type = "short",
            Length = 2
        };

        var rs = (Int32)driver.Write(node, pt, "15");
        Assert.Equal(1, rs);
    }

    [Fact]
    public void BuildSegments()
    {
        var driver = new FxLinksDriver();

        // 10个点位
        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = "X" + i,
                Length = 2
            };

            points.Add(pt);
        }

        // 凑批成为一个
        var segs = driver.BuildSegments(points, new FxLinksParameter());
        Assert.Equal(1, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(10, segs[0].Count);

        // 每4个一批，凑成3批
        segs = driver.BuildSegments(points, new FxLinksParameter { BatchSize = 4 });
        Assert.Equal(3, segs.Count);
        Assert.Equal(4, segs[1].Address);
        Assert.Equal(4, segs[1].Count);
    }

    [Fact]
    public void BuildSegmentsOnBit()
    {
        var driver = new FxLinksDriver();

        // 10个点位
        var points = new List<IPoint>
        {
            new PointModel { Name = "p0", Address = "Y0", },
            new PointModel { Name = "p2", Address = "Y2", },
            new PointModel { Name = "p4", Address = "Y4", },
            new PointModel { Name = "p8", Address = "Y8", },
            new PointModel { Name = "p16", Address = "Y10", },
            new PointModel { Name = "p20", Address = "Y14", }
        };

        // 无法合并
        var segs = driver.BuildSegments(points, new FxLinksParameter());
        Assert.Equal(6, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(1, segs[0].Count);

        // 凑批成为一个
        segs = driver.BuildSegments(points, new FxLinksParameter { BatchStep = 4 });
        Assert.Equal(1, segs.Count);
        Assert.Equal(0, segs[0].Address);
        Assert.Equal(15, segs[0].Count);

        // 每4个一批，凑成3批
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

        // 模拟
        var mb = new Mock<FxLinks>();
        //mb.Setup(e => e.Read("BR", 1, "M0", 8))
        //    .Returns("12-34-56-78-90-12-34-56-78-90-12-34-56-78-90-12".ToHex());
        mb.Setup(e => e.ReadBit(1, "M0", 8))
            .Returns("12-34-56-78-90-12-34-56-78-90-12-34-56-78-90-12".ToHex());
        mb.Setup(e => e.ReadBit(1, "M8", 2))
            .Returns("78-90-12-34-56-78-90-12".ToHex());
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = "M" + i,
            };

            points.Add(pt);
        }

        // 打断
        p.BatchSize = 8;

        // 读取
        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Count);

        for (var i = 0; i < 10; i++)
        {
            var name = "p" + i;
            Assert.True(rs.ContainsKey(name));
        }
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
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = "D" + i,
                Length = 2
            };

            points.Add(pt);
        }

        // 打断
        p.BatchSize = 4;

        // 读取
        var rs = driver.Read(node, points.ToArray());
        Assert.NotNull(rs);
        Assert.Equal(10, rs.Count);

        for (var i = 0; i < 10; i++)
        {
            var name = "p" + i;
            Assert.True(rs.ContainsKey(name));
        }
    }
}