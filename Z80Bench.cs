using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine;
using CommandLine.Text;

namespace z80bench
{
    public static class Z80Bench
    {
        // ReSharper disable once ClassNeverInstantiated.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global
        public class Options
        {
            [Value(0, 
                Required = true, 
                Hidden = true,
                MetaName = "(files)", 
                MetaValue = "<filename>[@<offset>]")]
            public IEnumerable<string> Files { get; set; }

            [Option('e', "execute", 
                MetaValue = "<address>", 
                Required = false,
                HelpText = "Start execution at the given offset (hex)", 
                Default = "0")]
            public string ExecutionAddress { get; set; }

            [Option('s', "stack-pointer", 
                MetaValue = "<address>", 
                Required = false,
                HelpText = "Set the stack pointer to the given offset (hex)", 
                Default = "dff0")]
            public string StackPointer { get; set; }

            [Option('m', "max-cycles", 
                MetaValue = "<count>", 
                Required = false,
                HelpText = "Limit Z80 execution to the given number of CPU cycles", 
                Default = 1_000_000_000)]
            public long MaxCycles { get; set; }

            [Option('v', "vram-compare", 
                MetaValue = "<filename>[@<offset>]", 
                Required = false,
                HelpText = "Compare VRAM contents to the given file with optional offset. More than one may be specified.")]
            public IEnumerable<string> VramComparisons { get; set; }

            [Option('r', "ram-compare", 
                MetaValue = "<filename>[@<offset>]", 
                Required = false,
                HelpText = "Compare RAM contents to the given file with optional offset. More than one may be specified.")]
            public IEnumerable<string> RamComparisons { get; set; }
        }
        // ReSharper restore UnusedAutoPropertyAccessor.Global

        public static int Main(string[] args)
        {
            var parser = new Parser(config =>
            {
                config.HelpWriter = null;
                config.AutoVersion = false;
            });
            var parserResult = parser.ParseArguments<Options>(args);

            return parserResult.MapResult(Run, errors => HandleError(errors, parserResult));
        }

        private static int HandleError(IEnumerable<Error> errors, ParserResult<Options> parserResult)
        {
            // We replace CommandLine's default error/help implementation to gain some extra control over its formatting...
            var h = new HelpText
            {
                Heading = "Usage: z80bench <filename[@address]> [additional files] [options]",
                AutoVersion = false,
                AddDashesToOption = true
            };
            h.AddPreOptionsLine("Filenames will be inserted into Z80 address space at the (hex) address given, or 0 if unspecified.");
            h.AddPreOptionsLine("You should load some code at a suitable address, and end with ret. No interrupts will happen.");
            h.AddPreOptionsLine("");
            h.AddPreOptionsLine("Options:");
            h.AddOptions(parserResult);

            if (!errors.All(x => x is HelpRequestedError))
            {
                h.AddPostOptionsLine("Errors:");
                h.AddPostOptionsLines(HelpText.RenderParsingErrorsTextAsLines(
                    parserResult, 
                    error => error is MissingRequiredOptionError 
                        ? "At least one filename must be given" : 
                        h.SentenceBuilder.FormatError(error), 
                    h.SentenceBuilder.FormatMutuallyExclusiveSetErrors, 2));
            }

            Console.WriteLine(h);
            return -1;
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