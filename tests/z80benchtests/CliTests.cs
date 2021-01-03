using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;

namespace z80bench.tests {
    [TestClass]
    public class CliTests {
        private StringWriter _stdOut;
        private StringWriter _stdErr;
        private static readonly string[] _parameters = {
            "--execute=<address>",
            "--stack-pointer=<address>",
            "--max-cycles=<count>",
            "--vram-compare=<filename>[@<offset>]"
        };

        [TestInitialize()]
        public void Setup() {
            // Override console standard and error output streams before each test
            _stdOut = new StringWriter();
            _stdErr = new StringWriter();
            Console.SetOut(_stdOut);
            Console.SetError(_stdErr);
        }

        [TestMethod]
        public void ExitsWithErrorWithoutArguments() {
            // Execute with no parameters and collect the output
            int result;
            List<string> output;
            result = Z80Bench.Main(new string[] { });
            output = _stdOut.ToString()
                .Split("\n")
                .Select(x => x.Trim())
                .ToList();

            // Expect -1 for an error
            Assert.AreEqual(result, -1);

            // Expect the first line to be usage info
            Assert.AreEqual(output[0], "Usage: z80bench <filename[@address]> [additional files] [options]");

            // Expect the various parameters to be in the usage info
            foreach (var line in _parameters) {
                Assert.IsTrue(output.Any(x => x.Contains(line)));
            }
        }

        [TestMethod]
        public void ExitsWithErrorWithUnknownArgument() {
            // Execute with unknown parameter and collect output
            int result = Z80Bench.Main(new string[] { "--unknown-arg", "unknownValue" });
            string output = _stdOut.ToString();

            // Expect -1 for an error
            Assert.AreEqual(-1, result);

            // Expect an errors section and for it to contain a message about the unknown parameter
            Assert.IsTrue(output.Contains("Errors:"));
            Assert.IsTrue(output.Contains("Option 'unknown-arg' is unknown."));
        }
    }
}
