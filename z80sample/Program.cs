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

        const int REG_RBR = 0; // Receive buffer (byte recieved)
        const int REG_THR = 0; // Transmitter holding (byte to tx)
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

        /// <summary>Interrupt identification</summary>
        const int REG_IIR = 2;
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
        /// <summary>Flow control FIFO control register..</summary>
        const int REG_FCT = 2;
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

        /// <summary>Line control</summary>
        const int REG_LCR = 3; /// <summary>Line control</summary>
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
        
        /// <summary>Modem control</summary>
        const int REG_MCR = 4; 
        /// <summary>Line status</summary>
        const int REG_LSR = 5; 
            //0	Data available
            //1	Overrun error
            //2	Parity error
            //3	Framing error
            //4	Break signal received
            //5	THR is empty
            //6	THR is empty, and line is idle
            //7	Errornous data in FIFO
        
        /// <summary>Modem status</summary>
        const int REG_MSR = 6;
        /// <summary>Scratch</summary>
        const int REG_SCR = 7; 

        const int UART0_PORT = 0xC0;
        const int UART1_PORT = 0xD0;

        readonly Dictionary<int, int>  _dctState = new Dictionary<int, int>();
        readonly Dictionary<int, byte> _dctLineCtrl = new Dictionary<int, byte>();

        public SamplePorts() {
            _dctState.Add( UART0_PORT, 0 );
            _dctState.Add( UART1_PORT, 0 );
        }

        public byte ReadPort(ushort sPortAddr)
        {
            //Console.WriteLine($"IN 0x{port:X4}");

            int iOffset = sPortAddr & 0xff;
            int iPort   = sPortAddr >> 8;

            switch( iOffset ) {
                case REG_THR: 
                    if( iPort == UART0_PORT ) {
                        if( Console.KeyAvailable ) {
                            _dctState[iPort] = 1;
                            return UART_LSR_RDY;
                        }
                    }
                    break;
                case REG_DLH:
                    break;
                case REG_FCT: /* REG_IIR & REG_FCT */
                    break;
                case REG_LCR: 
                    // return 0b0000011; // 8 data bits, 1 stop bit, no parity
                    // return _dctLineCtrl[iPort];
                    return 1;
                case REG_LSR:
                    if( _dctState[iPort] == 1 ) {
                        if( iPort == UART0_PORT ) {
                            ConsoleKeyInfo oKey = Console.ReadKey();
                            _dctState[iPort] = 0;
                            return (byte)oKey.KeyChar;
                        }
                    }
                    break;
            }


            return 0;
        }
        public void WritePort(ushort sPortAddr, byte value)
        {
            //Console.WriteLine($"OUT 0x{port:X4}, 0x{value:X2}");
            Console.Write( (char)value );

            int iOffset = sPortAddr & 0xff;
            int iPort   = sPortAddr >> 8;

            switch( iOffset ) {
                case REG_THR: 
                    Console.Write((char)value );
                    break;
                case REG_DLH:
                    break;
                case REG_FCT: /* REG_IIR & REG_FCT */
                    break;
                case REG_LCR: 
                    if( !_dctLineCtrl.ContainsKey( iPort ) )
                        _dctLineCtrl.Add( iPort, value );
                    break;
                case REG_LSR:
                    break;
            }

        }
        public bool NMI => false;
        public bool MI => false;
        public byte Data => 0x00;
    }
}