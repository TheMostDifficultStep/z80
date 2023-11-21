using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using z80;
using static System.Net.Mime.MediaTypeNames;
using static z80Sample.SamplePorts;

namespace z80Sample
{
    internal class Program
    {
        private static byte[] InitMe() {
            List<byte> rgBytes = new List<byte>();

            rgBytes.Add( 0x31 ); // sp, nn
            rgBytes.Add( 0x00 );
            rgBytes.Add( 0x10 );

            rgBytes.Add( 0xc3 ); // jp nn

            int iStart_Patch = rgBytes.Count;

            rgBytes.Add( 0x00 );
            rgBytes.Add( 0x00 );

            int iHello = rgBytes.Count;

            rgBytes.Add( (byte)'h' );
            rgBytes.Add( (byte)'e' );
            rgBytes.Add( (byte)'l' );
            rgBytes.Add( (byte)'l' );
            rgBytes.Add( (byte)'o' );
            rgBytes.Add( 0x00 );

            int iStart = rgBytes.Count;
            rgBytes[iStart_Patch] = (byte)iStart; // just set low byte.

            rgBytes.Add( 0x21 );  // ld hl, nn
            rgBytes.Add( (byte)iHello );
            rgBytes.Add( 0x00 );

            int iLoop = rgBytes.Count;

            rgBytes.Add( 0x7e ); // ld a (hl)
            rgBytes.Add( 0xfe ); // cmp 0
            rgBytes.Add( 0x00 );
            rgBytes.Add( 0xca ); // C2 jmp nz nn; CA-> JMP Z NN
            rgBytes.Add( (byte)iStart );
            rgBytes.Add( 0x00 );
            rgBytes.Add( 0xd3 ); // Out, n
            rgBytes.Add( 0x03 );
            rgBytes.Add( 0x23 ); // inc hl
            rgBytes.Add( 0xc3 ); // jp nn
            rgBytes.Add( (byte)iLoop );
            rgBytes.Add( 0x00 );

            return rgBytes.ToArray();
        }

        private static void Main(string[] args)
        {
            var ram = new byte[65536];
            Array.Clear(ram, 0, ram.Length);

            //var inp = File.ReadAllBytes("48.rom");
            //if( inp.Length != 16384 )
            //    throw new InvalidOperationException("Invalid 48.rom file");
            //Array.Copy(inp, ram, inp.Length);

            ushort iCount = 0;
            foreach( byte b in File.ReadAllBytes("tinybasic2dms.bin") ) {
                ram[iCount++] = b;
            }

            //ushort iCount = 0;
            //foreach( byte b in InitMe() ) {
            //    ram[iCount++] = b; 
            //}  

            var myZ80 = new Z80(new Memory(ram, iCount), new SamplePorts());
            Console.Clear();
            //var counter = 0;
            while (!myZ80.Halt)
            {
                myZ80.Parse();
                //counter++;
                //if (counter % 1000 == 1)
                //{
                //    if (Console.KeyAvailable)
                //        break;
                //    var registers = myZ80.GetState();
                //    Console.WriteLine($"0x{(ushort)(registers[25] + (registers[24] << 8)):X4}");
                //}
            }


            Console.WriteLine(Environment.NewLine + myZ80.DumpState());
            for (var i = 0; i < 0x80; i++)
            {
                if (i % 16 == 0) Console.Write("{0:X4} | ", i);
                Console.Write("{0:x2} ", ram[i]);
                if (i % 8 == 7) Console.Write("  ");
                if (i % 16 == 15) Console.WriteLine();
            }
            Console.WriteLine();
            for (var i = 0x4000; i < 0x4100; i++)
            {
                if (i % 16 == 0) Console.Write("{0:X4} | ", i);
                Console.Write("{0:x2} ", ram[i]);
                if (i % 8 == 7) Console.Write("  ");
                if (i % 16 == 15) Console.WriteLine();
            }
        }
    }

//    Eight I/O bytes are used for each UART to access its registers. The following table shows,
//    where each register can be found. The base address used in the table is the lowest
//    I/O port number assigned.
//    The switch bit DLAB can be found in the line control
//    register LCR as bit 7 at I/O address base + 3.

//I/O port  Read (DLAB=0)                   Write (DLAB=0)          Read (DLAB=1)                   Write (DLAB=1)
//base	    RBR receiver buffer             THR transmitter holding DLL divisor latch LSB           DLL divisor latch LSB
//base+1	IER interrupt enable            IER interrupt enable    DLM divisor latch MSB	        DLM divisor latch MSB
//base+2	IIR interrupt identification	FCR FIFO control	    IIR interrupt identification	FCR FIFO control
//base+3	LCR line control	            LCR line control        LCR line control                LCR line control
//base+4	MCR modem control	            MCR modem control	    MCR modem control	            MCR modem control
//base+5	LSR line status                 factory test	        LSR line status	                factory test
//base+6	MSR modem status	            not used	            MSR modem status	            not used
//base+7	SCR scratch	                    SCR scratch	            SCR scratch	                    SCR scratch

    class SamplePorts : IPorts
    {
        const int UART_LSR_ERR = 0x80; // Error
        const int UART_LSR_ETX = 0x40; // Transmit empty
        const int UART_LSR_ETH = 0x20; // Transmit holding register empty
        const int UART_LSR_RDY = 0x01; // Data ready

        const int REG_IER = 1; // Interrupt enable
            //0	Received data available
            //1	Transmitter holding register empty
            //2	Receiver line status register change
            //3	Modem status register change

        /// <summary>Divisor latch low</summary>
        const int REG_DLL = 0; 
        /// <summary>Divisor latch high</summary>
        const int REG_DLH = 1;
            //Speed (bps)	Divisor	DLL	DLM
            //  50      2,304	0x00	0x09
            //  300     384	    0x80	0x01
            //  1,200	96	    0x60	0x00
            //  2,400	48	    0x30	0x00
            //  4,800	24	    0x18	0x00
            //  9,600	12	    0x0C	0x00
            //  19,200	6	    0x06	0x00
            //  38,400	3	    0x03	0x00
            //  57,600	2	    0x02	0x00
            //  115,200	1	    0x01	0x00
        
     
        readonly Action<UART, byte> [,] _rgWritMatrix = new Action<UART,byte>[2,8];
        readonly Func  <UART, byte> [,] _rgReadMatrix = new Func  <UART,byte>[2,8];

        public byte EmptyReadMethod( UART oUart ) {
            return 0;
        }

        public void EmptyWriteMethod( UART oUart, byte b ) {
        }

        /// <summary>
        /// data bits, stop bits, parity, break sig, dlab.
        /// </summary>
        /// <param name="oUart"></param>
        /// <returns></returns>
        public byte Read_LineControl( UART oUart ) {
            return oUart._bLineControl; // 8 data bits, 1 stop bit, no parity
        }

            //1,0	xxxxxx00	5 data bits
            //	    xxxxxx01	6 data bits
            //	    xxxxxx10	7 data bits
            //	    xxxxxx11	8 data bits
            //2	    xxxxx0xx	1 stop bit
            //	    xxxxx1xx	1.5 stop bits (5 bits data word)
            //                  2 stop bits (6, 7 or 8 bits data word)
            //5,4,3	xxxx0xxx	No parity
            //	    xx001xxx	Odd parity
            //	    xx011xxx	Even parity
            //      xx101xxx    High parity (stick)

            //      xx111xxx	Low parity (stick)
            //6	    x0xxxxxx	Break signal disabled
            //	    x1xxxxxx	Break signal enabled
            //7	    0xxxxxxx	DLAB : RBR, THR and IER accessible
            //	    1xxxxxxx	DLAB : DLL and DLM accessible
        public void Writ_LineControl( UART oUart, byte bValue ) {
            oUart._iDLAB = (0b10000000 & bValue ) > 0 ? 1 : 0;

            bool fBreakSig = (0b01000000 & bValue) > 0;

            oUart._bLineControl = bValue;
        }

        enum LineStatus : byte {
            Data_available         = 0b00000001,
            Overrun_error          = 0b00000010,
            Parity_error           = 0b00000100,
            Framing_error          = 0b00001000,
            Break_signal_received  = 0b00010000,
            THR_is_empty           = 0b00100000,
            THR_is_empty_n_idle    = 0b01000000,
            Errornous_data_in_FIFO = 0b10000000
        } 
        /// <summary>
        /// Get the line status. Note: This can only be read by an IN command.
        /// </summary>
        /// <seealso cref="LineStatus"/>
        public byte Read_LineStatus( UART oUart ) {
            if( Console.KeyAvailable ) {
                return (byte)LineStatus.Data_available;
            }

            return (byte)LineStatus.THR_is_empty;
        }

        public byte Read_RX_Buffer( UART oUart ) {
            if( Console.KeyAvailable ) {
                ConsoleKeyInfo oKey = Console.ReadKey();
                return (byte)oKey.KeyChar;
            }
            return 0;
        }

        public void Writ_TX_Holding( UART oUart, byte bValue ) {
            Console.Write( (char)bValue );
        }

        protected byte SetBit( int iBit ) {
            switch( iBit ) {
                case 0:
                    return 1;
                case 1:
                    return 2;
                case 3:
                    return 4;
                case 4:
                    return 8;
                case 5:
                    return 16;
                case 6:
                    return 32;
                case 7:
                    return 64;
            }
            throw new ArgumentOutOfRangeException();
        }

            //int iStatus = ( bValue >> 1 & 0b00000111 );
            //int iFifo   = ( bValue >> 6 & 0b00000011 );
            //int iFifo64 = ( bValue >> 5 & 0b00000001 );
            //int iIterup = ( bValue  & 0b00000001 );
            //Bit	Value	Description	Reset by
            //0	    xxxxxxx0	Interrupt pending	–
            //	    xxxxxxx1	No interrupt pending	–
            //3,2,1	xxxx000x	Modem status change	MSR read
            //	    xxxx001x	Transmitter holding register empty	IIR read or THR write
            //	    xxxx010x	Received data available	RBR read
            //	    xxxx011x	Line status change	LSR read
            //	    xxxx110x	Character timeout (16550)	RBR read
            //4	    xxx0xxxx	Reserved	–
            //5	    xx0xxxxx	Reserved (8250, 16450, 16550)	–
            //	    xx1xxxxx	64 byte FIFO enabled (16750)	–
            //7,6	00xxxxxx	No FIFO	–
            //	    01xxxxxx	Unusable FIFO (16550 only)	–
            //	    11xxxxxx	FIFO enabled	–
        public byte Read_Interrupt_ID( UART oUart ) {
            if( Console.KeyAvailable )
                return 0b11000100;

           return 0b11000010;
        }

            //Bit	Value	Description
            //0	    xxxxxxx0	Disable FIFO’s
            //	    xxxxxxx1	Enable FIFO’s
            //1	    xxxxxx0x	–
            //	    xxxxxx1x	Clear receive FIFO
            //2	    xxxxx0xx	–
            //	    xxxxx1xx	Clear transmit FIFO
            //3	    xxxx0xxx	Select DMA mode 0
            //	    xxxx1xxx	Select DMA mode 1
            //4	    xxx0xxxx	Reserved
            //5	    xx0xxxxx	Reserved (8250, 16450, 16550)
            //	    xx1xxxxx	Enable 64 byte FIFO (16750)
            //		Receive FIFO interrupt trigger level:
            //7,6	00xxxxxx	1 byte
            //	    01xxxxxx	4 bytes
            //	    10xxxxxx	8 bytes
            //	    11xxxxxx	14 bytes

        /// <summary>
        /// This is a write only register...
        /// </summary>
        public void Writ_FIFO_Control( UART oUart, byte bValue ) {
            bool bFifoEna   = ( bValue & 0b00000001 ) > 0;
            bool bClrRxFifo = ( bValue & 0b00000010 ) > 0;
            bool bClrTxFifo = ( bValue & 0b00000100 ) > 0;
            int  iDMAMode   = ( bValue & 0b00001000 ) > 0 ? 1 : 0;
            bool bFifo64    = ( bValue & 0b00100000 ) > 0;
            int  iTrigger   =   bValue >> 6;
        }

        public class UART {
            public int  _iUart;
            public byte _bLineControl = 0b00000010; 
          //public byte _bLineStatus  = (int)LineStatus.THR_is_empty_and_line_is_idle;
            public int  _iDLAB = 0;
            public byte _bIIR;

            public UART( int iPort) {
                _iUart = iPort;
            }

            public override string ToString() {
                return "Uart " + _iUart.ToString();
            }
        }

        readonly Dictionary< int, UART > _dctUarts = new Dictionary<int, UART>();

        public SamplePorts() {
            for( int iDLAB = 0; iDLAB < 2; iDLAB++ ) {
                for( int j=0; j<8; j++ ) {
                    _rgReadMatrix[iDLAB,j] = EmptyReadMethod;
                    _rgWritMatrix[iDLAB,j] = EmptyWriteMethod;
                }
            }

            for( int iDLAB =0; iDLAB<2; iDLAB++ ) {
                _rgWritMatrix[iDLAB,3] = Writ_LineControl;
                _rgReadMatrix[iDLAB,3] = Read_LineControl;

                _rgReadMatrix[iDLAB,5] = Read_LineStatus;

                _rgReadMatrix[iDLAB,2] = Read_Interrupt_ID;
                _rgWritMatrix[iDLAB,2] = Writ_FIFO_Control;
            }

            _rgWritMatrix[0, 0] = Writ_TX_Holding;
            _rgReadMatrix[0, 0] = Read_RX_Buffer;
        }

        protected int DLAB( int iPortAddr ) {
            int iDLAB = 0;
            int iUart = iPortAddr / 8;

            if( _dctUarts.ContainsKey( iUart ) )
                iDLAB = _dctUarts[iUart]._iDLAB;

            return iDLAB;
        }

        protected UART FindUart( int iPortAddr ) {
            int iUart = iPortAddr / 8;

            if( _dctUarts.ContainsKey( iUart ) )
                return _dctUarts[iUart];

            UART oUart = new UART( iUart );
            _dctUarts.Add( iUart, oUart );

            return oUart;
        }


        public byte ReadPort(ushort sPortAddr)
        {
            //Console.WriteLine($"IN 0x{port:X4}");

            int iPort = sPortAddr % 8;
            int iDLAB = DLAB(sPortAddr);

            return _rgReadMatrix[iDLAB, iPort]( FindUart( sPortAddr ) );
        }
        public void WritePort(ushort sPortAddr, byte value)
        {
            int iPort = sPortAddr % 8;
            int iDLAB = DLAB(sPortAddr);

            //Console.WriteLine($"OUT 0x{port:X4}, 0x{value:X2}");
            //Console.Write( (char)value );

            _rgWritMatrix[iDLAB, iPort]( FindUart( sPortAddr ), value );
        }
        public bool NMI => false;
        public bool MI => false;
        public byte Data => 0x00;
    }
}