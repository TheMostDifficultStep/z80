using System;
using System.Net.NetworkInformation;

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

        public bool CmpF( bool z, bool s, bool h, bool pv, bool n, bool c ) {
            byte bFReg = Cpu.registers[Z80.F];

            if( ( bFReg & Z80.Fl_Z ) > 0 != z )
                return false;
            if( ( bFReg & Z80.Fl_S ) > 0 != s )
                return false;
            if( ( bFReg & Z80.Fl_H ) > 0 != h )
                return false;
            if( ( bFReg & Z80.Fl_PV ) > 0 != pv )
                return false;
            if( ( bFReg & Z80.Fl_N ) > 0 != n )
                return false;
            if( ( bFReg & Z80.Fl_C ) > 0 != c )
                return false;

            return true;
        }

        public void RunAdd() {
            Cpu.registers[Z80.A] = 0x00;
            Cpu.Add( 0 ); // z=1, s=0, h=0, pv=0, n=0, c=0
            CmpF( z:true, s:false, h:false, pv:false, n:false, c:false );

            Cpu.registers[Z80.A] = 0xff;
            Cpu.Add( 0 ); // z=0, s=1, h=0, pv=0, n=0, c=0
            CmpF( z:false, s:true, h:false, pv:false, n:false, c:false );

            Cpu.registers[Z80.A] = 0xff;
            Cpu.Add( 1 ); // z=1, s=0, h=1, pv=0, n=0, c=1
            CmpF( z:true, s:false, h:true, pv:false, n:false, c:true );

            Cpu.registers[Z80.A] = 0xfe;
            Cpu.Add( 1 ); // z=0, s=1, h=0, pv=0, n=0, c=0
            CmpF( z:false, s:true, h:false, pv:false, n:false, c:false );

            Cpu.registers[Z80.A] = 0x7f;
            Cpu.Add( 1 ); // z=0, s=1, h=1, pv=1, n=0, c=0
            CmpF( z:false, s:true, h:true, pv:true, n:false, c:false );

            Cpu.registers[Z80.A] = 0x80;
            Cpu.Add( 0xff ); // z=0, s=0, h=0(s/b 1), pv=1, n=0, c=1
            CmpF( z:false, s:false, h:false, pv:true, n:false, c:true );

            Cpu.registers[Z80.A] = 0x0f;
            Cpu.Add( 0x01 ); // z=0, s=0, h=1, pv=0, n=0, c=0
            CmpF( z:false, s:false, h:true, pv:false, n:false, c:false );

            Cpu.registers[Z80.A] = 0x7f;
            Cpu.Add( 0x0f ); // z=0, s=1, h=1, pv=1, n=0, c=0
            CmpF( z:false, s:true, h:true, pv:true, n:false, c:false );
        }

        public void RunSub() {
            Cpu.registers[Z80.A] = 0x00;
            Cpu.Sub( 0x00 ); 
            CmpF( z:true, s:false, h:false, pv:false, n:true, c:false );

            Cpu.registers[Z80.A] = 0x80;
            Cpu.Sub( 0x80 ); 
            CmpF( z:true, s:false, h:false, pv:false, n:true, c:false );

            Cpu.registers[Z80.A] = 0x00;
            Cpu.Sub( 0x01 ); 
            CmpF( z:false, s:true, h:true, pv:false, n:true, c:true );

            Cpu.registers[Z80.A] = 0x01;
            Cpu.Sub( 0x01 ); 
            CmpF( z:true, s:false, h:false, pv:false, n:true, c:false );

            Cpu.registers[Z80.A] = 0x7f;
            Cpu.Sub( 0x81 ); 
            CmpF( z:false, s:true, h:false, pv:true, n:true, c:true );

            Cpu.registers[Z80.A] = 0x80;
            Cpu.Sub( 0x01 ); 
            CmpF( z:false, s:false, h:true, pv:true, n:true, c:false );

            Cpu.registers[Z80.A] = 0x10;
            Cpu.Sub( 0x01 ); 
            CmpF( z:false, s:false, h:true, pv:false, n:true, c:false );

            Cpu.registers[Z80.A] = 0x0f;
            Cpu.Sub( 0x10 ); 
            CmpF( z:false, s:true, h:false, pv:false, n:true, c:true );

        }

        static void Main(string[] args)
        {
            Console.WriteLine("Test LD 0x40 -> 0x7f");

            Program oProg = new Program();
            oProg.RunSub();
            oProg.RunAdd();
        }
    }
}
