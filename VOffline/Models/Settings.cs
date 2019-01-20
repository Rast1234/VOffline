using System;
using System.Collections.Generic;
using System.Linq;

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

        public string UserAgent { get; set; } = "KateMobileAndroid/51.2 lite-443 (Android 4.4.2; SDK 19; x86; unknown Android SDK built for x86; en)";
        public int RequestRetryCount { get; set; } = 1;
        public TimeSpan RequestRetryDelay { get; set; } = TimeSpan.FromSeconds(3);
        public int ParallelDownloadsLimit { get; set; } = 10;
        public int DownloadsErrorLimit { get; set; } = 3;
        public int TaskErrorLimit { get; set; } = 2;
    }
}
