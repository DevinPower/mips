using Lexer.AST;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lexer
{
    internal class Helpers
    {
        public static int HexToInt(string Value)
        {
            if (!Value.StartsWith("0x"))
                throw new Exception($"Expected hex string beginning with 0x, got '{Value}'");

            return Convert.ToInt32(Value, 16);
        }

        public static float IntToFloat(int Value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(Value), 0);
        }

        public static int FloatToInt(float Value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Value), 0);
        }

        /// <summary>
        /// Takes in a length of data, returns a pointer to this block on the heap. Data[0] of the allocated heap data is the size of allocation
        /// </summary>
        /// <param name="CompilationMeta"></param>
        /// <param name="Code"></param>
        /// <param name="DataSize"></param>
        /// <returns></returns>
        public static RegisterResult HeapAllocation(CompilationMeta CompilationMeta, List<string> Code, int DataSize)
        {
            //TODO: May need to store a0 and v0 register state
            DataSize += 1;

            RegisterResult result = new RegisterResult($"$t{CompilationMeta.GetTempRegister()}");

            Code.Add($"Li $a0, {DataSize}");
            Code.Add($"Ori $v0, $zero, 9");
            Code.Add($"Syscall");
            Code.Add($"SB $a0, 0($v0)");
            Code.Add($"Move {result}, $v0");

            return result;
        }
    }
}
