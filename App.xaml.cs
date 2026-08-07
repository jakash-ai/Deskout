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

            // 4. Register Shutdown Service Hook
            var helper = new System.Windows.Interop.WindowInteropHelper(_reminderWindow);
            helper.EnsureHandle(); // Create window handle without showing window
            
            _shutdownService.HasIncompleteTasks = () => _reminderViewModel.TasksForToday.Any(t => !t.IsChecked);
            _shutdownService.OnShutdownCancelled = ShowReminderWindow;
            _shutdownService.RegisterHook(helper.Handle);

            // Set up show settings request
            _reminderViewModel.RequestShowSettings = ShowSettingsWindow;
            _reminderViewModel.RequestShowToast = ShowTrayBalloonTip;

            // 5. Initialize System Tray Icon
            InitializeTrayIcon();

            // 6. Proactive check if starting up with --background flag
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
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "icon.ico");
                if (File.Exists(iconPath))
                {
                    iconValue = new Icon(iconPath);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load tray icon: {ex.Message}");
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
    }
}
