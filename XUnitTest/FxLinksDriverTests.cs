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

        var p = driver.GetDefaultParameter() as FxLinksParameter;
        var dic = p.ToDictionary();

        var node = driver.Open(null, dic);

        // 模拟FxLinks
        var mb = new Mock<FxLinks>();
        //mb.Setup(e => e.Read(FunctionCodes.ReadRegister, 1, 0, 10))
        //    .Returns("12-34-56-78-90-12-34-56-78-90-12-34-56-78-90-12-34-56-78-90".ToHex());
        driver.Link = mb.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 10; i++)
        {
            var pt = new PointModel
            {
                Name = "p" + i,
                Address = i + "",
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
    public void ReadRegister()
    {
        var driver = new FxLinksDriver();

        var p = driver.GetDefaultParameter() as FxLinksParameter;

        var node = driver.Open(null, p);

        // 模拟FxLinks
        var mockFxLinks = new Mock<FxLinks> { CallBase = true };
        mockFxLinks.Setup(e => e.SendCommand(It.IsAny<FxLinksMessage>()))
            .Returns<FxLinksMessage>(e => new FxLinksResponse
            {
                Payload = e.Address == "D100" ? "12-34".ToHex() : "56-78".ToHex(),
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

        Assert.Equal(0x1234, (rs["调节池运行时间"] as Byte[]).ToUInt16(0, false));
        Assert.Equal(0x5678, (rs["调节池停止时间"] as Byte[]).ToUInt16(0, false));
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
        Assert.Equal(0, rs);
    }
}