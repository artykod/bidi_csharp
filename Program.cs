using System;

namespace BidiTest
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.Unicode;

            var input = @"ضص, شس.
ءيب (الب)? hello world.
abc [def], why {so} fsh?
انق ثقفغ!
huihu 123.";

            //var input = @"ضص, شس. ءيب الب? hello world. abc def, why so fsh? انق ثقفغ! huihu 123.";

            var output = new Bidi(true).Convert(input);

            Console.WriteLine($"input:\n{input}");
            Console.WriteLine();
            Console.WriteLine($"output:\n{output}");

            Console.ReadKey();
        }
    }
}
