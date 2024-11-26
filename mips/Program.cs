using mips;


while (true)
{
    Console.Clear();
    Console.WriteLine("Enter the name of a file to run");
    string file = Console.ReadLine();

    if (!System.IO.File.Exists($"Samples/{file}.txt"))
        continue;

    string[] contents = System.IO.File.ReadAllLines($"Samples/{file}.txt");

    Computer c = new Computer(128);
    c.Compile(contents);

    c.ProcessFull();

    c.DumpMemory();

    Console.ReadKey();
}