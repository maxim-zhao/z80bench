using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Konamiman.Z80dotNet;

namespace z80bench
{
    /// <summary>
    /// This class runs a Z80 benchmark. You must create it with a Benchmark instance (<see cref="Benchmark"/>).
    /// </summary>
    public class BenchmarkRunner
    {
        /// <summary>
        /// The number of CPU cycles emulated
        /// </summary>
        public long Cycles { get; }

        /// <summary>
        /// The wall-clock time spent on the benchmark
        /// </summary>
        public TimeSpan WallClockTime { get; }

        /// <summary>
        /// Any VRAM comparison mismatches found
        /// </summary>
        public List<Mismatch> VramMismatches { get; }

        /// <summary>
        /// Any RAM comparison mismatches found
        /// </summary>
        public List<Mismatch> RamMismatches { get; }

        public class Mismatch
        {
            public int Offset { get; set; }
            public int Expected { get; set; }
            public int Actual { get; set; }

            public override string ToString()
            {
                return $"{Offset:X4}: {Expected:X2} {Actual:X2}";
            }
        }

        public BenchmarkRunner(Benchmark settings)
        {
            // Create the emulator
            var emulator = new Z80Processor();

            // Insert the memory contents
            foreach (var item in settings.Memory)
            {
                emulator.Memory.SetContents(item.Offset, item.Values);
            }

            // Configure the CPU emulator...
            emulator.MemoryAccess += OnMemoryAccess;
            var counter = new CycleCountingClockSynchronizer(emulator, settings.MaxCycles);
            emulator.ClockSynchronizer = counter;
            emulator.AutoStopOnRetWithStackEmpty = true;
            emulator.InterruptMode = 1;
            emulator.Reset();
            // ReSharper disable once IntVariableOverflowInUncheckedContext
            emulator.Registers.SP = (short) settings.StackPointer;
            emulator.Registers.PC = (ushort) settings.ExecutionAddress;

            var sw = Stopwatch.StartNew();
            emulator.Start();
            sw.Stop();

            if (emulator.StopReason == StopReason.StopInvoked)
            {
                throw new Exception("Emulator stopped due to running for too long");
            }

            VramMismatches = checkComparisons(settings.VramComparisons, i => _vdp.Vram[i]).ToList();
            RamMismatches = checkComparisons(settings.RamComparisons, i => emulator.Memory[i]).ToList();

            sw.Stop();

            // Record state
            Cycles = counter.Cycles;
            WallClockTime = sw.Elapsed;
        }

        private IEnumerable<Mismatch> checkComparisons(IEnumerable<Benchmark.Data> comparisons, Func<int, byte> getter)
        {
            foreach (var comparison in comparisons)
            {
                // We then compare VRAM to the file passed, to confirm correctness
                for (int i = 0; i < comparison.Values.Length; ++i)
                {
                    int offset = i + comparison.Offset;
                    byte expected = comparison.Values[i];
                    byte actual = getter(offset);
                    if (expected != actual)
                    {
                        yield return new Mismatch {Offset = offset, Actual = actual, Expected = expected};
                    }
                }
            }
        }

        private readonly Vdp _vdp = new Vdp();

        private const ushort PortVdpData = 0xbe;
        private const ushort PortVdpControl = 0xbf;

        private void OnMemoryAccess(object sender, MemoryAccessEventArgs e)
        {
            switch (e.EventType)
            {
                case MemoryAccessEventType.BeforePortRead:
                    if (e.Address == PortVdpData)
                    {
                        e.Value = _vdp.ReadData();
                        e.CancelMemoryAccess = true;
                    }
                    else if (e.Address == PortVdpControl)
                    {
                        e.Value = _vdp.ReadControl();
                        e.CancelMemoryAccess = true;
                    }

                    break;
                case MemoryAccessEventType.AfterPortWrite:
                    if (e.Address == PortVdpData)
                    {
                        _vdp.WriteData(e.Value);
                    }
                    else if (e.Address == PortVdpControl)
                    {
                        _vdp.WriteControl(e.Value);
                    }

                    break;
                default:
                    return;
            }
        }
    }
}