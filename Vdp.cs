namespace z80bench
{
    internal class Vdp
    {
        private bool _latched;
        public byte[] Vram { get; } = new byte[0x4000];
        private int _address;

        private enum Mode
        {
            Read = 0,
            Write = 1,
            // RegisterWrite = 2,
            // PaletteWrite = 3
        }
        private Mode _mode;
        private byte _readBuffer;

        public byte ReadData()
        {
            // Every time the data port is read (regardless
            // of the code register) the contents of a buffer are returned. The VDP will
            // then read a byte from VRAM at the current address, and increment the address
            // register. In this way data for the next data port read is ready with no
            // delay while the VDP reads VRAM. 
            var value = _readBuffer;
            BufferRead();
            _latched = false;
            return value;
        }

        public void WriteData(byte value)
        {
            if (_mode == Mode.Write)
            {
                Vram[_address++] = value;
                _address &= 0x3fff;
            }
            // An additional quirk is that writing to the
            // data port will also load the buffer with the value written.
            _readBuffer = value;
            _latched = false;
        }

        public void WriteControl(byte value)
        {
            if (!_latched)
            {
                // First byte
                // Update address immediately
                _address &= 0b111111_00000000;
                _address |= value;
                // Set latch
                _latched = true;
            }
            else
            {
                // Second byte
                // Apply bits to address
                _address &= 0b000000_11111111;
                _address |= (value & 0b111111) << 8;
                // Clear latch
                _latched = false;
                // Set mode
                _mode = (Mode) (value >> 6);
                // Pre-buffer on read
                if (_mode == Mode.Read)
                {
                    BufferRead();
                }
            }
        }

        private void BufferRead()
        {
            _readBuffer = Vram[_address++];
            _address &= 0x3fff;
        }

        public byte ReadControl()
        {
            _latched = false;
            // We always return the same value...
            return 0b10000000;
        }
    }
}