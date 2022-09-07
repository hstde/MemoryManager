using System;
using System.Diagnostics.CodeAnalysis;

namespace Program;

public struct MemoryPointer
{
    public static readonly MemoryPointer NullPtr = new MemoryPointer(new Memory(Array.Empty<byte>()), 0);

    private Memory memory;
    private readonly int addr;

    public const int Size = sizeof(int);

    public int Value => addr;
    public Memory Memory => memory;
    public bool IsNullPtr => addr == 0;
    
    public MemoryPointer(Memory memory, int addr)
    {
        this.memory = memory;
        this.addr = addr;
    }

    public MemoryPointer AddOffset(int offset) => new MemoryPointer(memory, addr + offset);
    
    public byte GetUInt8() => GetUInt8(addr);
    public void Set(byte val) => Set(addr, val);
    public sbyte GetInt8() => GetInt8(addr);
    public void Set(sbyte val) => Set(addr, val);
    public ushort GetUInt16() => GetUInt16(addr);
    public void Set(ushort val) => Set(addr, val);
    public short GetInt16() => GetInt16(addr);
    public void Set(short val) => Set(addr, val);
    public uint GetUInt32() => GetUInt32(addr);
    public void Set(uint val) => Set(addr, val);
    public int GetInt32() => GetInt32(addr);
    public void Set(int val) => Set(addr, val);
    public ulong GetUInt64() => GetUInt64(addr);
    public void Set(ulong val) => Set(addr, val);
    public long GetInt64() => GetInt64(addr);
    public void Set(long val) => Set(addr, val);
    public MemoryPointer GetPtr() => GetPtr(addr);
    public void Set(MemoryPointer val) => Set(addr, val);
    

    private byte GetUInt8(int addr) => memory[addr];
    private void Set(int addr, byte val) => memory[addr] = val;
    private sbyte GetInt8(int addr) => (sbyte)memory[addr];
    private void Set(int addr, sbyte val) => memory[addr] = (byte)val;
    private ushort GetUInt16(int addr) => (ushort)((GetUInt8(addr + 1) << 8) | GetUInt8(addr));
    private void Set(int addr, ushort val)
    {
        Set(addr + 1, (byte)(val >> 8));
        Set(addr, (byte)(val));
    }
    private short GetInt16(int addr) => (short)GetUInt16(addr);
    private void Set(int addr, short val) => Set(addr, (ushort)(val));
    private uint GetUInt32(int addr) => (uint)((GetUInt16(addr + 2) << 16) | GetUInt16(addr));
    private void Set(int addr, uint val)
    {
        Set(addr + 2, (ushort)(val >> 16));
        Set(addr, (ushort)val);
    }
    private int GetInt32(int addr) => (int)GetUInt32(addr);
    private void Set(int addr, int val) => Set(addr, (uint)val);
    private ulong GetUInt64(int addr) => (((ulong)GetUInt32(addr + 4) << 32) | GetUInt32(addr));
    private void Set(int addr, ulong val)
    {
        Set(addr + 4, (uint)(val >> 32));
        Set(addr, (uint)val);
    }
    private long GetInt64(int addr) => (long)GetUInt64(addr);
    private void Set(int addr, long val) => Set(addr, (ulong)val);

    private MemoryPointer GetPtr(int addr) => new MemoryPointer(memory, GetInt32(addr));
    private void Set(int addr, MemoryPointer val) => Set(addr, val.addr);

    public static bool operator ==(MemoryPointer a, MemoryPointer b)
    {
        return a.Value == b.Value;
    }

    public static bool operator !=(MemoryPointer a, MemoryPointer b)
    {
        return !(a == b);
    }

    public static bool operator <(MemoryPointer a, MemoryPointer b)
    {
        return a.Value < b.Value;
    }

    public static bool operator <=(MemoryPointer a, MemoryPointer b)
    {
        return !(a > b);
    }

    public static bool operator >(MemoryPointer a, MemoryPointer b)
    {
        return a.Value > b.Value;
    }
    
    public static bool operator >=(MemoryPointer a, MemoryPointer b)
    {
        return !(a < b);
    }
}

public struct MemoryPointer<T> where T : struct
{
    private MemoryPointer rawPtr;

    public MemoryPointer(MemoryPointer rawPtr) => this.rawPtr = rawPtr;


    private object InternalGet()
    {
        T ret = default;

        switch(ret)
        {
            case byte:
                return rawPtr.GetUInt8();

            case int:
                return rawPtr.GetInt32();
            case uint:
                return rawPtr.GetUInt32();
                
            case long:
                return rawPtr.GetInt64();
            case ulong:
                return rawPtr.GetUInt64();

            default:
                throw new NotImplementedException();
        }
    }

    public T Get()
    {
        return (T)InternalGet();
    }

    public void Set(T value)
    {
        switch(value)
        {
            case byte b:
                rawPtr.Set(b);
                break;

            case int i:
                rawPtr.Set(i);
                break;
            case uint ui:
                rawPtr.Set(ui);
                break;

            case long l:
                rawPtr.Set(l);
                break;
            case ulong ul:
                rawPtr.Set(ul);
                break;
                
            default:
                throw new NotImplementedException();
        }
    }

    private int SizeOf()
    {
        switch(default(T))
        {
            case byte:
                return sizeof(byte);
            
            case int:
            case uint:
                return sizeof(uint);
            
            case long:
            case ulong:
                return sizeof(ulong);

            default:
                throw new NotImplementedException();
        }
    }

    public MemoryPointer<T> AddOffset(int offset)
    {
        return new MemoryPointer<T>(rawPtr.AddOffset(offset * SizeOf()));
    }
}