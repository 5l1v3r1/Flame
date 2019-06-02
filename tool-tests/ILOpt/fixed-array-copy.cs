// TODO: investigate, re-enable this test for Linux builds only: //! run
// For some reason, this test case appears to loop forever on the .NET framework.
// Not sure why, don't have a Windows development machine on hand.

using System;

class Program
{
    static unsafe void Main()
    {
        var str = "Hello world!";
        var arr = new char[str.Length];
        fixed (char* dest = arr)
        {
            fixed (char* src = str)
            {
                Copy(dest, src, arr.Length);
            }
        }

        Print(arr);
    }

    private unsafe static void Copy(
        char* destination,
        char* source,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            *(destination + i) = *(source + i);
        }
    }

    private static void Print(char[] value)
    {
        foreach (var elem in value)
        {
            Console.Write(elem);
        }
        Console.WriteLine();
    }
}
