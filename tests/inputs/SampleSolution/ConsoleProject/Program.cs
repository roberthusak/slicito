using LibraryProject;

namespace ConsoleProject;

internal class Program
{
    static void Main(string[] args)
    {
        if (args.Length != 2)
        {
            Console.WriteLine("Usage: ConsoleProject <number> <number>");
            return;
        }

        if (!int.TryParse(args[0], out int a) || !int.TryParse(args[1], out int b))
        {
            Console.WriteLine("Invalid input");
            return;
        }

        var adder = AdderProvider.GetAdder(a, b);
        Console.WriteLine(adder.Add());
    }
}
