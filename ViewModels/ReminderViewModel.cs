using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using Deskout.Models;
using Deskout.Services;

namespace Deskout.ViewModels
{
    public class ReminderViewModel : BaseViewModel
    {
        private readonly ConfigService _configService;
        private readonly ShutdownService _shutdownService;
        private readonly DetectionService _detectionService;
        private readonly DispatcherTimer _snoozeTimer;

        private ObservableCollection<TaskItem> _tasksForToday = new();
        private string _note = string.Empty;
        private DetectionResult? _detection;
        private bool _isSnoozed;
        private string _snoozeStatusText = string.Empty;
        private bool _isDetecting;

        public ObservableCollection<TaskItem> TasksForToday
        {
            get => _tasksForToday;
            set => SetField(ref _tasksForToday, value);
        }

        public string Note
        {
            get => _note;
            set
            {
                if (SetField(ref _note, value))
                {
                    _configService.Config.SavedNote = value;
                    _configService.SaveConfig();
                }
            }
        }

        public DetectionResult? Detection
        {
            get => _detection;
            set => SetField(ref _detection, value);
        }

        public bool IsSnoozed
        {
            get => _isSnoozed;
            set
            {
                SetField(ref _isSnoozed, value);
                OnPropertyChanged(nameof(SnoozeActiveVisibility));
            }
        }

        public string SnoozeStatusText
        {
            get => _snoozeStatusText;
            set => SetField(ref _snoozeStatusText, value);
        }

        public bool IsDetecting
        {
            get => _isDetecting;
            set => SetField(ref _isDetecting, value);
        }

        public bool DeveloperMode => _configService.Config.DeveloperMode;
        public string CurrentProfileName => _configService.Config.CurrentProfile;
        public bool ForceComplete => _configService.Config.ForceComplete;

        public bool IsShutdownAnywayEnabled
        {
            get
            {
                if (!ForceComplete) return true;
                return TasksForToday.All(t => t.IsChecked);
            }
        }

        public bool SnoozeActiveVisibility => IsSnoozed;

        // Commands
        public ICommand ToggleTaskCommand { get; }
        public ICommand ShutdownAnywayCommand { get; }
        public ICommand RestartAnywayCommand { get; }
        public ICommand LogoffAnywayCommand { get; }
        public ICommand CancelShutdownCommand { get; }
        public ICommand SnoozeCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand RefreshDetectionCommand { get; }

        public Action? RequestClose { get; set; }
        public Action? RequestShowSettings { get; set; }
        public Action<string, string>? RequestShowToast { get; set; }

        public ReminderViewModel(ConfigService configService, ShutdownService shutdownService, DetectionService detectionService)
        {
            _configService = configService;
            _shutdownService = shutdownService;
            _detectionService = detectionService;

            _snoozeTimer = new DispatcherTimer();
            _snoozeTimer.Tick += SnoozeTimer_Tick;

            ToggleTaskCommand = new RelayCommand(_ => ToggleTask());
            ShutdownAnywayCommand = new RelayCommand(_ => PerformShutdown("shutdown"));
            RestartAnywayCommand = new RelayCommand(_ => PerformShutdown("restart"));
            LogoffAnywayCommand = new RelayCommand(_ => PerformShutdown("logoff"));
            CancelShutdownCommand = new RelayCommand(_ => CancelShutdown());
            SnoozeCommand = new RelayCommand(p => Snooze(p));
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings());
            RefreshDetectionCommand = new RelayCommand(async _ => await RefreshDetectionAsync());

            LoadTasksAndNote();
        }

        public void LoadTasksAndNote()
        {
            // Reset checked state if day changed
            CheckAndResetDailyTasks();

            Note = _configService.Config.SavedNote;

            var profile = _configService.Config.Profiles.FirstOrDefault(p => p.Name.Equals(_configService.Config.CurrentProfile, StringComparison.OrdinalIgnoreCase));
            if (profile != null)
            {
                var today = DateTime.Today.DayOfWeek;
                var filtered = profile.Tasks.Where(t => t.DaysOfWeek.Count == 0 || t.DaysOfWeek.Contains(today)).ToList();
                TasksForToday = new ObservableCollection<TaskItem>(filtered);
            }
            else
            {
                TasksForToday = new ObservableCollection<TaskItem>();
            }

            OnPropertyChanged(nameof(DeveloperMode));
            OnPropertyChanged(nameof(CurrentProfileName));
            OnPropertyChanged(nameof(ForceComplete));
            OnPropertyChanged(nameof(IsShutdownAnywayEnabled));

            _shutdownService.UpdateShutdownBlockState();
        }

        private static string s_lastResetDate = string.Empty;
        private void CheckAndResetDailyTasks()
        {
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");
            if (s_lastResetDate != todayStr)
            {
                s_lastResetDate = todayStr;
                foreach (var profile in _configService.Config.Profiles)
                {
                    foreach (var task in profile.Tasks)
                    {
                        task.IsChecked = false;
                    }
                }
                _configService.SaveConfig();
            }
        }

        private void ToggleTask()
        {
            _configService.SaveConfig();
            OnPropertyChanged(nameof(IsShutdownAnywayEnabled));
            _shutdownService.UpdateShutdownBlockState();
        }

        private void PerformShutdown(string type)
        {
            IsSnoozed = false;
            _snoozeTimer.Stop();
            _shutdownService.PerformShutdown(type);
            RequestClose?.Invoke();
        }

        private void CancelShutdown()
        {
            // Simply hide/close window, shutdown is already cancelled by blocking WM_QUERYENDSESSION
            RequestClose?.Invoke();
        }

        private void Snooze(object? parameter)
        {
            if (parameter is string minutesStr && int.TryParse(minutesStr, out int minutes))
            {
                IsSnoozed = true;
                SnoozeStatusText = $"Snoozed for {minutes} min";
                
                // Temporarily remove block reason so they can shut down if they wish during snooze
                _shutdownService.HasIncompleteTasks = () => false;
                _shutdownService.UpdateShutdownBlockState();

                _snoozeTimer.Interval = TimeSpan.FromMinutes(minutes);
                _snoozeTimer.Start();

                RequestShowToast?.Invoke("Deskout Snoozed", $"Reminder snoozed for {minutes} minutes.");
                RequestClose?.Invoke();
            }
        }

        private void SnoozeTimer_Tick(object? sender, EventArgs e)
        {
            _snoozeTimer.Stop();
            IsSnoozed = false;

            // Restore the shutdown blocking behavior
            _shutdownService.HasIncompleteTasks = () => TasksForToday.Any(t => !t.IsChecked);
            _shutdownService.UpdateShutdownBlockState();

            // Notify user and pop up the window again if they are on the desktop
            RequestShowToast?.Invoke("Deskout Alert", "Snooze expired! Please complete your remaining tasks.");
        }

        private void OpenSettings()
        {
            RequestShowSettings?.Invoke();
        }

        public async Task RefreshDetectionAsync()
        {
            if (IsDetecting) return;
            IsDetecting = true;

            try
            {
                Detection = await Task.Run(() => _detectionService.RunChecks());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to run detection checks: {ex.Message}");
            }
            finally
            {
                IsDetecting = false;
            }
        }
    }
}
