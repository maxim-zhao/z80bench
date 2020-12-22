using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace z80bench.tests {
    [TestClass]
    public class CliTests {
        [TestMethod]
        public void ExitsWithErrorWithoutArguments()
        {
            // Execute with no parameters and collect the output
            int result;
            List<string> output;
            using (var sw = new StringWriter())
            {
                Console.SetOut(sw);
                result = Z80Bench.Main(new string[] { });
                output = sw.ToString()
                    .Split("\n")
                    .Select(x => x.Trim())
                    .ToList();
            }

            // Expect -1 for an error
            Assert.AreEqual(result, -1);
            
            // Expect the first line to be usage info
            Assert.AreEqual(output[0], "Usage: z80bench <filename@offset> [additional files] [options]");

            // Expect the various parameters to be in the usage info
            foreach (var line in new[] {
                "--execute <offset>",
                "--stack-pointer <offset>",
                "--max-cycles <count>",
                "--vram-compare filename[@offset]]"
            }) {
                Assert.IsTrue(output.Any(x => x.StartsWith(line)));
            }
        }
    }
}
