using System;

namespace BidiTest
{
    class Program
    {
        static void Main()
        {
            var input = @"XY, ZW.
ABC DEF? hello (world).
abc def, why so fsh?
QWE RTY!
huihu 123.";
            var output = new Bidi().Convert(input);

            Console.WriteLine($"input:\n{input}");
            Console.WriteLine();
            Console.WriteLine($"output:\n{output}");

            Console.ReadKey();
        }
    }
}
