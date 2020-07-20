using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Konamiman.Z80dotNet;

namespace z80bench
{
    class Z80Bench
    {
        private class FileWithOffset
        {
            public FileWithOffset(string arg)
            {
                var match = Regex.Match(arg, "^(?<filename>[^@]+)(@(?<offset>[0-9a-fA-F]+))?$");
                if (!match.Success)
                {
                    throw new ArgumentException($"Could not parse parameter \"{arg}\"");
                }

                Filename = match.Groups["filename"].Value;
                if (match.Groups["offset"].Success)
                {
                    Offset = Convert.ToInt32(match.Groups["offset"].Value, 16);
                }
                else
                {
                    Offset = 0;
                }
            }

            public int Offset { get;  }

            public string Filename { get;  }
        }

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                // Print usage
                Console.WriteLine("Usage: z80bench <program file>[@offset] [filename[@offset]] ... [--execute offset] [--stack-pointer offset] [--vram-compare filename[@offset]] ");
                Console.WriteLine("- Files will be inserted at the offset given, or $0000 if missing.");
                Console.WriteLine("- Program should end with a ret. No interrupts will happen.");
                Console.WriteLine("- Default program start address is $0000");
                Console.WriteLine("- Default stack pointer is $dff0");
                Console.WriteLine("- All offsets are bare hex, e.g. \"program.sms@0000\"");
                Console.WriteLine("- VRAM contents at the given address will be compared to the expected VRAM file. Program will return 0 if the data matches.");
                return -1;
            }

            var emulator = new Z80Processor();

            var startOffset = 0;
            var stackOffset = 0xdff0;
            var vramCompares = new List<FileWithOffset>();
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].StartsWith("--"))
                {
                    // Program option
                    var option = args[i].Substring(2);
                    if (++i >= args.Length)
                    {
                        // No space for arg
                        Console.Error.WriteLine($"Missing value for final parameter --{option}");
                        return -1;
                    }

                    switch (option.ToLowerInvariant())
                    {
                        case "execute":
                            startOffset = Convert.ToInt32(args[i], 16);
                            break;
                        case "stack-pointer":
                            stackOffset = Convert.ToInt32(args[i], 16);
                            break;
                        case "vram-compare":
                            vramCompares.Add(new FileWithOffset(args[i]));
                            break;
                        default:
                            Console.Error.WriteLine($"Unknown parameter --{option}");
                            return -1;
                    }
                }
                else
                {
                    // Program or data
                    var data = new FileWithOffset(args[i]);
                    emulator.Memory.SetContents(data.Offset, File.ReadAllBytes(data.Filename));
                }
            }

            emulator.MemoryAccess += OnMemoryAccess;
            var counter = new CycleCountingClockSynchronizer(emulator);
            emulator.ClockSynchronizer = counter;
            emulator.AutoStopOnRetWithStackEmpty = true;
            emulator.InterruptMode = 1;
            emulator.Reset();
            // ReSharper disable once IntVariableOverflowInUncheckedContext
            emulator.Registers.SP = (short) stackOffset;
            emulator.Registers.PC = (ushort) startOffset;

            var sw = Stopwatch.StartNew();
            emulator.Start();
            sw.Stop();

            if (emulator.StopReason == StopReason.StopInvoked)
            {
                Console.Error.WriteLine("Emulator stopped due to running for too long");
                return -1;
            }

            int mismatches = 0;
            foreach (var comparison in vramCompares)
            {
                // We then compare VRAM to the file passed, to confirm correctness
                var expectedData = File.ReadAllBytes(comparison.Filename);
                for (int i = 0; i < expectedData.Length; ++i)
                {
                    if (expectedData[i] != Vdp.Vram[i + comparison.Offset])
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
            }
            if (mismatches == 0 && vramCompares.Count > 0)
            {
                Console.WriteLine("VRAM comparison: pass");
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
