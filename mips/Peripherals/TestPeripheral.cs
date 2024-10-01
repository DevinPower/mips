using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips.Peripherals
{
    public class TestPeripheral : Peripheral
    {
        public override int MemoryAddress => _mappedMemory;
        public override string Name => "MONITOR";

        int _mappedMemory { get; set; }
        public override void Initialize(Computer Owner)
        {
            _mappedMemory = Owner.ReserveMemory(2);
            Owner.Memory[_mappedMemory] = 0;
            Owner.Memory[_mappedMemory + 1] = (int)'M';
        }

        public override void Step(Computer Owner)
        {
            if (Owner.Memory[_mappedMemory] == 1)
            {
                Console.Write((char)Owner.Memory[_mappedMemory + 1]);
                Owner.Memory[_mappedMemory] = 0;
            }
        }
    }
}
