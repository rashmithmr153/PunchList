using Microsoft.Win32;

namespace PunchList.Services
{
    public class StartupService
    {
        public const string RUN_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        public const string APP_NAME = "PunchList";

        public bool IsAutoStartEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, false);
            return key?.GetValue(APP_NAME) != null;
        }

        public void SetAutoStart(bool enable)
        {
            using var key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
            if (key != null)
            {
                if (enable)
                {
                    string? exePath = Environment.ProcessPath;
                    if (!string.IsNullOrEmpty(exePath))
                    {
                        key.SetValue(APP_NAME, $"\"{exePath}\""); 
                    }

                }
                else
                {
                    key.DeleteValue(APP_NAME, false);
                }
            }
        }
    }
}
