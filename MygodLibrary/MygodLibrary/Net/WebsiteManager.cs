using System;
using System.Diagnostics;
using System.Net;
using Mygod.Windows;

namespace Mygod.Net
{
    public static class WebsiteManager
    {
        private static readonly WebClient Client = new WebClient();

        private static string UpdateUrl => "https://mygod.be/product/update/" + CurrentApp.Version.Revision + '/';
        public static string Url => Client.DownloadString(UpdateUrl);

        public static void CheckForUpdates(Action noUpdates = null, Action<Exception> errorCallback = null)
        {
            try
            {
                var url = Url;
                if (!string.IsNullOrWhiteSpace(url)) Process.Start(url); else noUpdates?.Invoke();
            }
            catch (Exception e)
            {
                if (errorCallback == null) throw;
                errorCallback(e);
            }
        }
    }
}
