using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips.Instructions
{
    internal class HelperFunctions
    {
        /// <summary>
        /// Processes a string in full from quote to quote, returns the rest of the input in Incomplete
        /// </summary>
        /// <param name="FullString">Full command</param>
        /// <param name="Incomplete">Everything not processesd</param>
        public static string ProcessString(string FullString, out string Incomplete)
        {
            StringBuilder result = new StringBuilder();
            int current = 1;
            while (current < FullString.Length)
            {
                char currentChar = FullString[current];
                if (currentChar == '"')
                {
                    Incomplete = FullString.Substring(current);
                    return result.ToString();
                }
                result.Append(currentChar);
            }

            throw new Exception("String not terminated by quotes :(");
        }

        /// <summary>
        /// Processes a string in full from quote to quote
        /// </summary>
        /// <param name="FullString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string ProcessString(string FullString)
        {
            StringBuilder result = new StringBuilder();
            int current = 1;
            while (current < FullString.Length)
            {
                char currentChar = FullString[current];
                if (currentChar == '"')
                {
                    return result.ToString();
                }
                result.Append(currentChar);
                current++;
            }

            throw new Exception("String not terminated by quotes :(");
        }

        public static int ProcessMemoryAddress(Computer Computer, string Address)
        {
            if (!Address.Contains(')'))
                return Computer.Registers[Computer.GetRegisterAddress(Address)];

            string[] Splits = Address.Split('(');
            int add = int.Parse(Splits[0]);

            string register = Splits[1].Split(')')[0];
            return Computer.Registers[Computer.GetRegisterAddress(register)] + add;
        }
    }
}
