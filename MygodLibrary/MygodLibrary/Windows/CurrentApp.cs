using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Mygod.Windows
{
    public static partial class CurrentApp
    {
        private static AssemblyName NowAssemblyName => Assembly.GetEntryAssembly().GetName();
        public static string Name => NowAssemblyName.Name;
        public static Version Version => NowAssemblyName.Version;
        public static string Title => $"{Name} {Version.Major}.{Version.Minor}.{Version.Build}";
        public static string Path => Assembly.GetEntryAssembly().Location;
        public static string Directory => AppDomain.CurrentDomain.BaseDirectory;

        public static DateTime CompilationTime
        {
            get
            {
                var b = new byte[2048];
                using (var s = new FileStream(Assembly.GetEntryAssembly().Location, FileMode.Open, FileAccess.Read)) s.Read(b, 0, 2048);
                var dt = new DateTime(1970, 1, 1).AddSeconds(BitConverter.ToInt32(b, BitConverter.ToInt32(b, 60) + 8));
                return dt + TimeZone.CurrentTimeZone.GetUtcOffset(dt);
            }
        }
        public static string FileName => Process.GetCurrentProcess().MainModule.FileName;
    }
}
