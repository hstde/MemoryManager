using System;

namespace Program;

public struct Bitmap
{
    public const int maxSize = 0x7fff_ffff;
    private static byte[] bitmaskFirstByte = { 0xFF, 0xFE, 0xFC, 0xF8, 0xF0, 0xE0, 0xC0, 0x80 };
    private static byte[] bitmaskLastByte = { 0x00, 0x1, 0x3, 0x7, 0xF, 0x1F, 0x3F, 0x7F };

    private MemoryPointer data;
    private int size = 0;

    public int Size => size;
    public int ByteSize => Program.CeilDiv(size, 8);

    public Bitmap(MemoryPointer data, int size)
    {
        this.data = data;
        this.size = size;
    }

    public void Set(int index, bool value)
    {
        var addr = data.AddOffset(index / 8);
        var readVal = addr.GetUInt8();

        if (value)
            addr.Set((byte)(readVal | (byte)(1u << (index % 8))));
        else
            addr.Set((byte)(readVal & (byte)(~(1u << (index % 8)))));
    }

    public int? FindFirstFit(int minimumLength)
    {
        int start = 0;
        int? lengthOfFoundRange = FindNextRangeOfUnsetBits(ref start, minimumLength, minimumLength);

        if(lengthOfFoundRange is not null)
            return start;

        return null;
    }

    public int? FindBestFit(int minimumLength)
    {
        int start = 0;
        int bestRegionStart = 0;
        int bestRegionSize = maxSize;
        bool found = false;

        while (true) {
            // Look for the next block which is bigger than requested length.
            int? lengthOfFoundRange = FindNextRangeOfUnsetBits(ref start, minimumLength, bestRegionSize);
            if (lengthOfFoundRange is not null) {
                if (bestRegionSize > lengthOfFoundRange.Value || !found) {
                    bestRegionStart = start;
                    bestRegionSize = lengthOfFoundRange.Value;
                    found = true;
                }
                start += lengthOfFoundRange.Value;
            } else {
                // There are no ranges which can fit requested length.
                break;
            }
        }

        if (found) {
            return bestRegionStart;
        }
        return null;
    }

    private int? FindNextRangeOfUnsetBits(ref int from, int minLength, int maxLength = maxSize)
    {
       if (minLength > maxLength) {
            return null;
        }

        int bitSize = 8 * sizeof(ulong);

        var bitmap = new MemoryPointer<ulong>(data);

        // Calculating the start offset.
        int startBucketIndex = from / bitSize;
        int startBucketBit = from % bitSize;

        ref int startOfFreeChunks = ref from;
        int freeChunks = 0;

        for (int bucketIndex = startBucketIndex; bucketIndex < size / bitSize; ++bucketIndex) {
            var bucket = bitmap.AddOffset(bucketIndex).Get();
            if (bucket == ulong.MaxValue) {
                // Skip over completely full bucket of size bit_size.
                if (freeChunks >= minLength) {
                    return Math.Min(freeChunks, maxLength);
                }
                freeChunks = 0;
                startBucketBit = 0;
                continue;
            }
            if (bucket == 0x0) {
                // Skip over completely empty bucket of size bit_size.
                if (freeChunks == 0) {
                    startOfFreeChunks = bucketIndex * bitSize;
                }
                freeChunks += bitSize;
                if (freeChunks >= maxLength) {
                    return maxLength;
                }
                startBucketBit = 0;
                continue;
            }

            byte viewedBits = (byte)startBucketBit;
            int trailingZeroes = 0;

            bucket >>= viewedBits;
            startBucketBit = 0;

            while (viewedBits < bitSize) {
                if (bucket == 0) {
                    if (freeChunks == 0) {
                        startOfFreeChunks = bucketIndex * bitSize + viewedBits;
                    }
                    freeChunks += bitSize - viewedBits;
                    viewedBits = (byte)bitSize;
                } else {
                    trailingZeroes = CountTrailingZeroes(bucket);
                    bucket >>= trailingZeroes;

                    if (freeChunks == 0) {
                        startOfFreeChunks = bucketIndex * bitSize + viewedBits;
                    }
                    freeChunks += trailingZeroes;
                    viewedBits += (byte)trailingZeroes;

                    if (freeChunks >= minLength) {
                        return Math.Min(freeChunks, maxLength);
                    }

                    // Deleting trailing ones.
                    int trailingOnes = CountTrailingZeroes(~bucket);
                    bucket >>= trailingOnes;
                    viewedBits += (byte)trailingOnes;
                    freeChunks = 0;
                }
            }
        }

        if (freeChunks < minLength) {
            int firstTrailingBit = (size / bitSize) * bitSize;
            int trailingBits = size % bitSize;
            for (int i = 0; i < trailingBits; ++i) {
                if (!Get(firstTrailingBit + i)) {
                    if (freeChunks == 0)
                        startOfFreeChunks = firstTrailingBit + i;
                    if (++freeChunks >= minLength)
                        return Math.Min(freeChunks, maxLength);
                } else {
                    freeChunks = 0;
                }
            }
            return null;
        }

        return Math.Min(freeChunks, maxLength);
    }

    private bool Get(int index)
    {
        return 0 != (data.AddOffset(index / 8).GetUInt8() & (1u << (index % 8)));
    }

    private int CountTrailingZeroes(ulong value)
    {
        for (int i = 0; i < 8 * sizeof(ulong); ++i) {
            if (((value >> i) & 1) == 1) {
                return i;
            }
        }
        return 8 * sizeof(ulong);
    }

    public void SetRange(int start, int len, bool value)
    {
        if (len == 0)
            return;

        var first = data.AddOffset(start / 8);
        var last = data.AddOffset((start + len) / 8);
        byte byteMask = bitmaskFirstByte[start % 8];

        if (first == last) {
            byteMask &= bitmaskLastByte[(start + len) % 8];

            if (value)
                first.Set((byte)(first.GetUInt8() | byteMask));
            else
                first.Set((byte)(first.GetUInt8() & ~byteMask));
        } else {
            
            if (value)
                first.Set((byte)(first.GetUInt8() | byteMask));
            else
                first.Set((byte)(first.GetUInt8() & ~byteMask));

            byteMask = bitmaskLastByte[(start + len) % 8];

            if (value)
                last.Set((byte)(last.GetUInt8() | byteMask));
            else
                last.Set((byte)(last.GetUInt8() & ~byteMask));
            
            first = first.AddOffset(1);

            if (first < last) {
                if (value)
                    Memory.Memset(first, 0xFF, last.Value - first.Value);
                else
                    Memory.Memset(first, 0x0, last.Value - first.Value);
            }
        }
    }

    public void Fill(bool value)
    {
        Memory.Memset(data, (byte)(value ? 0xFF : 0x00), ByteSize);
    }
}