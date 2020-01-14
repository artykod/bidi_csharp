using System;

namespace BidiTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var input = @"ABC DEF
abc def
QWE RTY";
            var output = new Bidi().Convert(input);

            Console.WriteLine(input);
            Console.WriteLine();
            Console.WriteLine(output);

            Console.ReadKey();
        }
    }
}
