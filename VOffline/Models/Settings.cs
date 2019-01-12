using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace VOffline.Models
{
    public class Settings
    {
        public List<string> Targets { get; set; }
        public List<Mode> Modes { get; set; }
        public string OutputPath { get; set; }

        public IReadOnlyList<Mode> GetWorkingModes()
        {
            var unique = Modes.Distinct().ToList();
            if (!unique.Contains(Mode.All))
            {
                return unique
                    .ToList(); 
            }

            return Enum.GetValues(typeof(Mode))
                .Cast<Mode>()
                .Except(Enumerable.Repeat(Mode.All, 1))
                .OrderBy(x => x.ToString())
                .ToList();

        }
    }
}
