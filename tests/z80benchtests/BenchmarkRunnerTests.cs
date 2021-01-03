using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace z80bench.tests {
    [TestClass]
    public class BenchmarkRunnerTests {
        [TestMethod]
        [ExpectedException(typeof(Exception), "Emulator stopped due to running for too long")]
        public void ThrowsExceptionWhenOverMaxCycles() {
            var benchmark = new Benchmark();
            var data = new z80bench.Benchmark.Data();
            var memory = new List<Benchmark.Data>();
            data.Values = new byte[6] { 0x3E, 0x07, 0xC6, 0x04, 0x3C, 0xC9 };
            data.Offset = 0;
            memory.Add(data);

            benchmark.MaxCycles = 1;
            benchmark.Memory = memory;

            new BenchmarkRunner(benchmark);
        }
    }
}
