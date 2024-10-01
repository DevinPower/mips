using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mips.Peripherals
{
    public class Peripheral
    {
        public virtual int MemoryAddress { get; }
        public virtual string Name { get; }

        public virtual void Initialize(Computer Owner)
        {

        }

        public virtual void Step(Computer Owner)
        {

        }
    }
}
