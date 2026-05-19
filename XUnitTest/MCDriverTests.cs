using System;
using System.Collections.Generic;
using System.ComponentModel;
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

/// <summary>MCDriver 驱动层测试（使用 Moq 模拟 MCProtocol）</summary>
public class MCDriverTests
{
    #region Open / Close

    [Fact]
    [DisplayName("Open 返回 MelsecNode 且 Link 被初始化")]
    public void Open_ReturnsMelsecNode()
    {
        var driver = new MCDriver();

        var p = new MCParameter { Address = "192.168.1.10:6000" };
        var node = driver.Open(null, p);

        var melsecNode = node as MelsecNode;
        Assert.NotNull(melsecNode);
        Assert.Null(melsecNode.Device);
        Assert.NotNull(driver.Link);
    }

    [Fact]
    [DisplayName("Open 多次打开同地址共享同一 Link")]
    public void Open_MultipleNodes_ShareSameLink()
    {
        var driver = new MCDriver();

        var p = new MCParameter { Address = "192.168.1.10:6000" };
        var node1 = driver.Open(null, p) as MelsecNode;
        var link1 = driver.Link;

        var node2 = driver.Open(null, p) as MelsecNode;
        var link2 = driver.Link;

        Assert.NotEqual(node1, node2);
        Assert.Same(link1, link2);
        Assert.Equal(node1.Driver, node2.Driver);

        driver.Close(node1);
        driver.Close(node2);
    }

    [Fact]
    [DisplayName("Close 引用计数减至0时释放 Link")]
    public void Close_RefCountZero_DisposesLink()
    {
        var driver = new MCDriver();

        var p = new MCParameter { Address = "192.168.1.10:6000" };
        var node1 = driver.Open(null, p);
        var node2 = driver.Open(null, p);

        var link = driver.Link;
        Assert.NotNull(link);

        driver.Close(node1);
        Assert.NotNull(driver.Link); // 还有一个节点

        driver.Close(node2);
        Assert.Null(driver.Link);    // 最后一个关闭后 Link 应为 null
    }

    [Fact]
    [DisplayName("CreateParameter 空字符串返回默认值")]
    public void CreateParameter_Null_ReturnsDefault()
    {
        var driver = new MCDriver();
        var p = driver.CreateParameter(null) as MCParameter;

        Assert.NotNull(p);
        Assert.Equal("192.168.1.10:6000", p.Address);
        Assert.Equal(5000, p.Timeout);
    }

    #endregion

    #region Read

    [Fact]
    [DisplayName("Read 字软元件 D100-D104 返回正确结果")]
    public void Read_WordRegisters_ReturnsReadResult()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        // 模拟 MCProtocol
        var mock = new Mock<MCProtocol> { CallBase = false };
        mock.Setup(e => e.ReadWords(DeviceCode.D, 100, It.IsAny<Int32>()))
            .Returns([0x0001, 0x0002, 0x0003, 0x0004, 0x0005]);
        driver.Link = mock.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 5; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "D" + (100 + i) });

        var rs = driver.Read(node, [.. points]);

        Assert.NotNull(rs);
        Assert.Equal(5, rs.Points.Length);
        Assert.Equal((UInt16)0x0001, rs.GetValue("p0"));
        Assert.Equal((UInt16)0x0002, rs.GetValue("p1"));
        Assert.Equal((UInt16)0x0005, rs.GetValue("p4"));
    }

    [Fact]
    [DisplayName("Read 位软元件 M200-M207 返回正确结果")]
    public void Read_BitDevices_ReturnsReadResult()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        // 模拟 MCProtocol
        var mock = new Mock<MCProtocol> { CallBase = false };
        mock.Setup(e => e.ReadBits(DeviceCode.M, 200, It.IsAny<Int32>()))
            .Returns([true, false, true, true, false, false, true, false]);
        driver.Link = mock.Object;

        var points = new List<IPoint>();
        for (var i = 0; i < 8; i++)
            points.Add(new PointModel { Name = "p" + i, Address = "M" + (200 + i) });

        var rs = driver.Read(node, [.. points]);

        Assert.Equal(8, rs.Points.Length);
        Assert.Equal(true,  rs.GetValue("p0"));
        Assert.Equal(false, rs.GetValue("p1"));
        Assert.Equal(true,  rs.GetValue("p2"));
        Assert.Equal(true,  rs.GetValue("p3"));
    }

    [Fact]
    [DisplayName("Read 空点位集合 返回空结果")]
    public void Read_EmptyPoints_ReturnsEmpty()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        var mock = new Mock<MCProtocol> { CallBase = false };
        driver.Link = mock.Object;

        var rs = driver.Read(node, []);
        Assert.NotNull(rs);
        Assert.Empty(rs.Points);
    }

    #endregion

    #region Write

    [Fact]
    [DisplayName("Write 字软元件 D100 调用 WriteWords 并返回成功")]
    public void Write_WordRegister_CallsWriteWords()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        var mock = new Mock<MCProtocol> { CallBase = false };
        mock.Setup(e => e.WriteWords(DeviceCode.D, 100, It.IsAny<UInt16[]>()));
        driver.Link = mock.Object;

        var pt = new PointModel { Name = "D100Test", Address = "D100", Type = "short" };
        var result = driver.Write(node, pt, (Int32)0x1234);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AffectedCount);
        mock.Verify(e => e.WriteWords(DeviceCode.D, 100, It.IsAny<UInt16[]>()), Times.Once);
    }

    [Fact]
    [DisplayName("Write 位软元件 M0 ON 调用 WriteBits 并返回成功")]
    public void Write_BitDevice_CallsWriteBits()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        var mock = new Mock<MCProtocol> { CallBase = false };
        mock.Setup(e => e.WriteBits(DeviceCode.M, 0, It.IsAny<Boolean[]>()));
        driver.Link = mock.Object;

        var pt = new PointModel { Name = "M0Test", Address = "M0" };
        var result = driver.Write(node, pt, true);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.AffectedCount);
        mock.Verify(e => e.WriteBits(DeviceCode.M, 0, It.IsAny<Boolean[]>()), Times.Once);
    }

    #endregion

    #region BuildSegments

    [Fact]
    [DisplayName("BuildSegments 相邻字地址合并为一个分段")]
    public void BuildSegments_AdjacentWordAddresses_MergeToOne()
    {
        var driver = new MCDriver();

        var points = Enumerable.Range(0, 5)
            .Select(i => (IPoint)new PointModel { Name = "p" + i, Address = "D" + (100 + i) })
            .ToList();

        var segs = driver.BuildSegments(points, new MCParameter());
        var seg0 = Assert.Single(segs);
        Assert.Equal(DeviceCode.D, seg0.Code);
        Assert.Equal(100, seg0.StartAddress);
        Assert.Equal(5, seg0.Count);
    }

    [Fact]
    [DisplayName("BuildSegments 不同软元件代码不合并")]
    public void BuildSegments_DifferentDeviceCodes_NotMerged()
    {
        var driver = new MCDriver();

        var points = new List<IPoint>
        {
            new PointModel { Name = "d0", Address = "D100" },
            new PointModel { Name = "d1", Address = "D101" },
            new PointModel { Name = "m0", Address = "M0" },
            new PointModel { Name = "m1", Address = "M1" },
        };

        var segs = driver.BuildSegments(points, new MCParameter());
        Assert.Equal(2, segs.Count);
    }

    [Fact]
    [DisplayName("BuildSegments 按 BatchSize 拆分多批")]
    public void BuildSegments_BatchSize_SplitsSegments()
    {
        var driver = new MCDriver();

        var points = Enumerable.Range(0, 10)
            .Select(i => (IPoint)new PointModel { Name = "p" + i, Address = "D" + (100 + i) })
            .ToList();

        // BatchSize=4: 前4个一批，5-8一批，最后2个一批
        var segs = driver.BuildSegments(points, new MCParameter { BatchSize = 4 });
        Assert.Equal(3, segs.Count);
        Assert.Equal(4, segs[0].Count);
        Assert.Equal(104, segs[1].StartAddress);
        Assert.Equal(4, segs[1].Count);
        Assert.Equal(108, segs[2].StartAddress);
        Assert.Equal(2, segs[2].Count);
    }

    [Fact]
    [DisplayName("BuildSegments 地址不连续不合并")]
    public void BuildSegments_NonAdjacentAddresses_NotMerged()
    {
        var driver = new MCDriver();

        var points = new List<IPoint>
        {
            new PointModel { Name = "p0", Address = "D100" },
            new PointModel { Name = "p1", Address = "D200" },
        };

        var segs = driver.BuildSegments(points, new MCParameter());
        Assert.Equal(2, segs.Count);
        Assert.Equal(100, segs[0].StartAddress);
        Assert.Equal(200, segs[1].StartAddress);
    }

    [Fact]
    [DisplayName("BuildSegments BatchStep=4 跨越间隔小于4时合并")]
    public void BuildSegments_BatchStep4_MergesWithinStep()
    {
        var driver = new MCDriver();

        var points = new List<IPoint>
        {
            new PointModel { Name = "p0", Address = "M0" },
            new PointModel { Name = "p1", Address = "M2" },
            new PointModel { Name = "p2", Address = "M4" },
            new PointModel { Name = "p3", Address = "M8" },
        };

        var segs = driver.BuildSegments(points, new MCParameter { BatchStep = 4 });
        var seg0 = Assert.Single(segs);
        Assert.Equal(0, seg0.StartAddress);
        Assert.Equal(9, seg0.Count); // 0 to 8 = 9 points span
    }

    #endregion

    #region Dispatch

    [Fact]
    [DisplayName("Dispatch 字分段数据正确分配到各点位")]
    public void Dispatch_WordSegment_CorrectlyMappedToPoints()
    {
        var driver = new MCDriver();

        var points = new IPoint[]
        {
            new PointModel { Name = "d100", Address = "D100" },
            new PointModel { Name = "d101", Address = "D101" },
            new PointModel { Name = "d102", Address = "D102" },
        };

        var seg = new MCDriver.MCSegment
        {
            Code = DeviceCode.D,
            StartAddress = 100,
            Count = 3,
            Words = [0x1111, 0x2222, 0x3333],
        };

        var dic = driver.Dispatch(points, [seg]);

        Assert.Equal((UInt16)0x1111, dic["d100"]);
        Assert.Equal((UInt16)0x2222, dic["d101"]);
        Assert.Equal((UInt16)0x3333, dic["d102"]);
    }

    [Fact]
    [DisplayName("Dispatch 位分段数据正确分配到各点位")]
    public void Dispatch_BitSegment_CorrectlyMappedToPoints()
    {
        var driver = new MCDriver();

        var points = new IPoint[]
        {
            new PointModel { Name = "m0", Address = "M0" },
            new PointModel { Name = "m1", Address = "M1" },
            new PointModel { Name = "m2", Address = "M2" },
        };

        var seg = new MCDriver.MCSegment
        {
            Code = DeviceCode.M,
            StartAddress = 0,
            Count = 3,
            Bits = [true, false, true],
        };

        var dic = driver.Dispatch(points, [seg]);

        Assert.Equal(true, dic["m0"]);
        Assert.Equal(false, dic["m1"]);
        Assert.Equal(true, dic["m2"]);
    }

    [Fact]
    [DisplayName("Write Boolean true 位软元件 M0 调用 WriteBits 含正确值")]
    public void Write_BitDevice_Boolean_True_CallsWriteBits()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        var mock = new Mock<MCProtocol> { CallBase = false };
        mock.Setup(e => e.WriteBits(DeviceCode.M, 0, It.IsAny<Boolean[]>()));
        driver.Link = mock.Object;

        var pt = new PointModel { Name = "M0Bit", Address = "M0" };
        var result = driver.Write(node, pt, true);

        Assert.True(result.IsSuccess);
        mock.Verify(e => e.WriteBits(DeviceCode.M, 0, It.Is<Boolean[]>(b => b.Length == 1 && b[0] == true)), Times.Once);
    }

    [Fact]
    [DisplayName("Read 某分段抛出异常时其他分段数据仍正常返回")]
    public void Read_SegmentThrows_OtherSegmentsStillReturn()
    {
        var driver = new MCDriver();
        var p = new MCParameter { Address = "127.0.0.1:6000" };
        var node = driver.Open(null, p);

        var mock = new Mock<MCProtocol> { CallBase = false };
        // D段抛异常
        mock.Setup(e => e.ReadWords(DeviceCode.D, It.IsAny<Int32>(), It.IsAny<Int32>()))
            .Throws(new InvalidOperationException("D段模拟失败"));
        // M段正常
        mock.Setup(e => e.ReadBits(DeviceCode.M, 0, It.IsAny<Int32>()))
            .Returns([true, false]);
        driver.Link = mock.Object;

        var points = new List<IPoint>
        {
            new PointModel { Name = "d100", Address = "D100" },
            new PointModel { Name = "m0",   Address = "M0"   },
            new PointModel { Name = "m1",   Address = "M1"   },
        };

        // 不应抛出异常
        var rs = driver.Read(node, [.. points]);

        // D100 因异常没有值，M0/M1 应正常读取
        Assert.Null(rs.GetValue("d100"));
        Assert.Equal(true,  rs.GetValue("m0"));
        Assert.Equal(false, rs.GetValue("m1"));
    }

    #endregion
}
