using System;

namespace Program;

public struct Memory
{
    private readonly byte[] internalMemory;

    public byte this[int i]
    {
        get => internalMemory[i];
        set => internalMemory[i] = value;
    }

    public int Size => internalMemory.Length;

    public Memory(byte[] mem) => internalMemory = mem;

    public MemoryView GetView(int offset, int size) => new MemoryView(GetPointer(offset), size);

    public MemoryPointer GetPointer(int offset) => new MemoryPointer(this, offset);

    public static void Memset(MemoryPointer start, byte value, int length)
    {
        Array.Fill(start.Memory.internalMemory, value, start.Value, length);
    }
}