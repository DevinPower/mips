using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips.Instructions
{
    public class HelperFunctions
    {
        public static int[] BitsToInt(int data, params int[] sizes)
        {
            sizes = sizes.Reverse().ToArray();
            int[] result = new int[sizes.Length];
            int currentBitPosition = 0;

            for (int i = 0; i < sizes.Length; i++)
            {
                int size = sizes[i];

                int mask = (1 << size) - 1;
                result[i] = (data >> currentBitPosition) & mask;

                currentBitPosition += size;
            }

            return result;
        }

        public static float IntToFloat(int Value)
        {
            return BitConverter.ToSingle(BitConverter.GetBytes(Value), 0);
        }

        public static int FloatToInt(float Value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(Value), 0);
        }
    }
}
