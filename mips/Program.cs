using mips;


while (true)
{
    Console.Clear();
    Console.WriteLine("Enter the name of a file to run");

    string[] sampleFiles = System.IO.Directory.GetFiles("Samples");
    int i = 0;
    string current = "";
    foreach (var sample in sampleFiles)
    {
        current += sample.Split('\\')[1].PadRight(30, '.');
        i++;
        if (i % 3 == 0)
        {
            Console.WriteLine(current);
            current = "";
        }
    }

    Console.WriteLine(current);
    Console.WriteLine("");

    string file = Console.ReadLine();

    if (!System.IO.File.Exists($"Samples/{file}.txt"))
        continue;

    string[] contents = System.IO.File.ReadAllLines($"Samples/{file}.txt");

    Computer c = new Computer(512);
    c.Compile(contents);

    c.ProcessFull();

    c.DumpMemory();

    Console.ReadKey();
}