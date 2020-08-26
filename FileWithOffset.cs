using System;
using System.Text.RegularExpressions;

namespace z80bench
{
    internal class FileWithOffset
    {
        public FileWithOffset(string arg)
        {
            var match = Regex.Match(arg, "^(?<filename>[^@]+)(@(?<offset>[0-9a-fA-F]+))?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Could not parse parameter \"{arg}\"");
            }

            Filename = match.Groups["filename"].Value;
            if (match.Groups["offset"].Success)
            {
                Offset = Convert.ToInt32(match.Groups["offset"].Value, 16);
            }
            else
            {
                Offset = 0;
            }
        }

        public int Offset { get;  }

        public string Filename { get;  }
    }
}