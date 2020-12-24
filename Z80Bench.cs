using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;

namespace z80bench
{
    public class Z80Bench
    {
        public class Options
        {
            [Value(0, Required = true, MetaName = "(files)", MetaValue = "<filename>[@<offset>]", HelpText = "Filenames with an optional hex address after an @ sign. Data will be inserted into Z80 address space at the address given, or 0 if unspecified.")]
            public IEnumerable<string> Files { get; set; }
            [Option('e', "execute", MetaValue = "<address>", Required = false, HelpText = "Start execution at the given offset (hex)", Default = "0")]
            public string ExecutionAddress { get; set; }
            [Option('s', "stack-pointer", MetaValue = "<address>", Required = false, HelpText = "Set the stack pointer to the given offset (hex)", Default = "dff0")]
            public string StackPointer { get; set; }
            [Option('m', "max-cycles", MetaValue = "<count>", Required = false, HelpText = "Limit Z80 execution to the given number of CPU cycles", Default = 1_000_000_000)]
            public long MaxCycles { get; set; }
            [Option('v', "vram-compare", MetaValue = "<filename>[@<offset>]", Required = false, HelpText = "Compare VRAM contents to the given file with optional offset. More than one may be specified.")]
            public IEnumerable<string> VramComparisons { get; set; }
            [Option('r', "ram-compare", MetaValue = "<filename>[@<offset>]", Required = false, HelpText = "Compare RAM contents to the given file with optional offset. More than one may be specified.")]
            public IEnumerable<string> RamComparisons { get; set; }
        }

        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                    Run,
                    errors =>
                    {
                        foreach (var error in errors)
                        {
                            Console.Error.WriteLine(error);
                        }

                        return -1;
                    });
        }

        private static int Run(Options options)
        {
            try
            {
                // Convert options to real data
                var benchmark = new Benchmark(options);

                // Run the benchmark
                var runner = new BenchmarkRunner(benchmark);

                // Print any mismatches
                PrintMismatches(benchmark.VramComparisons, runner.VramMismatches, "VRAM");
                PrintMismatches(benchmark.RamComparisons, runner.RamMismatches, "RAM");

                // Print the clock time
                Console.WriteLine($"Executed {runner.Cycles} cycles in {runner.WallClockTime}");

                // Return the result (0 on success)
                return runner.RamMismatches.Count + runner.VramMismatches.Count;

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex);
                return -1;
            }
        }

        private static void PrintMismatches(IEnumerable<Benchmark.Data> comparisons, IList<BenchmarkRunner.Mismatch> mismatches, string description)
        {
            if (!comparisons.Any())
            {
                return;
            }
            if (mismatches.Any())
            {
                Console.WriteLine($"{description} comparison: fail");
                // Write a header
                Console.WriteLine("Address: Expected Actual");
                foreach (var mismatch in mismatches)
                {
                    Console.WriteLine(mismatch);
                }
            }
            else
            {
                Console.WriteLine($"{description} comparison: pass");
            }

        }
    }
}
