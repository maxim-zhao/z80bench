using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Konamiman.Z80dotNet;

namespace z80bench
{
    public class Z80Bench
    {
        public static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                // Print usage
                Console.WriteLine("Usage: z80bench <filename@offset> [additional files] [options]");
                Console.WriteLine("Filenames will be inserted into Z80 address space at the address given, or 0 if unspecified.");
                Console.WriteLine("You should load some code at a suitable address, and end with ret. No interrupts will happen.");
                Console.WriteLine("Options:");
                Console.WriteLine("--execute <offset>                   Start execution at the given offset (hex). Default is 0.");
                Console.WriteLine("--stack-pointer <offset>             Set the stack pointer to the given offset (hex). Default is dff0.");
                Console.WriteLine("--max-cycles <count>                 Limit Z80 execution to the given number. Default is 1e9.");
                Console.WriteLine("--vram-compare filename[@offset]]    Compare an emulated Sega 8-bit system's VRAM at the given offset to the specified file");
                return -1;
            }

            var emulator = new Z80Processor();

            var startOffset = 0;
            var stackOffset = 0xdff0;
            var maxCycles = 1_000_000_000; // 4.1 minutes at 4MHz
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
                        case "max-cycles":
                            maxCycles = Convert.ToInt32(args[i]);
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
            var counter = new CycleCountingClockSynchronizer(emulator, maxCycles);
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
                    else if (e.Address == PortVdpControl)
                    {
                        e.Value = Vdp.ReadControl();
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
