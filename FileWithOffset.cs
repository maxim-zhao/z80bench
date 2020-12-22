using System;
using System.Text.RegularExpressions;

namespace z80bench
{
    /// <summary>
    /// This class helps parse filenames with optional offsets.
    /// </summary>
    internal class FileWithOffset
    {
        /// <summary>
        /// Pass a filename parameter and we can either parse it as a filename with implicit zero offset,
        /// or an explicit offset after an @ sign. We do not check it is a valid filename.
        /// </summary>
        /// <param name="s">The parameter to parse</param>
        public FileWithOffset(string s)
        {
            var match = Regex.Match(s, "^(?<filename>[^@]+)(@(?<offset>[0-9a-fA-F]+))?$");
            if (!match.Success)
            {
                throw new ArgumentException($"Could not parse parameter \"{s}\"");
            }

            Filename = match.Groups["filename"].Value;
            Offset = match.Groups["offset"].Success 
                ? Convert.ToInt32(match.Groups["offset"].Value, 16) 
                : 0;
        }

        /// <summary>
        /// The offset for the file data to be placed at
        /// </summary>
        public int Offset { get;  }

        /// <summary>
        /// The name of the file to read data from
        /// </summary>
        public string Filename { get;  }
    }
}