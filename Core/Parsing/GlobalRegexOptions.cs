using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace ScheduleBuilder.Core.Parsing
{
    internal static class GlobalRegexOptions
    {
        public const RegexOptions Value = RegexOptions.Multiline | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled;

    }
}
