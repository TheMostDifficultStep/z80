using System;
using System.Collections;
using System.Collections.Generic;

namespace z80
{
    /// <summary>
    /// Gated memory access object. The memory space for the program
    /// is considered ROM. Everything above the ramStart is RAM.
    /// Technically you could debug your program and then just replace
    /// this object with a normal byte array. :-/
    /// </summary>
    public class Z80Memory :
        IReadOnlyCollection<byte>,
        IReadOnlyList      <byte>
    {
        private byte[] _memory;
        private ushort _ramStart;

        /// <param name="memory"  >The memory block for us to use.</param>
        /// <param name="ramStart">Where in this memory block it's ok to write.</param>
        public Z80Memory(byte[] memory, ushort ramStart)
        {
            Reset( memory, ramStart, fComFile:false );
        }

        public Z80Memory() {
            _memory   = new byte[0];
            _ramStart = 0;
        }

        /// <summary>
        /// Use this to access the array directly. I need the
        /// exceptions and not the "safe" access by our normal
        /// this[] operator.
        /// </summary>
        /// <remarks>If it's a COM file then it starts at 0x100 and
        /// we have room for our CPM low memory stuff...</remarks>
        public byte[] RawMemory => _memory;

        public void Reset( byte[] memory, ushort usProgEnd, bool fComFile ) {
            _memory   = memory;
            _ramStart = usProgEnd;

            // Generated from CPMfake.asm...
            if( fComFile ) {
                _memory[0] = 0xc3; // Jp to 0x100
                _memory[1] = 0x00;
                _memory[2] = 0x01;
                _memory[3] = 0x00;
                _memory[4] = 0x00;
                _memory[5] = 0x3e; // ld a, 0x11
                _memory[6] = 0x11; 
                _memory[7] = 0xb9; // cp c
                _memory[8] = 0x28; // jr z, 1
                _memory[9] = 0x01; 
                _memory[10] = 0xc9; // return
                _memory[10] = 0xdb; // in a, (0x02) ... was ld a, 0x0 (comstat)
                _memory[11] = 0x02;
                _memory[12] = 0xc9; // return
            }
        }

        public int RamStart => _ramStart;

        /// <summary>
        /// We set the "ram" start, as where
        /// variables live, this keeps the program
        /// out of the stack and data area. O.o Kewl!
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException" />
        public byte this[int iAddress] {
            get {
                try {
                    return _memory[iAddress];
                } catch( IndexOutOfRangeException ) {
                    return 0x76; // Halt
                }
            }
            // Set isn't part of the r/o List interface. :-/
            set {
                // First keep the program out of it's own memory.
                if (iAddress >= _ramStart)
                    _memory[iAddress] = value;
                else {
                    // Second, allow it a little bit into my CP/M low area.
                    if( iAddress > 0x5 && iAddress < 0x100 )
                        _memory[iAddress] = value;
                    else
                        throw new IndexOutOfRangeException();
                }
            }
        }

        public int Count => _memory.Length - _ramStart;

        public IEnumerator<byte> GetEnumerator() {
            for( int i=_ramStart; i < _memory.Length; ++i ) 
                yield return _memory[i];
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}