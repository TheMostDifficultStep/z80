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
        private readonly byte[] _memory;
        private readonly ushort _ramStart;

        /// <param name="memory">The memory block for us to use.</param>
        /// <param name="ramStart">Where in this memory block it's ok to write.</param>
        public Z80Memory(byte[] memory, ushort ramStart)
        {
            _memory   = memory;
            _ramStart = ramStart;
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
                return _memory[iAddress];
            }
            // Set isn't part of the r/o List interface. :-/
            set {
                if (iAddress >= _ramStart)
                    _memory[iAddress] = value;
                else
                    throw new ArgumentOutOfRangeException();
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