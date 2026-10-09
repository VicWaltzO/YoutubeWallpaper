using System.Security.Principal;

namespace YoutubeWallpaper;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        string user = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        using var showPanel = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\YoutubeWallpaper.Show." + user);
        using var instance = new Mutex(true, @"Local\YoutubeWallpaper.Instance." + user, out bool firstInstance);
        if (!firstInstance)
        {
            showPanel.Set();
            return;
        }
        try
        {
            using var form = new Form1();
            using var notifications = new System.Windows.Forms.Timer { Interval = 400 };
            notifications.Tick += (_, _) => { if (showPanel.WaitOne(0)) form.ShowConfiguration(); };
            notifications.Start();
            Application.Run(form);
        }
        finally { instance.ReleaseMutex(); }
    }
}
