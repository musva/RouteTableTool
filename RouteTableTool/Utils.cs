using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RouteTableTool
{
    public class Utils
    {
        public static void Purge(ref List<string> needToPurge)
        {

            for (int i = 0; i < needToPurge.Count - 1; i++)
            {
                string deststring = needToPurge[i];
                for (int j = i + 1; j < needToPurge.Count; j++)
                {
                    if (deststring.CompareTo(needToPurge[j]) == 0)
                    {
                        needToPurge.RemoveAt(j);
                        j--;
                        continue;
                    }
                }
            }
        }

        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);
        public static void FlushMemory()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
            }
        }

        public static void ShowNetworkInfo()
        {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                Debug.WriteLine(adapter.Name);
                Debug.WriteLine(adapter.Description);
                Debug.WriteLine(adapter.GetPhysicalAddress().ToString());
                Debug.WriteLine(properties.GetIPv4Properties().Index.ToString());
                Debug.WriteLine(properties.UnicastAddresses.Where(i => i.Address.AddressFamily == AddressFamily.InterNetwork).ToList().Select(i => i.Address.ToString()).Aggregate((i, j) => i + "," + j));
                if (properties.GatewayAddresses.Count > 0)
                    Debug.WriteLine(properties.GatewayAddresses.Where(i => i != null && i.Address != null).ToList().Select(i => i.Address.ToString()).Aggregate((i, j) => i + "," + j));
                Debug.WriteLine("=======================================================================");
            }

        }

        public static void DisplayGatewayAddresses()
        {
            Debug.WriteLine("Gateways");
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties adapterProperties = adapter.GetIPProperties();
                GatewayIPAddressInformationCollection addresses = adapterProperties.GatewayAddresses;
                if (addresses.Count > 0)
                {
                    Debug.WriteLine(adapter.Description);
                    foreach (GatewayIPAddressInformation address in addresses)
                    {
                        Debug.WriteLine("  Gateway Address ......................... : {0}",
                            address.Address.ToString());
                    }
                    Debug.WriteLine("=======================================================================");
                }
            }
        }
    }
}
