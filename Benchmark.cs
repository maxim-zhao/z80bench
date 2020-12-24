using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace z80bench
{
    /// <summary>
    /// This encapsulates a single benchmark for the <see cref="BenchmarkRunner"/>.
    /// </summary>
    public class Benchmark
    {
        /// <summary>
        /// The address to start execution at
        /// </summary>
        public int ExecutionAddress { get; set; }

        /// <summary>
        /// The maximum number of CPU cycles to emulate
        /// </summary>
        public long MaxCycles { get; set; }

        /// <summary>
        /// The initial value of the stack pointer
        /// </summary>
        public int StackPointer { get; set; }

        /// <summary>
        /// Data in memory (RAM)
        /// </summary>
        public IEnumerable<Data> Memory { get; set; }

        /// <summary>
        /// RAM comparisons to make after execution
        /// </summary>
        public IEnumerable<Data> RamComparisons { get; set; }

        /// <summary>
        /// VRAM comparisons to make after execution
        /// </summary>
        public IEnumerable<Data> VramComparisons { get; set; }

        /// <summary>
        /// Encapsulates some data at some offset
        /// </summary>
        public class Data
        {
            public byte[] Values { get; set; }
            public int Offset { get; set; }
        }


        /// <summary>
        /// Manual construction for testing
        /// </summary>
        public Benchmark()
        {
            // Default values
            ExecutionAddress = 0;
            MaxCycles = 1_000_000_000;
            StackPointer = 0xdff0;
            // These cannot be added to...
            Memory = RamComparisons = VramComparisons = new List<Data>();
        }

        /// <summary>
        /// Construction from an Options CLI class
        /// </summary>
        public Benchmark(Z80Bench.Options o)
        {
            ExecutionAddress = Convert.ToInt32(o.ExecutionAddress, 16);
            StackPointer = Convert.ToInt32(o.StackPointer, 16);
            MaxCycles = o.MaxCycles;
            Memory = o.Files.Select(ParseFileAndOffset);
            RamComparisons = o.RamComparisons.Select(ParseFileAndOffset);
            VramComparisons = o.VramComparisons.Select(ParseFileAndOffset);
        }

        private Data ParseFileAndOffset(string s)
        {
            var match = Regex.Match(s, "^(?<filename>[^@]+)(@(?<offset>[0-9a-fA-F]+))?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Could not parse parameter \"{s}\"");
            }

            var filename = match.Groups["filename"].Value;
            return new Data
            {
                Values = File.ReadAllBytes(filename),
                Offset = match.Groups["offset"].Success
                    ? Convert.ToInt32(match.Groups["offset"].Value, 16)
                    : 0
            };
        }
    }
}