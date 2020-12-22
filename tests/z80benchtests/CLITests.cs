using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using z80bench;

namespace z80benchtests {
    [TestClass]
    public class CLITests {
        private static readonly string[] _expectedErrorOutput = {
            "--execute <offset>",
            "--stack-pointer <offset>",
            "--max-cycles <count>",
            "--vram-compare filename[@offset]]"
        };

        [TestMethod]
        public void ExitsWithErrorWithoutArguments() {
            using (var sw = new StringWriter()) {
                Console.SetOut(sw);
                int result = Z80Bench.Main(new string[] { });
                string[] output = sw.ToString().Split("\n");
                Assert.AreEqual(result, -1);
                Assert.AreEqual(output[0].Trim(), "Usage: z80bench <filename@offset> [additional files] [options]");

                foreach (string line in _expectedErrorOutput) {
                    Assert.IsTrue(Array.Find(output, outputLine => outputLine.StartsWith(line)) != null);
                }
            }
        }
    }
}
