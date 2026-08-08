using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Forms;
using Deskout.Services;
using Deskout.ViewModels;
using Deskout.Views;
using System.Diagnostics;
using System.Windows.Threading;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Deskout
{
    public partial class App : Application
    {
        private static Mutex? _mutex;
        private NotifyIcon? _notifyIcon;
        
        private ConfigService? _configService;
        private ShutdownService? _shutdownService;
        private DetectionService? _detectionService;
        
        private ReminderViewModel? _reminderViewModel;
        private ReminderWindow? _reminderWindow;
        private SettingsWindow? _settingsWindow;
        
        private DispatcherTimer? _dailyReminderTimer;
        private readonly System.Collections.Generic.List<string> _triggeredTaskRemindersToday = new();

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Single Instance Check
            _mutex = new Mutex(true, "Local\\Deskout_Unique_Mutex_ID_10283", out bool isNewInstance);
            if (!isNewInstance)
            {
                MessageBox.Show("Deskout is already running in the system tray.", "Deskout", MessageBoxButton.OK, MessageBoxImage.Information);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            // 2. Initialize Services
            _configService = new ConfigService();
            _shutdownService = new ShutdownService();
            _detectionService = new DetectionService(_configService);

            // 3. Initialize ViewModels and Windows
            _reminderViewModel = new ReminderViewModel(_configService, _shutdownService, _detectionService);
            _reminderWindow = new ReminderWindow(_reminderViewModel);
            SetWindowIcon(_reminderWindow);

            // 4. Register Shutdown Service Hook
            var helper = new System.Windows.Interop.WindowInteropHelper(_reminderWindow);
            helper.EnsureHandle(); // Create window handle without showing window
            
            _shutdownService.HasIncompleteTasks = () => _reminderViewModel.TasksForToday.Any(t => !t.IsChecked);
            _shutdownService.OnShutdownCancelled = ShowReminderWindow;
            _shutdownService.RegisterHook(helper.Handle);

            // Set up show settings request
            _reminderViewModel.RequestShowSettings = ShowSettingsWindow;
            _reminderViewModel.RequestShowToast = ShowTrayBalloonTip;
            _reminderViewModel.RequestShowReminder = ShowReminderWindow;

            // 5. Initialize System Tray Icon
            InitializeTrayIcon();

            // 6. Initialize Daily Reminder Timer
            InitializeDailyReminderTimer();

            // 7. Proactive check if starting up with --background flag
            if (!e.Args.Contains("--background"))
            {
                // If started normally (not background on startup), show checklist or settings
                ShowReminderWindow();
            }
        }

        private void InitializeTrayIcon()
        {
            Icon iconValue = SystemIcons.Application;
            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
                var streamInfo = System.Windows.Application.GetResourceStream(iconUri);
                if (streamInfo != null)
                {
                    using (var stream = streamInfo.Stream)
                    {
                        iconValue = new Icon(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon from resources: {ex.Message}");
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                    if (File.Exists(iconPath))
                    {
                        iconValue = new Icon(iconPath);
                    }
                }
                catch { }
            }

            _notifyIcon = new NotifyIcon
            {
                Icon = iconValue,
                Text = "Deskout - Task Reminder",
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => ShowReminderWindow();

            var contextMenu = new ContextMenuStrip();
            
            var openItem = new ToolStripMenuItem("Open Checklist", null, (s, e) => ShowReminderWindow());
            var settingsItem = new ToolStripMenuItem("Settings Dashboard", null, (s, e) => ShowSettingsWindow());
            
            // Profiles submenu
            var profileSubMenu = new ToolStripMenuItem("Switch Profile");
            UpdateProfileSubMenu(profileSubMenu);
            profileSubMenu.DropDownOpening += (s, e) => UpdateProfileSubMenu(profileSubMenu);

            var exitItem = new ToolStripMenuItem("Exit Deskout", null, (s, e) => ExitApplication());

            contextMenu.Items.Add(openItem);
            contextMenu.Items.Add(settingsItem);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(profileSubMenu);
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add(exitItem);

            _notifyIcon.ContextMenuStrip = contextMenu;
        }

        private void UpdateProfileSubMenu(ToolStripMenuItem parentMenu)
        {
            parentMenu.DropDownItems.Clear();
            if (_configService == null || _reminderViewModel == null) return;

            foreach (var profile in _configService.Config.Profiles)
            {
                var item = new ToolStripMenuItem(profile.Name);
                item.Checked = profile.Name.Equals(_configService.Config.CurrentProfile, StringComparison.OrdinalIgnoreCase);
                item.Click += (s, e) =>
                {
                    _configService.Config.CurrentProfile = profile.Name;
                    _configService.SaveConfig();
                    _reminderViewModel.LoadTasksAndNote();
                    _shutdownService?.UpdateShutdownBlockState();
                };
                parentMenu.DropDownItems.Add(item);
            }
        }

        private void ShowReminderWindow()
        {
            if (_reminderWindow != null && _reminderViewModel != null)
            {
                _reminderViewModel.LoadTasksAndNote();
                _reminderWindow.Show();
                _reminderWindow.WindowState = WindowState.Normal;
                _reminderWindow.Activate();
                _reminderWindow.Focus();
                
                // Trigger live process checks
                _ = _reminderViewModel.RefreshDetectionAsync();
            }
        }

        private void ShowSettingsWindow()
        {
            if (_configService == null || _shutdownService == null || _reminderViewModel == null) return;

            if (_settingsWindow == null)
            {
                var settingsVm = new SettingsViewModel(_configService, _shutdownService);
                _settingsWindow = new SettingsWindow(settingsVm);
                _settingsWindow.Owner = _reminderWindow; // Stacks settings directly on top of topmost reminder window
                SetWindowIcon(_settingsWindow);
                _settingsWindow.Closed += (s, e) =>
                {
                    _settingsWindow = null;
                    // Reload tasks in reminder window when settings close
                    _reminderViewModel.LoadTasksAndNote();
                };
            }
            _settingsWindow.Show();
            _settingsWindow.Activate();
        }

        private void ShowTrayBalloonTip(string title, string text)
        {
            _notifyIcon?.ShowBalloonTip(3000, title, text, ToolTipIcon.Info);
        }

        private void ExitApplication()
        {
            // Destroy shutdown block reason so we don't prevent shutdown upon exiting
            if (_reminderWindow != null)
            {
                var helper = new System.Windows.Interop.WindowInteropHelper(_reminderWindow);
                if (helper.Handle != IntPtr.Zero)
                {
                    Helpers.Win32.ShutdownBlockReasonDestroy(helper.Handle);
                }
                _reminderWindow.SetTrulyClosing();
                _reminderWindow.Close();
            }

            if (_settingsWindow != null)
            {
                _settingsWindow.Close();
            }

            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }

            _mutex?.ReleaseMutex();
            _mutex?.Dispose();

            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            base.OnExit(e);
        }

        private void SetWindowIcon(Window window)
        {
            try
            {
                var iconUri = new Uri("pack://application:,,,/Assets/icon.ico", UriKind.Absolute);
                window.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(iconUri);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to set window icon from resources: {ex.Message}");
                try
                {
                    string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                    if (File.Exists(iconPath))
                    {
                        window.Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri(iconPath));
                    }
                }
                catch { }
            }
        }

        private void InitializeDailyReminderTimer()
        {
            _dailyReminderTimer = new DispatcherTimer();
            _dailyReminderTimer.Interval = TimeSpan.FromSeconds(30);
            _dailyReminderTimer.Tick += DailyReminderTimer_Tick;
            _dailyReminderTimer.Start();
        }

        private void DailyReminderTimer_Tick(object? sender, EventArgs e)
        {
            if (_configService == null || _reminderViewModel == null) return;

            var config = _configService.Config;
            string todayStr = DateTime.Today.ToString("yyyy-MM-dd");

            // Clear old entries from previous days to keep the list clean
            _triggeredTaskRemindersToday.RemoveAll(k => !k.EndsWith(todayStr));

            var profile = config.Profiles.FirstOrDefault(p => p.Name.Equals(config.CurrentProfile, StringComparison.OrdinalIgnoreCase));
            if (profile == null) return;

            var today = DateTime.Today.DayOfWeek;
            var tasksForToday = profile.Tasks.Where(t => t.DaysOfWeek.Count == 0 || t.DaysOfWeek.Contains(today)).ToList();

            bool showWindow = false;

            foreach (var task in tasksForToday)
            {
                if (string.IsNullOrWhiteSpace(task.ReminderTime)) continue;

                string triggerKey = $"{task.Id}_{todayStr}";
                if (_triggeredTaskRemindersToday.Contains(triggerKey)) continue;

                if (TryParseReminderTime(task.ReminderTime, out TimeSpan reminderTime))
                {
                    var nowTime = DateTime.Now.TimeOfDay;
                    // Check if we are within 2 minutes of the target time today
                    if (nowTime >= reminderTime && nowTime < reminderTime.Add(TimeSpan.FromMinutes(2)))
                    {
                        _triggeredTaskRemindersToday.Add(triggerKey);
                        showWindow = true;

                        if (!string.IsNullOrWhiteSpace(task.CustomUrl))
                        {
                            AutoLaunchUrl(task.CustomUrl);
                        }
                    }
                }
            }

            if (showWindow)
            {
                ShowReminderWindow();
            }
        }

        private bool TryParseReminderTime(string timeStr, out TimeSpan timeSpan)
        {
            timeSpan = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(timeStr)) return false;

            if (TimeSpan.TryParse(timeStr, out timeSpan)) return true;

            if (DateTime.TryParse(timeStr, out DateTime parsedDateTime))
            {
                timeSpan = parsedDateTime.TimeOfDay;
                return true;
            }

            return false;
        }

        private void AutoLaunchUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to open task URL: {ex.Message}");
            }
        }
    }
}
