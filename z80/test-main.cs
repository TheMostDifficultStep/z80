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

        public void RunSbc() {
            // 1. No Borrow, No Flags (Clean Baseline)
            Cpu.Flags = 0;
            Cpu.Hl    = 0x4000;
            Cpu.SbcHl(  0x1000 );
            // C=0, H=0, V=0, Z=0, S=0, N=1
            CmpF( z:false, s:false, h:false, pv:false, n:true, c:false );

            // 2. Underflow to Zero (Exact Match with Carry)
            Cpu.Flags = Z80.Fl_C;
            Cpu.Hl    = 0x1001;
            Cpu.SbcHl(  0x1000 );
            // C=0, H=0, V=0, Z=1, S=0, N=1
            CmpF( z:true, s:false, h:false, pv:false, n:true, c:false );

            // 3. Maximum Borrow / Wraparound (0 - 1)
            Cpu.Flags = 0;
            Cpu.Hl    = 0x0000;
            Cpu.SbcHl(  0x0001 );
            // C=1, H=1, V=0, Z=0, S=1, N=1
            CmpF( z:false, s:true, h:true, pv:false, n:true, c:true );

            // 4. Half-Carry (H) Boundary
            Cpu.Flags = 0;
            Cpu.Hl    = 0x1000;
            Cpu.SbcHl(  0x0001 );
            // C=0, H=1, V=0, Z=0, S=0, N=1
            CmpF( z:false, s:false, h:true, pv:false, n:true, c:false );

            // 5. Signed Overflow (V) - Positive minus Negative
            Cpu.Flags = 0;
            Cpu.Hl    = 0x7000;
            Cpu.SbcHl(  0x9000 );
            // C=1, H=0, V=1, Z=0, S=1, N=1
            CmpF( z:false, s:true, h:false, pv:true, n:true, c:true );

            // 6. Signed Overflow (V) - Negative minus Positive
            Cpu.Flags = 0;
            Cpu.Hl    = 0x8000;
            Cpu.SbcHl(  0x0001 );
            // C=0, H=1, V=1, Z=0, S=0, N=1
            CmpF( z:false, s:false, h:true, pv:true, n:true, c:false );

            // 7. Subtracting a Register from Itself (With Carry)
            Cpu.Flags = Z80.Fl_C;
            Cpu.Hl    = 0x5555;
            Cpu.SbcHl(  0x5555 );
            // C=1, H=1, V=0, Z=0, S=1, N=1
            CmpF( z:false, s:true, h:true, pv:false, n:true, c:true );

        }

        public void RunAdc() {
            // 1. No-Op (Zero Cases)
            Cpu.Flags = 0;
            Cpu.Hl    = 0x0000;
            Cpu.AdcHl(  0x0000 );
            CmpF( z:true, s:false, h:false, pv:false, n:false, c:false );

            // 2. Maximum Limits (Wraparound)
            Cpu.Flags = 0;
            Cpu.Hl    = 0xFFFF;
            Cpu.AdcHl(  0x0001 );
            CmpF( z:true, s:false, h:true, pv:false, n:false, c:true );

            // 3. Half-Carry Boundary
            Cpu.Flags = 0;
            Cpu.Hl    = 0x0FFF;
            Cpu.AdcHl(  0x0001 );
            CmpF( z:false, s:false, h:true, pv:false, n:false, c:false );

            // 4. Signed Overflow (Positive + Positive = Negative)
            Cpu.Flags = 0;
            Cpu.Hl    = 0x7FFF ;
            Cpu.AdcHl(  0x0001 );
            CmpF( z:false, s:true, h:true, pv:true, n:false, c:false );

            // 5. Signed Overflow (Negative + Negative = Positive)
            Cpu.Flags = 0;
            Cpu.Hl    = 0x8000  ;
            Cpu.AdcHl(  0xFFFF  );
            CmpF( z:false, s:false, h:false, pv:true, n:false, c:true );

            // 6. Ripple Carry via Carry Flag
            Cpu.Flags = Z80.Fl_C;
            Cpu.Hl    = 0x0FFF;
            Cpu.AdcHl(  0x0000  );
            CmpF( z:false, s:false, h:true, pv:false, n:false, c:false );

            // 7. Sign Flag Triggering
            Cpu.Flags = 0;
            Cpu.Hl    = 0x4000;
            Cpu.AdcHl(  0x4000  );
            CmpF( z:false, s:true, h:false, pv:true, n:false, c:false );

        }

        static void Main(string[] args)
        {
            Console.WriteLine("Test LD 0x40 -> 0x7f");

            Program oProg = new Program();
            oProg.RunSub();
            oProg.RunAdd();
            oProg.RunSbc();
            oProg.RunAdc();
        }
    }
}
