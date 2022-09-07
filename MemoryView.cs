using System;

namespace Program;

public struct MemoryView
{
    private MemoryPointer ptr;
    private readonly int size;

    public int Start => ptr.Value;
    public int End => ptr.Value + size;
    public int Size => size;

    public MemoryPointer this[int i]
    {
        get
        {
            VerifyInRange(i);
            return ptr.AddOffset(i);
        }
    }

    private readonly void VerifyInRange(int i)
    {
        if (i < 0 || i > size) throw new IndexOutOfRangeException();
    }

    public MemoryView(MemoryPointer ptr, int size)
    {
        this.ptr = ptr;
        this.size = size;
    }
}