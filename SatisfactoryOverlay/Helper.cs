namespace SatisfactoryOverlay
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    static class Helper
    {
        public static void OpenUrlInBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch (Win32Exception w32ex)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else
                {
                    throw new PlatformNotSupportedException("Can not open url on current operating system.", w32ex);
                }
            }
        }

        public static void OpenUrlInBrowser(Uri uri) => OpenUrlInBrowser(uri.AbsoluteUri);

        [DebuggerHidden]
        [Conditional("DEBUG")]
        public static void DebugBreak()
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}
