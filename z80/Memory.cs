using System;
using System.Collections;
using System.Collections.Generic;

namespace z80
{
    /// <summary>
    /// This object looks odd since if we have a _ramStart why not
    /// allocate less ram and then add that offset on each access?
    /// Need to look at that. I'm keeping this since we can also
    /// set memory break points with an object like this.
    /// </summary>
    public class Z80Memory :
        IReadOnlyCollection<byte>,
        IReadOnlyList      <byte>
    {
        private readonly byte[] _memory;
        private readonly ushort _ramStart;

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