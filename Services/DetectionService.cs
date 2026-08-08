using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Deskout.Models;

namespace Deskout.Services
{
    public class DetectionResult
    {
        public bool UnityRunning { get; set; }
        public bool UnrealRunning { get; set; }
        public bool BlenderRunning { get; set; }
        public bool PremiereRunning { get; set; }
        public bool ActiveDownloads { get; set; }
        public List<string> UncommittedRepos { get; set; } = new();
        public List<string> ConnectedRemovableDrives { get; set; } = new();
        public bool OnBattery { get; set; }
        public float BatteryPercent { get; set; }

        public bool HasWarnings => UnityRunning || UnrealRunning || BlenderRunning || PremiereRunning || 
                                   ActiveDownloads || UncommittedRepos.Count > 0 || ConnectedRemovableDrives.Count > 0 || OnBattery;
    }

    public class DetectionService
    {
        private readonly ConfigService _configService;

        public DetectionService(ConfigService configService)
        {
            _configService = configService;
        }

        public DetectionResult RunChecks()
        {
            var result = new DetectionResult();

            // 1. Process Checks
            try
            {
                var processes = Process.GetProcesses();
                result.UnityRunning = processes.Any(p => p.ProcessName.Equals("Unity", StringComparison.OrdinalIgnoreCase));
                result.UnrealRunning = processes.Any(p => p.ProcessName.Equals("UnrealEditor", StringComparison.OrdinalIgnoreCase));
                result.BlenderRunning = processes.Any(p => p.ProcessName.Equals("blender", StringComparison.OrdinalIgnoreCase));
                result.PremiereRunning = processes.Any(p => p.ProcessName.Equals("Adobe Premiere Pro", StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking processes: {ex.Message}");
            }

            // 2. Downloads Check
            try
            {
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string downloadsPath = Path.Combine(userProfile, "Downloads");
                if (Directory.Exists(downloadsPath))
                {
                    // Look for chrome/edge temporary downloads (*.crdownload) or firefox (*.part)
                    var hasCrDownload = Directory.EnumerateFiles(downloadsPath, "*.crdownload").Any();
                    var hasPartDownload = Directory.EnumerateFiles(downloadsPath, "*.part").Any();
                    result.ActiveDownloads = hasCrDownload || hasPartDownload;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking downloads: {ex.Message}");
            }

            // 3. Git Status Check
            var gitRepos = _configService.Config.GitRepositories;
            if (gitRepos != null && gitRepos.Count > 0)
            {
                foreach (var repo in gitRepos)
                {
                    if (Directory.Exists(repo) && IsGitRepository(repo))
                    {
                        if (HasUncommittedChanges(repo))
                        {
                            result.UncommittedRepos.Add(new DirectoryInfo(repo).Name);
                        }
                    }
                }
            }

            // 4. External Drives Check
            try
            {
                var drives = DriveInfo.GetDrives();
                foreach (var drive in drives)
                {
                    if (drive.DriveType == DriveType.Removable && drive.IsReady)
                    {
                        result.ConnectedRemovableDrives.Add($"{drive.Name} ({drive.VolumeLabel})");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking removable drives: {ex.Message}");
            }

            // 5. Battery Check
            try
            {
                var powerStatus = SystemInformation.PowerStatus;
                result.OnBattery = powerStatus.PowerLineStatus == PowerLineStatus.Offline;
                result.BatteryPercent = powerStatus.BatteryLifePercent;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error checking battery: {ex.Message}");
            }

            return result;
        }

        private bool IsGitRepository(string path)
        {
            return Directory.Exists(Path.Combine(path, ".git"));
        }

        private bool HasUncommittedChanges(string repoPath)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "status --porcelain",
                    WorkingDirectory = repoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    return !string.IsNullOrWhiteSpace(output);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error running git status on {repoPath}: {ex.Message}");
            }
            return false;
        }
    }
}
