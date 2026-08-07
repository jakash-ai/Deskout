using System;
using System.Diagnostics;
using System.Windows.Interop;
using Deskout.Helpers;

namespace Deskout.Services
{
    public class ShutdownService
    {
        private IntPtr _hwnd;
        private bool _shutdownInitiated;
        private string _lastShutdownType = "shutdown"; // "shutdown", "restart", "logoff"

        public Func<bool>? HasIncompleteTasks { get; set; }
        public Action? OnShutdownCancelled { get; set; }

        public bool ShutdownInitiated => _shutdownInitiated;
        public string LastShutdownType => _lastShutdownType;

        public void RegisterHook(IntPtr hwnd)
        {
            _hwnd = hwnd;
            HwndSource source = HwndSource.FromHwnd(hwnd);
            source.AddHook(WndProc);

            // Proactively register block if there are incomplete tasks
            UpdateShutdownBlockState();
        }

        public void UpdateShutdownBlockState()
        {
            if (_hwnd == IntPtr.Zero) return;

            bool block = HasIncompleteTasks?.Invoke() ?? false;
            if (block)
            {
                Win32.ShutdownBlockReasonCreate(_hwnd, "Please complete your Deskout tasks before shutting down.");
            }
            else
            {
                Win32.ShutdownBlockReasonDestroy(_hwnd);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Win32.WM_QUERYENDSESSION)
            {
                // Detect shutdown type
                long flags = lParam.ToInt64();
                if ((flags & Win32.ENDSESSION_LOGOFF) != 0)
                {
                    _lastShutdownType = "logoff";
                }
                else
                {
                    // Note: restart cannot be distinguished from shutdown in WM_QUERYENDSESSION easily.
                    // We default to "shutdown" but let user choose in UI if needed.
                    _lastShutdownType = "shutdown";
                }

                bool shouldBlock = HasIncompleteTasks?.Invoke() ?? false;
                if (shouldBlock)
                {
                    // Block shutdown
                    handled = true;
                    _shutdownInitiated = true;
                    
                    // Register reason again just to be safe
                    Win32.ShutdownBlockReasonCreate(hwnd, "Please complete your Deskout tasks before shutting down.");
                    
                    return IntPtr.Zero; // return false to block
                }
                else
                {
                    // Allow shutdown
                    return new IntPtr(1); // return true to allow
                }
            }
            else if (msg == Win32.WM_ENDSESSION)
            {
                bool isEnding = wParam != IntPtr.Zero;
                if (!isEnding)
                {
                    // Shutdown was cancelled (either user clicked Cancel, or another app blocked)
                    if (_shutdownInitiated)
                    {
                        _shutdownInitiated = false;
                        // Notify that shutdown was cancelled, so we show our checklist window
                        OnShutdownCancelled?.Invoke();
                    }
                }
            }

            return IntPtr.Zero;
        }

        public void PerformShutdown(string type)
        {
            // First destroy the block reason
            if (_hwnd != IntPtr.Zero)
            {
                Win32.ShutdownBlockReasonDestroy(_hwnd);
            }

            // Execute the system shutdown
            string args = type switch
            {
                "restart" => "/r /t 0 /f",
                "logoff" => "/l",
                _ => "/s /t 0 /f"
            };

            try
            {
                Process.Start(new ProcessStartInfo("shutdown", args)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to execute shutdown command: {ex.Message}");
            }
        }
    }
}
