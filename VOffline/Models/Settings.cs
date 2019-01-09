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

        public ImmutableHashSet<Mode> GetWorkingModes()
        {
            var unique = Modes.ToImmutableHashSet();

            if (!unique.Contains(Mode.All))
            {
                return unique; 
            }

            var goodEnumValues = Enum.GetValues(typeof(Mode))
                .Cast<Mode>()
                .Except(Enumerable.Repeat(Mode.All, 1));
            return goodEnumValues.ToImmutableHashSet();

        }
    }
}
