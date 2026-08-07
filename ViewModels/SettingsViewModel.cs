using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Input;
using System.Windows.Forms;
using Microsoft.Win32;
using Deskout.Models;
using Deskout.Services;

namespace Deskout.ViewModels
{
    public class TaskSettingsItem : BaseViewModel
    {
        private readonly TaskItem _task;
        private readonly Action _onChanged;

        public TaskItem Model => _task;

        public string Text
        {
            get => _task.Text;
            set
            {
                if (_task.Text != value)
                {
                    _task.Text = value;
                    OnPropertyChanged(nameof(Text));
                    _onChanged();
                }
            }
        }

        public bool IsMonday
        {
            get => HasDay(DayOfWeek.Monday);
            set => ToggleDay(DayOfWeek.Monday, value);
        }

        public bool IsTuesday
        {
            get => HasDay(DayOfWeek.Tuesday);
            set => ToggleDay(DayOfWeek.Tuesday, value);
        }

        public bool IsWednesday
        {
            get => HasDay(DayOfWeek.Wednesday);
            set => ToggleDay(DayOfWeek.Wednesday, value);
        }

        public bool IsThursday
        {
            get => HasDay(DayOfWeek.Thursday);
            set => ToggleDay(DayOfWeek.Thursday, value);
        }

        public bool IsFriday
        {
            get => HasDay(DayOfWeek.Friday);
            set => ToggleDay(DayOfWeek.Friday, value);
        }

        public bool IsSaturday
        {
            get => HasDay(DayOfWeek.Saturday);
            set => ToggleDay(DayOfWeek.Saturday, value);
        }

        public bool IsSunday
        {
            get => HasDay(DayOfWeek.Sunday);
            set => ToggleDay(DayOfWeek.Sunday, value);
        }

        public TaskSettingsItem(TaskItem task, Action onChanged)
        {
            _task = task;
            _onChanged = onChanged;
        }

        private bool HasDay(DayOfWeek day)
        {
            return _task.DaysOfWeek.Contains(day);
        }

        private void ToggleDay(DayOfWeek day, bool add)
        {
            if (add)
            {
                if (!_task.DaysOfWeek.Contains(day))
                {
                    _task.DaysOfWeek.Add(day);
                    _onChanged();
                }
            }
            else
            {
                if (_task.DaysOfWeek.Remove(day))
                {
                    _onChanged();
                }
            }
            OnPropertyChanged(nameof(IsMonday));
            OnPropertyChanged(nameof(IsTuesday));
            OnPropertyChanged(nameof(IsWednesday));
            OnPropertyChanged(nameof(IsThursday));
            OnPropertyChanged(nameof(IsFriday));
            OnPropertyChanged(nameof(IsSaturday));
            OnPropertyChanged(nameof(IsSunday));
        }
    }

    public class SettingsViewModel : BaseViewModel
    {
        private readonly ConfigService _configService;
        private readonly ShutdownService _shutdownService;

        private ObservableCollection<Profile> _profiles = new();
        private Profile? _selectedProfile;
        private ObservableCollection<TaskSettingsItem> _tasks = new();
        private ObservableCollection<string> _gitRepositories = new();

        private bool _forceComplete;
        private bool _showOnRestart;
        private bool _showOnLogoff;
        private bool _developerMode;
        private bool _startWithWindows;

        public ObservableCollection<Profile> Profiles
        {
            get => _profiles;
            set => SetField(ref _profiles, value);
        }

        public Profile? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (SetField(ref _selectedProfile, value))
                {
                    LoadTasksForSelectedProfile();
                    OnPropertyChanged(nameof(CanDeleteProfile));
                }
            }
        }

        public ObservableCollection<TaskSettingsItem> Tasks
        {
            get => _tasks;
            set => SetField(ref _tasks, value);
        }

        public ObservableCollection<string> GitRepositories
        {
            get => _gitRepositories;
            set => SetField(ref _gitRepositories, value);
        }

        public bool ForceComplete
        {
            get => _forceComplete;
            set
            {
                if (SetField(ref _forceComplete, value))
                {
                    _configService.Config.ForceComplete = value;
                    SaveSettings();
                }
            }
        }

        public bool ShowOnRestart
        {
            get => _showOnRestart;
            set
            {
                if (SetField(ref _showOnRestart, value))
                {
                    _configService.Config.ShowOnRestart = value;
                    SaveSettings();
                }
            }
        }

        public bool ShowOnLogoff
        {
            get => _showOnLogoff;
            set
            {
                if (SetField(ref _showOnLogoff, value))
                {
                    _configService.Config.ShowOnLogoff = value;
                    SaveSettings();
                }
            }
        }

        public bool DeveloperMode
        {
            get => _developerMode;
            set
            {
                if (SetField(ref _developerMode, value))
                {
                    _configService.Config.DeveloperMode = value;
                    SaveSettings();
                }
            }
        }

        public bool StartWithWindows
        {
            get => _startWithWindows;
            set
            {
                if (SetField(ref _startWithWindows, value))
                {
                    _configService.Config.StartWithWindows = value;
                    UpdateStartupRegistry(value);
                    SaveSettings();
                }
            }
        }

        public bool CanDeleteProfile => Profiles.Count > 1;

        // Commands
        public ICommand AddProfileCommand { get; }
        public ICommand DeleteProfileCommand { get; }
        public ICommand AddTaskCommand { get; }
        public ICommand DeleteTaskCommand { get; }
        public ICommand AddGitRepoCommand { get; }
        public ICommand DeleteGitRepoCommand { get; }

        public SettingsViewModel(ConfigService configService, ShutdownService shutdownService)
        {
            _configService = configService;
            _shutdownService = shutdownService;

            AddProfileCommand = new RelayCommand(_ => AddProfile());
            DeleteProfileCommand = new RelayCommand(_ => DeleteProfile(), _ => CanDeleteProfile);
            AddTaskCommand = new RelayCommand(_ => AddTask());
            DeleteTaskCommand = new RelayCommand(p => DeleteTask(p));
            AddGitRepoCommand = new RelayCommand(_ => AddGitRepo());
            DeleteGitRepoCommand = new RelayCommand(p => DeleteGitRepo(p));

            LoadConfigValues();
        }

        private void LoadConfigValues()
        {
            var config = _configService.Config;
            ForceComplete = config.ForceComplete;
            ShowOnRestart = config.ShowOnRestart;
            ShowOnLogoff = config.ShowOnLogoff;
            DeveloperMode = config.DeveloperMode;
            StartWithWindows = config.StartWithWindows;

            Profiles = new ObservableCollection<Profile>(config.Profiles);
            GitRepositories = new ObservableCollection<string>(config.GitRepositories ?? new List<string>());

            SelectedProfile = Profiles.FirstOrDefault(p => p.Name.Equals(config.CurrentProfile, StringComparison.OrdinalIgnoreCase)) 
                              ?? Profiles.FirstOrDefault();
        }

        private void LoadTasksForSelectedProfile()
        {
            if (SelectedProfile != null)
            {
                var wrappedTasks = SelectedProfile.Tasks.Select(t => new TaskSettingsItem(t, SaveSettings));
                Tasks = new ObservableCollection<TaskSettingsItem>(wrappedTasks);
                _configService.Config.CurrentProfile = SelectedProfile.Name;
                SaveSettings();
            }
            else
            {
                Tasks = new ObservableCollection<TaskSettingsItem>();
            }
        }

        private void SaveSettings()
        {
            _configService.SaveConfig();
            _shutdownService.UpdateShutdownBlockState();
        }

        private void AddProfile()
        {
            string profileName = Microsoft.VisualBasic.Interaction.InputBox("Enter Profile Name:", "New Profile", "Work");
            profileName = profileName.Trim();
            if (string.IsNullOrEmpty(profileName)) return;

            if (Profiles.Any(p => p.Name.Equals(profileName, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("A profile with that name already exists.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var newProfile = new Profile { Name = profileName };
            Profiles.Add(newProfile);
            _configService.Config.Profiles.Add(newProfile);
            SaveSettings();
            SelectedProfile = newProfile;
        }

        private void DeleteProfile()
        {
            if (SelectedProfile == null || !CanDeleteProfile) return;

            var profileToDelete = SelectedProfile;
            var confirmResult = MessageBox.Show($"Are you sure you want to delete profile '{profileToDelete.Name}'?", "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmResult == DialogResult.Yes)
            {
                int index = Profiles.IndexOf(profileToDelete);
                Profiles.Remove(profileToDelete);
                _configService.Config.Profiles.Remove(profileToDelete);
                SaveSettings();

                // Select another profile
                int nextIndex = Math.Max(0, index - 1);
                SelectedProfile = Profiles.ElementAtOrDefault(nextIndex);
            }
        }

        private void AddTask()
        {
            if (SelectedProfile == null) return;

            string taskText = Microsoft.VisualBasic.Interaction.InputBox("Enter Task Details:", "New Task", "");
            taskText = taskText.Trim();
            if (string.IsNullOrEmpty(taskText)) return;

            var newTask = new TaskItem
            {
                Text = taskText,
                DaysOfWeek = new List<DayOfWeek> { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday } // Weekdays default
            };

            SelectedProfile.Tasks.Add(newTask);
            Tasks.Add(new TaskSettingsItem(newTask, SaveSettings));
            SaveSettings();
        }

        private void DeleteTask(object? parameter)
        {
            if (SelectedProfile == null || parameter is not TaskSettingsItem taskWrapper) return;

            SelectedProfile.Tasks.Remove(taskWrapper.Model);
            Tasks.Remove(taskWrapper);
            SaveSettings();
        }

        private void AddGitRepo()
        {
            using var dialog = new FolderBrowserDialog();
            dialog.Description = "Select Git Repository Directory";
            dialog.UseDescriptionForTitle = true;

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string path = dialog.SelectedPath;
                if (!GitRepositories.Contains(path))
                {
                    GitRepositories.Add(path);
                    _configService.Config.GitRepositories.Add(path);
                    SaveSettings();
                }
            }
        }

        private void DeleteGitRepo(object? parameter)
        {
            if (parameter is string path)
            {
                GitRepositories.Remove(path);
                _configService.Config.GitRepositories.Remove(path);
                SaveSettings();
            }
        }

        private void UpdateStartupRegistry(bool startWithWindows)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    string appPath = Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;
                    if (startWithWindows)
                    {
                        if (!string.IsNullOrEmpty(appPath))
                        {
                            // Save with --background argument
                            key.SetValue("Deskout", $"\"{appPath}\" --background");
                        }
                    }
                    else
                    {
                        key.DeleteValue("Deskout", false);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to update startup registry: {ex.Message}");
            }
        }
    }
}
