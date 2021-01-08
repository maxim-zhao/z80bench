using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace z80bench.tests
{
    [TestClass]
    public class BenchmarkRunnerTests
    {
        [TestMethod]
        public void ThrowsExceptionWhenOverMaxCycles()
        {
            var benchmark = new Benchmark
            {
                Memory = new[]
                {
                    new Benchmark.Data
                    {
                        // ld a,7; add a,4; inc a; ret
                        // Total 7 + 7 + 4 + 10 = 28 cycles
                        Values = new byte[] {0x3E, 0x07, 0xC6, 0x04, 0x3C, 0xC9}, Offset = 0
                    }
                },
                MaxCycles = 27
            };

            Assert.ThrowsException<Exception>(() => new BenchmarkRunner(benchmark));
            benchmark.MaxCycles = 28;
            Assert.AreEqual(28, new BenchmarkRunner(benchmark).Cycles);
        }
    }
}