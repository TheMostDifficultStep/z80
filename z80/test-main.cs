using System;

namespace z80 {
    public class TestPorts : IPorts {
        public string Name => throw new NotImplementedException();

        public bool NMI => throw new NotImplementedException();

        public bool MI => throw new NotImplementedException();

        public byte Data => throw new NotImplementedException();

        public byte ReadPort(ushort address) {
            throw new NotImplementedException();
        }

        public void WritePort(ushort address, byte value) {
            throw new NotImplementedException();
        }
    }

    class Program
    {
        public    Z80Memory Memory { get; }
        protected Z80       Cpu    { get; }

        public Program() {
            Memory  = new Z80Memory( (int)Math.Pow( 2, 16 ) );
            Cpu     = new Z80(Memory, new TestPorts()) {
                Hl = 0
            };
        }

        public void Run() {
            for( int iInstr = 0x40; iInstr <= 0x7f; ++iInstr ) {
                byte r = (byte)(( iInstr >> 3 ) & 0x7);
                byte l = (byte)(iInstr & 0x7 );

                Cpu.registers[Z80.B] = 0x81;
                Cpu.registers[Z80.C] = 0x82;
                Cpu.registers[Z80.D] = 0x83;
                Cpu.registers[Z80.E] = 0x84;
                Cpu.registers[Z80.H] = 0x85;
                Cpu.registers[Z80.L] = 0x86;
                Cpu.registers[Z80.A] = 0x87;

                Memory[Cpu.Hl] = 0xfa;

                Console.Write( iInstr.ToString( "X2" ) + " " );

                Cpu.LdInstructions( r, l );
            }
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Test LD 0x40 -> 0x7f");

            Program oProg = new Program();
            oProg.Run();
        }
    }
}
