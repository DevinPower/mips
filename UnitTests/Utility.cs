using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests
{
    internal static class Utility
    {
        public static string ReadStringFromPointer(this Computer Computer, int AddressPointer)
        {
            int Address = Computer.Memory[AddressPointer];
            StringBuilder value = new StringBuilder();
            
            while (true)
            {
                int CurrentChar = Computer.Memory[Address++];
                if (CurrentChar == 0)
                {
                    return value.ToString();
                }
                value.Append((char)CurrentChar);
            }
        }
    }
}
