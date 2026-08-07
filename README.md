# DESKOUT - Windows Shutdown Task Reminder

Deskout is a lightweight, premium Windows desktop utility built in **C# (.NET 8) and WPF** that runs in the system tray and intercepts Windows shutdown, restart, or logoff actions. If there are pending tasks for the day, it blocks the shutdown, displays a modern task checklist, and lets you either complete your tasks or snooze them.

---

## 🌟 Features

### 📅 V1: Core Blocker & Checklist
* **Shutdown Interception:** Hooks into Windows shutdown events using Win32 API hooks.
* **Checks Prevention:** Prevents shutdown and shows a native Windows *"Apps preventing shutdown"* screen displaying: *"Please complete your Deskout tasks before shutting down."*
* **Automatic Restoration:** If you click "Cancel" on the Windows screen, Deskout automatically pops up the checklist on your desktop.
* **Checklist View:** Displays active tasks with custom dark-themed checkbox controls.
* **Auto-saved Notes:** Quick text pad that automatically saves notes to keep track of tasks for the next day.

### ⚙️ V2: Customizations, Profiles & Snooze
* **Profiles (Work / Home):** Keep different task sets for different contexts. Easily switch profiles from settings or the system tray menu.
* **Day-of-Week Scheduling:** Schedule tasks for specific days (e.g. fill timesheets only on weekdays, run backups on Fridays).
* **Force Checklist Completion:** Optional setting that disables the "Shutdown Anyway" button until all active tasks are checked off.
* **Snooze Functionality:** Postpone reminders for 5, 15, 30, 60, or 120 minutes. It temporarily unblocks shutdown during the snooze period and reminds you when the timer expires.
* **Windows Auto-Startup:** Toggle running in the background automatically when Windows starts.

### 💻 V3: Developer Integration & Live Diagnostics
* **Process Detection:** Checks if key developer processes (like Unity, Unreal Editor, Blender, and Adobe Premiere) are running and warns you.
* **Active Downloads Check:** Scans your Downloads folder for active browser downloads (`*.crdownload` and `*.part` files) to prevent interrupting large downloads.
* **Git Status Inspector:** Runs `git status` on configured directories and alerts you if there are uncommitted changes.
* **External Drives Guard:** Warns if removable USB drives are still mounted.
* **UPS/Battery Check:** Monitors power status and warns you if running on battery.

---

## 🛠️ Technology Stack
* **Language:** C# 12 / .NET 8.0 (Windows target)
* **UI Framework:** WPF (Windows Presentation Foundation) with custom control templates and styles (dark theme, glassmorphic layout)
* **Background Tray:** Native Windows Forms `NotifyIcon` (embedded without external dependencies)
* **Storage:** JSON (`System.Text.Json`) serialized to `%LocalAppData%\Deskout\config.json`

---

## 📂 Project Directory Structure

```
Deskout/
│
├── Deskout.csproj          # Project metadata & Target Framework config
├── App.xaml / App.xaml.cs  # App entry point, single-instance mutex, and tray menu
├── install.ps1             # Installer script (copies files, sets registry & shortcuts)
├── uninstall.ps1           # Cleanup uninstaller script
│
├── Models/
│   ├── AppConfig.cs        # Main configuration schema
│   ├── Profile.cs          # Task profiles (Work/Home)
│   └── TaskItem.cs         # Checklist task structure
│
├── Services/
│   ├── ConfigService.cs    # Manages loading/saving settings to JSON
│   ├── ShutdownService.cs  # Win32 WndProc handler and shutdown execution
│   └── DetectionService.cs # Diagnostics (processes, Git, downloads, battery)
│
├── ViewModels/
│   ├── BaseViewModel.cs    # INotifyPropertyChanged base
│   ├── RelayCommand.cs     # Command bindings for WPF buttons
│   ├── ReminderViewModel.cs# ViewModel logic for checklist view
│   └── SettingsViewModel.cs# ViewModel logic for settings panel
│
├── Views/
│   ├── ReminderWindow.xaml # Main checklist view window
│   └── SettingsWindow.xaml # Settings editor dashboard
│
├── Converters/
│   └── BooleanToBrushConverter.cs # Helper for red/green UI alerts
│
└── Assets/
    └── icon.ico            # Main application brand icon
```

---

## ⚙️ Configuration Schema

Everything is stored in a single JSON file located at:
`%LocalAppData%\Deskout\config.json`

```json
{
  "CurrentProfile": "Office",
  "ForceComplete": false,
  "ShowOnRestart": true,
  "ShowOnLogoff": true,
  "SnoozeDurationMinutes": 15,
  "StartWithWindows": true,
  "DeveloperMode": false,
  "SavedNote": "Test note",
  "Profiles": [
    {
      "Name": "Office",
      "Tasks": [
        {
          "Id": "a98b449b-7cc2-4df4-8d48-69ce5b3b19b5",
          "Text": "Update Zoho Projects",
          "IsChecked": false,
          "DaysOfWeek": [1, 2, 3, 4, 5]
        }
      ]
    }
  ],
  "GitRepositories": []
}
```

---

## 🚀 Installation & Uninstallation

### Installing Deskout
1. Open a PowerShell console.
2. Run the installer script:
   ```powershell
   Set-ExecutionPolicy Bypass -Scope Process -Force
   .\install.ps1
   ```
   *The script builds the project, copies it to `%LocalAppData%\Programs\Deskout\`, registers the startup registry keys, creates a Start Menu shortcut, and launches it in background mode.*

### Uninstalling Deskout
1. Open PowerShell and run the uninstaller script:
   ```powershell
   .\uninstall.ps1
   ```
   *This stops any running instances and fully removes all files, shortcuts, and registry keys.*
