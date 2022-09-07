using System;

namespace Program;

public static class Program
{
    private const int KiB = 1024;
    private const int MiB = 1024 * KiB;
    private const int GiB = 1024 * MiB;

    public static int CeilDiv(int a, int b)
    {
        int res = a / b;
        if ((a % b) != 0) ++res;
        return res;
    }

    public static int Main2(string[] args)
    {
        {
            var bitmap = CreateBitmap(32, true);
            bitmap.Fill(true);

            var fit = bitmap.FindFirstFit(1);

            Console.WriteLine(fit + " " + (fit == null));
        }

        {
            var bitmap = CreateBitmap(32, true);
            bitmap.Set(31, false);

            var fit = bitmap.FindFirstFit(1);

            Console.WriteLine(fit + " " + (fit == 31));
        }

        for (int i = 0; i < 128; ++i)
        {
            var bitmap = CreateBitmap(128, true);
            bitmap.Set(i, false);
            var fit = bitmap.FindFirstFit(1);
            
            Console.WriteLine(fit + " " + (fit == i));
        }

        for (int i = 0; i < 127; ++i)
        {
            var bitmap = CreateBitmap(128, true);
            bitmap.Set(i, false);
            bitmap.Set(i + 1, false);
            var fit = bitmap.FindFirstFit(2);

            Console.WriteLine(fit + " " + (fit == i));
        }


        return 0;
    }

    private static Bitmap CreateBitmap(int size, bool initValue)
    {
        var rawMem = new byte[CeilDiv(size, 8)];
        var mem = new Memory(rawMem);

        var bitmap = new Bitmap(mem.GetPointer(0), size);
        bitmap.Fill(initValue);

        return bitmap;
    }

    public static int Main(string[] args)
    {
        var rawMem = new byte[1 * MiB];
        var mem = new Memory(rawMem);

        var heap = new Heap(mem.GetPointer(1024), 64 * KiB);

        var ptr = heap.Allocate(1 * KiB);
        Console.WriteLine($"ptr1 @ {ptr.Value:X8}");

        ptr.Set(0xDEADBEEF);

        var ptr2 = heap.Allocate(100);
        Console.WriteLine($"ptr2 @ {ptr2.Value:X8}");
        ptr2.Set(0xC0FFEE);
        Console.WriteLine(ptr.GetUInt32().ToString("X"));
        Console.WriteLine(ptr2.GetUInt32().ToString("X"));

        Console.WriteLine("allocated bytes: " + heap.AllocatedBytes);
        Console.WriteLine("free bytes: " + heap.FreeBytes);

        //heap.Deallocate(ptr);

        Console.WriteLine("allocated bytes: " + heap.AllocatedBytes);
        Console.WriteLine("free bytes: " + heap.FreeBytes);

        ptr = heap.Allocate(10);
        Console.WriteLine($"ptr3 @ {ptr.Value:X8}");
        Console.WriteLine(ptr.GetUInt32().ToString("X"));
        ptr.Set(0x80808080);
        Console.WriteLine(ptr.GetUInt32().ToString("X"));


        Console.WriteLine("allocated bytes: " + heap.AllocatedBytes);
        Console.WriteLine("free bytes: " + heap.FreeBytes);

        DumpMem(rawMem, 1024, 128);
        DumpMem(rawMem, 2048, 128);
        DumpMem(rawMem, 0xFC00, 128);

        return 0;
    }

    public static void DumpMem(byte[] mem, int start, int length)
    {
        for (int y = start; y < start + length; y += 16)
        {
            for (int x = 0; x < 16 && x < start + length - y; x++)
            {
                Console.Write($"{mem[y + x]:X2} ");
                if ((x + 1) % 8 == 0)
                    Console.Write(" ");
            }
            Console.WriteLine();
        }
        Console.WriteLine(new String('=', 2 * 16 + 1 * 16));
    }
}