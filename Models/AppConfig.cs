using System;
using System.Collections.Generic;

namespace Deskout.Models
{
    public class AppConfig
    {
        public string CurrentProfile { get; set; } = "Office";
        public bool ForceComplete { get; set; } = true;
        public bool ShowOnRestart { get; set; } = true;
        public bool ShowOnLogoff { get; set; } = true;
        public int SnoozeDurationMinutes { get; set; } = 15;
        public bool StartWithWindows { get; set; } = true;
        public bool DeveloperMode { get; set; }
        public string SavedNote { get; set; } = string.Empty;
        public List<Profile> Profiles { get; set; } = new();
        public List<string> GitRepositories { get; set; } = new();
    }
}
