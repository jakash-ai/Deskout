using System.Linq;
using System.Windows;

namespace Deskout.Helpers
{
    public static class WindowHelper
    {
        public static Window? GetActiveWindow()
        {
            return System.Windows.Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) 
                   ?? System.Windows.Application.Current.Windows.OfType<Window>().LastOrDefault();
        }

        public static string? ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            var dialog = new Views.CustomInputDialog(title, prompt, defaultValue)
            {
                Owner = GetActiveWindow(),
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Topmost = true
            };

            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                return dialog.InputText;
            }
            return null;
        }
    }
}
