using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    internal class Conversions
    {
        public static int HexToInt(string Value)
        {
            if (!Value.StartsWith("0x"))
                throw new Exception($"Expected hex string beginning with 0x, got '{Value}'");

            return Convert.ToInt32(Value, 16);
        }
    }
}
