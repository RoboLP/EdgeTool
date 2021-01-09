using System;
using System.Linq;
using System.Net.NetworkInformation;

namespace Mygod.Net.NetworkInformation
{
    public static class NetworkTester
    {
        /// <summary>
        /// Indicates whether any network connection is available.
        /// Filter connections below a specified speed, as well as virtual network cards.
        /// 
        /// Based on: http://stackoverflow.com/a/8345173/2245107
        /// </summary>
        /// <param name="minimumSpeed">The minimum speed required. Passing 0 will not filter connection using speed.
        /// </param>
        /// <returns>
        ///     <c>true</c> if a network connection is available; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsNetworkAvailable(long minimumSpeed = 0)
        {
            return NetworkInterface.GetIsNetworkAvailable() && NetworkInterface.GetAllNetworkInterfaces().Any(ni =>
                ni.OperationalStatus == OperationalStatus.Up &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel && ni.Speed >= minimumSpeed &&
                ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) < 0 &&
                ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) < 0 &&
                !ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase));
        }
    }
}
