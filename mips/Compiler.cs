using mips.Instructions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace mips
{
    public class Compiler
    {
        public Compiler() { }

        public string[] Compile(Computer Computer, string[] Program)
        {
            string[] MemProgram = RunMemoryCommands(Computer, Program);
            var labels = FindLabels(MemProgram);
            string[] compiled = ReplaceLabels(MemProgram, labels);



            return compiled;
        }

        string asciiz(Computer Computer, string currentLine)
        {
            /*//TODO: Process everything AFTER asciiz to support labels
            currentLine = currentLine.Replace(".asciiz ", "");
            int pointerStart = Computer.GetMemoryPointer();
            foreach (char c in HelperFunctions.ProcessString(currentLine))
            {
                Computer.StoreMemory((int)c);
            }
            Computer.StoreMemory(0);        //null terminator

            return pointerStart.ToString();*/
            return "";
        }

        string ascii(Computer Computer, string currentLine)
        {
            /*//TODO: Process everything AFTER asciiz to support labels
            currentLine = currentLine.Replace(".ascii ", "");
            int pointerStart = Computer.GetMemoryPointer();
            foreach (char c in HelperFunctions.ProcessString(currentLine))
            {
                Computer.StoreMemory((int)c);
            }

            return pointerStart.ToString();*/
            return "";
        }

        string[] RunMemoryCommands(Computer Computer, string[] Program)
        {
            Func<Computer, string, string>[] memoryCommands = new Func<Computer, string, string>[] 
            { ascii, asciiz };
            List<string> compiledResult = new List<string>();
            for (int i = 0; i < Program.Length; i++)
            {
                string currentLine = Program[i];
                string[] splits = currentLine.Split(' ');
                for (int j = 0; j < splits.Length; j++)
                {
                    foreach(Func<Computer, string, string> action in memoryCommands)
                    {
                        if ('.' + action.Method.Name == splits[j])
                        {
                            currentLine = action.Invoke(Computer, currentLine);
                            break;
                        }
                    }
                }
                compiledResult.Add(currentLine);
            }

            return compiledResult.ToArray();
        }

        string[] ReplaceLabels(string[] Program, Dictionary<string, int> Labels)
        {
            List<string> compiledResult = new List<string>();
            for (int i = 0; i < Program.Length; i++)
            {
                string currentLine = Program[i];
                string[] splits = currentLine.Split(' ');
                for (int j = 0; j < splits.Length; j++)
                {
                    if (Labels.ContainsKey(splits[j]))
                    {
                        splits[j] = Labels[splits[j]].ToString();
                    }
                    else if (Labels.ContainsKey(splits[j].Replace(":", "")))
                    {
                        splits[j] = "";
                    }
                }
                compiledResult.Add(string.Join(' ', splits));
            }

            return compiledResult.ToArray();
        }

        Dictionary<string, int> FindLabels(string[] Program)
        {
            Dictionary<string, int> results = new Dictionary<string, int>();
            int i = 0;
            foreach (string line in Program)
            {
                string current = line.Split(' ')[0];
                if (current.EndsWith(':'))
                {
                    results.Add(current.Substring(0, current.Length - 1), i);
                }
                i++;
            }    

            return results;
        }
    }
}
