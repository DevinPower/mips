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
            GenericRegisterState registerState = new GenericRegisterState(new string[] {"$a0" }, CompilationMeta);
            registerState.SaveState(CompilationMeta, Code);
            DataSize += 1;

            RegisterResult result = new RegisterResult("$v0");

            Code.Add($"Li $a0, {DataSize}");
            Code.Add($"Ori $v0, $zero, 9");
            Code.Add($"Syscall");
            Code.Add($"SB $a0, 0($v0)");

            registerState.LoadState(CompilationMeta, Code);

            return result;
        }

        public static void DebugPrint(CompilationMeta CompilationMeta, List<string> Code, RegisterResult Register)
        {
            GenericRegisterState registerState = new GenericRegisterState(new string[] { "$a0", "$v0" }, CompilationMeta);
            registerState.SaveState(CompilationMeta, Code);

            Code.Add($"Move $a0, {Register}");
            Code.Add($"Ori $v0, $zero, 1");
            Code.Add($"Syscall");

            registerState.LoadState(CompilationMeta, Code);
        }

        public static void DebugBreak(CompilationMeta CompilationMeta, List<string> Code)
        {
            GenericRegisterState registerState = new GenericRegisterState(new string[] { "$v0" }, CompilationMeta);
            registerState.SaveState(CompilationMeta, Code);

            Code.Add($"Ori $v0, $zero, 19");
            Code.Add($"Syscall");

            registerState.LoadState(CompilationMeta, Code);
        }
    }
}
