using System;
using System.Diagnostics;
using System.IO;
using Konamiman.Z80dotNet;

namespace z80bench
{
    class Z80Bench
    {
        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                // Print usage
                Console.WriteLine("Usage: z80bench <program file> [data file] [expected VRAM file]");
                Console.WriteLine("- Program file will be executed at address $0000. It should end with a ret. No interrupts will happen.");
                Console.WriteLine("- Data file will be inserted at address $4000 if given");
                Console.WriteLine("- VRAM contents will be compared to the expected VRAM file if given");
                return -1;
            }

            var emulator = new Z80Processor();
            // The first parameter is the program to run. We put it at location 0.
            emulator.Memory.SetContents(0, File.ReadAllBytes(args[0]));
            int dataLength = 0;
            if (args.Length > 1)
            {
                // The second parameter is the data. We assume we can put it at 16KB.
                var data = File.ReadAllBytes(args[1]);
                dataLength = data.Length;
                emulator.Memory.SetContents(0x4000, data);
            }

            emulator.MemoryAccess += OnMemoryAccess;
            var counter = new CycleCountingClockSynchronizer(emulator);
            emulator.ClockSynchronizer = counter;
            emulator.AutoStopOnRetWithStackEmpty = true;
            emulator.InterruptMode = 1;
            emulator.Reset();
            emulator.Registers.SP = 0xdff0 - 0x10000; // Need to supply it as signed short, this is the equivalent 

            var sw = Stopwatch.StartNew();
            emulator.Start();
            sw.Stop();

            if (emulator.StopReason == StopReason.StopInvoked)
            {
                Console.Error.WriteLine("Emulator stopped due to running for too long");
                return -1;
            }

            int mismatches = 0;
            if (args.Length > 2)
            {
                // We then compare VRAM to the file passed third, to confirm correctness
                var expectedData = File.ReadAllBytes(args[2]);
                for (int i = 0; i < expectedData.Length; ++i)
                {
                    if (expectedData[i] != Vdp.Vram[i])
                    {
                        if (mismatches == 0)
                        {
                            Console.WriteLine("VRAM comparison: fail");
                            // Write a header
                            Console.WriteLine("Address: Expected Actual");
                        }

                        Console.WriteLine($"{i:X4}: {expectedData[i]:X2} {Vdp.Vram[i]:X2}");
                        ++mismatches;
                    }
                }

                if (mismatches == 0)
                {
                    Console.WriteLine("VRAM comparison: pass");
                }

                Console.WriteLine($"Compression level is 1:{(double)expectedData.Length / dataLength:N2} = {(double)(expectedData.Length - dataLength) / expectedData.Length:P1} compression");
                Console.WriteLine($"Data rate is {(double)expectedData.Length / counter.Cycles * 59736:N2} bytes per frame at NTSC timings");

            }

            Console.WriteLine($"Executed {counter.Cycles} cycles in {sw.Elapsed}");

            return mismatches;
        }

        private class CycleCountingClockSynchronizer : IClockSynchronizer
        {
            private readonly Z80Processor _emulator;

            public CycleCountingClockSynchronizer(Z80Processor emulator)
            {
                _emulator = emulator;
            }

            public void Start()
            {
            }

            public void Stop()
            {
            }

            public void TryWait(int periodLengthInCycles)
            {
                Cycles += periodLengthInCycles;
                if (Cycles > 1_000_000_000_000)
                {
                    // Probably going on too long
                    _emulator.Stop();
                }
            }

            public long Cycles { get; private set; }

            public decimal EffectiveClockFrequencyInMHz { get; set; }
        }

        private class VDP
        {
            private bool _latched;
            public byte[] Vram { get; } = new byte[0x4000];
            private int _address;

            private enum Mode
            {
                Read = 0,
                Write = 1,
                RegisterWrite = 2,
                PaletteWrite = 3
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
        }

        private static readonly VDP Vdp = new VDP();

        private const ushort PortVdpData = 0xbe;
        private const ushort PortVdpControl = 0xbf;

        private static void OnMemoryAccess(object sender, MemoryAccessEventArgs e)
        {
            switch (e.EventType)
            {
                case MemoryAccessEventType.BeforePortRead:
                    if (e.Address == PortVdpData)
                    {
                        e.Value = Vdp.ReadData();
                        e.CancelMemoryAccess = true;
                    }
                    break;
                case MemoryAccessEventType.AfterPortWrite:
                    if (e.Address == PortVdpData)
                    {
                        Vdp.WriteData(e.Value);
                    }
                    else if (e.Address == PortVdpControl)
                    {
                        Vdp.WriteControl(e.Value);
                    }
                    break;
                default:
                    return;
            }
        }
    }
}
