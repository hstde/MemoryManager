using System;

namespace Program;

public struct Heap
{
    private const int CHUNKSIZE = 32;

    private struct AllocHeader
    {
        public const int Size = sizeof(int);

        public MemoryPointer ptr;
        public int AllocationSizeInChunks
        {
            get => ptr.GetInt32();
            set => ptr.Set(value);
        }
        public MemoryPointer Data { get; }

        public AllocHeader(MemoryPointer ptr)
        {
            this.ptr = ptr;
            Data = ptr.AddOffset(sizeof(int));
        }
    }

    private int totalChunks = 0;
    private int allocatedChunks = 0;
    private MemoryPointer chunks = MemoryPointer.NullPtr;
    private Bitmap bitmap;

    public int TotalChunks => totalChunks;
    public int TotalBytes => totalChunks * CHUNKSIZE;
    public int FreeChunks => totalChunks - allocatedChunks;
    public int FreeBytes => FreeChunks * CHUNKSIZE;
    public int AllocatedChunks => allocatedChunks;
    public int AllocatedBytes => allocatedChunks * CHUNKSIZE;

    public Heap(MemoryPointer memory, int memorySize)
    {
        totalChunks = CalculateChunks(memorySize);
        chunks = memory;
        bitmap = new Bitmap(memory.AddOffset(totalChunks * CHUNKSIZE), totalChunks);
    }

    private static AllocHeader GetHeader(MemoryPointer ptr) => new AllocHeader(ptr.AddOffset(-AllocHeader.Size));

    private static int CalculateChunks(int memorySize) => (sizeof(byte) * memorySize) / (sizeof(byte) * CHUNKSIZE + 1);

    private static int CalculateMemoryForBytes(int bytes)
    {
        int neededChunks = (AllocHeader.Size + bytes + CHUNKSIZE - 1) / CHUNKSIZE;
        return neededChunks * CHUNKSIZE + (neededChunks + 7) / 8;
    }

    public MemoryPointer Allocate(int size)
    {
        int realSize = size + AllocHeader.Size;
        int chunksNeeded = (realSize + CHUNKSIZE - 1) / CHUNKSIZE;

        if (chunksNeeded > FreeChunks)
            return MemoryPointer.NullPtr;
        
        int? firstChunk = null;

        const int bestFitThreshold = 128;
        if (chunksNeeded < bestFitThreshold)
            firstChunk = bitmap.FindFirstFit(chunksNeeded);
        else
            firstChunk = bitmap.FindBestFit(chunksNeeded);
        
        if (firstChunk is null)
            return MemoryPointer.NullPtr;

        var header = new AllocHeader(chunks.AddOffset(firstChunk.Value * CHUNKSIZE));
        var ptr = header.Data;
        header.AllocationSizeInChunks = chunksNeeded;

        bitmap.SetRange(firstChunk.Value, chunksNeeded, true);

        allocatedChunks += chunksNeeded;

        return ptr;
    }

    public int Deallocate(MemoryPointer ptr)
    {
        if (ptr.IsNullPtr)
            return 0;

        var a = GetHeader(ptr);

        var start = (a.ptr.Value - chunks.Value) / CHUNKSIZE;


        bitmap.SetRange(start, a.AllocationSizeInChunks, false);

        allocatedChunks -= a.AllocationSizeInChunks;
        return a.AllocationSizeInChunks * CHUNKSIZE;
    }
    
    public bool Contains(MemoryPointer ptr)
    {
        var a = GetHeader(ptr);

        if (a.ptr < chunks)
            return false;
        if (ptr >= chunks.AddOffset(totalChunks * CHUNKSIZE))
            return false;
        return true;
    }
}