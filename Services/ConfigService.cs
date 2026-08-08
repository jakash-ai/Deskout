using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Deskout.Models;

namespace Deskout.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;
        private AppConfig _config;

        public AppConfig Config => _config;

        public ConfigService()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string deskoutPath = Path.Combine(appDataPath, "Deskout");
            Directory.CreateDirectory(deskoutPath);
            _configFilePath = Path.Combine(deskoutPath, "config.json");

            _config = LoadConfig();
        }

        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    string json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<AppConfig>(json);
                    if (config != null)
                    {
                        return config;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load config: {ex.Message}");
            }

            // Return default config if loading fails or file does not exist
            return CreateDefaultConfig();
        }

        public void SaveConfig()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save config: {ex.Message}");
            }
        }

        private AppConfig CreateDefaultConfig()
        {
            var config = new AppConfig
            {
                CurrentProfile = "Office",
                ForceComplete = true,
                ShowOnRestart = true,
                ShowOnLogoff = true,
                SnoozeDurationMinutes = 15,
                StartWithWindows = true,
                DeveloperMode = false,
                SavedNote = string.Empty,
                GitRepositories = new List<string>()
            };

            var weekdays = new List<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday
            };

            var mtwtfs = new List<DayOfWeek>
            {
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday,
                DayOfWeek.Friday,
                DayOfWeek.Saturday
            };

            var allDays = new List<DayOfWeek>
            {
                DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
                DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday
            };

            var officeProfile = new Profile
            {
                Name = "Office",
                Tasks = new List<TaskItem>
                {
                    new() { Text = "Update zoho project", IsChecked = false, DaysOfWeek = mtwtfs, ReminderTime = "7:25 PM" },
                    new() { Text = "Fill Zoho People Timesheet", IsChecked = false, DaysOfWeek = weekdays },
                    new() { Text = "Push Git Changes", IsChecked = false, DaysOfWeek = weekdays },
                    new() { Text = "Backup Current Work", IsChecked = false, DaysOfWeek = allDays },
                    new() { Text = "Close Unreal/Unity Properly", IsChecked = false, DaysOfWeek = allDays }
                }
            };

            var homeProfile = new Profile
            {
                Name = "Home",
                Tasks = new List<TaskItem>
                {
                    new() { Text = "Backup Photos", IsChecked = false, DaysOfWeek = allDays },
                    new() { Text = "Sync OneDrive", IsChecked = false, DaysOfWeek = allDays },
                    new() { Text = "Charge Laptop", IsChecked = false, DaysOfWeek = allDays }
                }
            };

            config.Profiles.Add(officeProfile);
            config.Profiles.Add(homeProfile);

            return config;
        }
    }
}
