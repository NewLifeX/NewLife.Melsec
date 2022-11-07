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
        var mb = new Mock<FxLinks>() { CallBase = true };
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadRegister, 1, It.IsAny<UInt16>(), 1))
        //    .Returns("02-02-00".ToHex());
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadRegister, 1, It.IsAny<UInt16>(), 2))
        //    .Returns("04-01-02-03-04".ToHex());

        var modbus = mb.Object;

        Assert.Equal("FxLinksProxy", modbus.Name);

        //Assert.Throws<NotSupportedException>(() => modbus.Read(FunctionCodes.ReadWriteMultipleRegisters, 1, 1, 1));

        //// 读取
        //var rs = modbus.Read(FunctionCodes.ReadRegister, 1, 100, 1) as Packet;
        //Assert.NotNull(rs);
        //Assert.Equal(0x0200, rs.ReadBytes().ToUInt16(0, false));

        //rs = modbus.Read(FunctionCodes.ReadRegister, 1, 102, 2) as Packet;
        //Assert.NotNull(rs);
        //Assert.Equal(0x01020304u, rs.ReadBytes().ToUInt32(0, false));
    }

    [Fact]
    public void ReadBit()
    {
        // 模拟FxLinks
        var mb = new Mock<FxLinks>() { CallBase = true };
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadCoil, 1, 100, 2))
        //    .Returns("02-12-34-56-78".ToHex());

        var modbus = mb.Object;

        //// 读取
        //var rs = modbus.ReadCoil(1, 100, 2);
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
        //mb.Setup(e => e.SendCommand(FunctionCodes.ReadDiscrete, 1, 100, 2))
        //    .Returns("02-12-34-56-78".ToHex());

        var modbus = mb.Object;

        // 读取
        var rs = modbus.ReadWord(1, 100, 2);
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
                Reply = true,
                //Host = e.Host,
                Address = e.Address,
                Payload = e.Payload.Slice(0, 2).Append("03-04".ToHex())
            });

        var modbus = mb.Object;

        Assert.Equal("FxLinksProxy", modbus.Name);

        Assert.Throws<NotSupportedException>(() => modbus.Write("WT", 1, 1, new UInt16[] { 1 }));

        var rs = (Int32)modbus.Write("WW", 1, 100, new UInt16[] { 1 });
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
                Reply = true,
                Address = e.Address,
                Payload = e.Payload.Slice(0, 4)
            });

        var modbus = mb.Object;

        // 读取
        var rs = modbus.WriteBit(1, 100, 0xFF00);
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
                Reply = true,
                Address = e.Address,
                Payload = e.Payload.Slice(0, 2).Append(e.Payload.Slice(2, 2))
            });

        var modbus = mb.Object;

        // 读取
        var rs = modbus.WriteWord(1, 100, 0x1234);
        Assert.Equal(0x1234, rs);
    }
}