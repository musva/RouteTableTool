using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Net;
using System.Collections.Generic;

namespace CodeCowboy.NetworkRoute
{
    public class Ip4RouteTable
    {

        public static void RoutePrint(bool testing)
        {
            var fwdTable = IntPtr.Zero;
            int size = 0;
            var result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);
            fwdTable = Marshal.AllocHGlobal(size);

            result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);

            var forwardTable = NativeMethods.ReadIPForwardTable(fwdTable);

            Marshal.FreeHGlobal(fwdTable);

            Console.Write("\tNumber of entries: {0}\n", forwardTable.Size);

            for (int i = 0; i < forwardTable.Table.Length; ++i)
            {
                Debug.WriteLine("\n\tRoute[{0}] Dest IP: {1}\n", i, new IPAddress((long)forwardTable.Table[i].dwForwardDest).ToString());
                Debug.WriteLine("\tRoute[{0}] Subnet Mask: {1}\n", i, new IPAddress((long)forwardTable.Table[i].dwForwardMask).ToString());
                Debug.WriteLine("\tRoute[{0}] Next Hop: {1}\n", i, new IPAddress((long)forwardTable.Table[i].dwForwardNextHop).ToString());
                Debug.WriteLine("\tRoute[{0}] If Index: {1}\n", i, forwardTable.Table[i].dwForwardIfIndex);
                Debug.WriteLine("\tRoute[{0}] Type: {1}\n", i, forwardTable.Table[i].dwForwardType);
                Debug.WriteLine("\tRoute[{0}] Proto: {1}\n", i, forwardTable.Table[i].dwForwardProto);
                Debug.WriteLine("\tRoute[{0}] Age: {1}\n", i, forwardTable.Table[i].dwForwardAge);
                Debug.WriteLine("\tRoute[{0}] Metric1: {1}\n", i, forwardTable.Table[i].dwForwardMetric1);
            }

        }

        public static void RoutePrint()
        {
            List<Ip4RouteEntry> routeTable = GetRouteTable();
            RoutePrint(routeTable);
        }


        public static void RoutePrint(List<Ip4RouteEntry> routeTable)
        {
            Debug.WriteLine("Route Count: {0}", routeTable.Count);
            Debug.WriteLine("{0,18} {1,18} {2,18} {3,5} {4,8} ", "DestinationIP", "NetMask", "Gateway", "IF", "Metric");
            foreach (Ip4RouteEntry entry in routeTable)
            {
                Console.WriteLine("{0,18} {1,18} {2,18} {3,5} {4,8} ", entry.DestinationIP, entry.SubnetMask, entry.GatewayIP, entry.InterfaceIndex, entry.Metric);
            }
        }


        public static List<Ip4RouteEntry> GetRouteTable()
        {
            var fwdTable = IntPtr.Zero;
            int size = 0;
            var result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);
            fwdTable = Marshal.AllocHGlobal(size);

            result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);

            var forwardTable = NativeMethods.ReadIPForwardTable(fwdTable);

            Marshal.FreeHGlobal(fwdTable);


            List<Ip4RouteEntry> routeTable = new List<Ip4RouteEntry>();
            for (int i = 0; i < forwardTable.Table.Length; ++i)
            {
                Ip4RouteEntry entry = new Ip4RouteEntry();
                entry.DestinationIP = new IPAddress((long)forwardTable.Table[i].dwForwardDest);
                entry.SubnetMask = new IPAddress((long)forwardTable.Table[i].dwForwardMask);
                entry.GatewayIP = new IPAddress((long)forwardTable.Table[i].dwForwardNextHop);
                entry.InterfaceIndex = Convert.ToInt32(forwardTable.Table[i].dwForwardIfIndex);
                entry.ForwardType = Convert.ToInt32(forwardTable.Table[i].dwForwardType);
                entry.ForwardProtocol = Convert.ToInt32(forwardTable.Table[i].dwForwardProto);
                entry.ForwardAge = Convert.ToInt32(forwardTable.Table[i].dwForwardAge);
                entry.Metric = Convert.ToInt32(forwardTable.Table[i].dwForwardMetric1);
                routeTable.Add(entry);
            }
            return routeTable;
        }


        public static bool InterfaceIndexExists(int interfaceIndex)
        {
            List<Ip4RouteEntry> routeTable = Ip4RouteTable.GetRouteTable();
            Ip4RouteEntry routeEntry = routeTable.Find(i => i.InterfaceIndex.Equals(interfaceIndex));
            return (routeEntry != null);
        }


        public static bool RouteExists(string destinationIP)
        {
            List<Ip4RouteEntry> routeTable = Ip4RouteTable.GetRouteTable();
            Ip4RouteEntry routeEntry = routeTable.Find(i => i.DestinationIP.ToString().Equals(destinationIP));
            return (routeEntry != null);
        }


        public static List<Ip4RouteEntry> GetRouteEntry(string destinationIP)
        {
            List<Ip4RouteEntry> routeTable = Ip4RouteTable.GetRouteTable();
            List<Ip4RouteEntry> routeMatches = routeTable.FindAll(i => i.DestinationIP.ToString().Equals(destinationIP));
            return routeMatches;
        }


        public static List<Ip4RouteEntry> GetRouteEntry(string destinationIP, string mask)
        {
            List<Ip4RouteEntry> routeTable = Ip4RouteTable.GetRouteTable();
            List<Ip4RouteEntry> routeMatches = routeTable.FindAll(i => i.DestinationIP.ToString().Equals(destinationIP) && i.SubnetMask.ToString().Equals(mask));
            return routeMatches;
        }


        public static void CreateRoute(Ip4RouteEntry routeEntry)
        {

            var route = new NativeMethods.MIB_IPFORWARDROW
            {
                dwForwardDest = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.DestinationIP.ToString()).GetAddressBytes(), 0),
                dwForwardMask = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.SubnetMask.ToString()).GetAddressBytes(), 0),
                dwForwardNextHop = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.GatewayIP.ToString()).GetAddressBytes(), 0),
                dwForwardMetric1 = 99,
                dwForwardType = Convert.ToUInt32(3), //Default to 3
                dwForwardProto = Convert.ToUInt32(3), //Default to 3
                dwForwardAge = 0,
                dwForwardIfIndex = Convert.ToUInt32(routeEntry.InterfaceIndex)
            };

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.MIB_IPFORWARDROW)));
            try
            {
                Marshal.StructureToPtr(route, ptr, false);
                var status = NativeMethods.CreateIpForwardEntry(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

        }

        public static void CreateRoute(string destination, string mask, int interfaceIndex, int metric)
        {
            NetworkAdaptor adaptor = NicInterface.GetNetworkAdaptor(interfaceIndex);
            var route = new NativeMethods.MIB_IPFORWARDROW
            {
                dwForwardDest = BitConverter.ToUInt32(IPAddress.Parse(destination).GetAddressBytes(), 0),
                dwForwardMask = BitConverter.ToUInt32(IPAddress.Parse(mask).GetAddressBytes(), 0),
                dwForwardNextHop = BitConverter.ToUInt32(IPAddress.Parse(adaptor.PrimaryGateway.ToString()).GetAddressBytes(), 0),
                dwForwardMetric1 = Convert.ToUInt32(metric),
                dwForwardType = Convert.ToUInt32(3), //Default to 3
                dwForwardProto = Convert.ToUInt32(3), //Default to 3
                dwForwardAge = 0,
                dwForwardIfIndex = Convert.ToUInt32(interfaceIndex)
            };

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.MIB_IPFORWARDROW)));
            try
            {
                Marshal.StructureToPtr(route, ptr, false);
                var status = NativeMethods.CreateIpForwardEntry(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

        }

        public static void DeleteRoute(Ip4RouteEntry routeEntry)
        {

            var route = new NativeMethods.MIB_IPFORWARDROW
            {
                dwForwardDest = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.DestinationIP.ToString()).GetAddressBytes(), 0),
                dwForwardMask = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.SubnetMask.ToString()).GetAddressBytes(), 0),
                dwForwardNextHop = BitConverter.ToUInt32(IPAddress.Parse(routeEntry.GatewayIP.ToString()).GetAddressBytes(), 0),
                dwForwardMetric1 = 99,
                dwForwardType = Convert.ToUInt32(3), //Default to 3
                dwForwardProto = Convert.ToUInt32(3), //Default to 3
                dwForwardAge = 0,
                dwForwardIfIndex = Convert.ToUInt32(routeEntry.InterfaceIndex)
            };

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.MIB_IPFORWARDROW)));
            try
            {
                Marshal.StructureToPtr(route, ptr, false);
                var status = NativeMethods.DeleteIpForwardEntry(ptr);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

        }

        public static void DeleteRoute(string destinationIP)
        {

            List<Ip4RouteEntry> routeMatches = Ip4RouteTable.GetRouteEntry(destinationIP);
            if (routeMatches == null) return;

            foreach (Ip4RouteEntry routeEntry in routeMatches)
            {
                DeleteRoute(routeEntry);
            }
        }

        public static void DeleteRoute(string destinationIP, string mask)
        {

            List<Ip4RouteEntry> routeMatches = Ip4RouteTable.GetRouteEntry(destinationIP, mask);
            if (routeMatches == null) return;

            foreach (Ip4RouteEntry routeEntry in routeMatches)
            {
                DeleteRoute(routeEntry);
            }
        }


        public static void DeleteRoute(int interfaceIndex)
        {

            var fwdTable = IntPtr.Zero;
            int size = 0;
            var result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);
            fwdTable = Marshal.AllocHGlobal(size);

            result = NativeMethods.GetIpForwardTable(fwdTable, ref size, true);

            var forwardTable = NativeMethods.ReadIPForwardTable(fwdTable);

            Marshal.FreeHGlobal(fwdTable);


            uint uIndex = Convert.ToUInt32(interfaceIndex);
            List<NativeMethods.MIB_IPFORWARDROW> filtered = new List<NativeMethods.MIB_IPFORWARDROW>();
            for (int i = 0; i < forwardTable.Table.Length; ++i)
            {
                if (forwardTable.Table[i].dwForwardIfIndex.Equals(uIndex))
                {
                    filtered.Add(forwardTable.Table[i]);
                }
            }

            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.MIB_IPFORWARDROW)));
            try
            {
                foreach (NativeMethods.MIB_IPFORWARDROW routeEntry in filtered)
                {
                    Marshal.StructureToPtr(routeEntry, ptr, false);
                    var status = NativeMethods.DeleteIpForwardEntry(ptr);
                }
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

        }

    }

    public class Ip4RouteEntry
    {
        public IPAddress DestinationIP { get; set; }
        public IPAddress SubnetMask { get; set; }
        public IPAddress GatewayIP { get; set; }
        public int InterfaceIndex { get; set; }
        public int ForwardType { get; set; }
        public int ForwardProtocol { get; set; }
        public int ForwardAge { get; set; }
        public int Metric { get; set; }
    }

    internal static class NativeMethods
    {
        //https://docs.microsoft.com/en-us/windows/win32/iphlp/managing-routing
        //https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-rrasm/5dca234b-bea4-4e67-958e-5459a32a7b71


        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        public struct IPForwardTable
        {
            public uint Size;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public MIB_IPFORWARDROW[] Table;
        };

        [ComVisible(false), StructLayout(LayoutKind.Sequential)]
        public struct MIB_IPFORWARDROW
        {
            internal uint /*DWORD*/ dwForwardDest;
            internal uint /*DWORD*/ dwForwardMask;
            internal uint /*DWORD*/ dwForwardPolicy;
            internal uint /*DWORD*/ dwForwardNextHop;
            internal uint /*DWORD*/ dwForwardIfIndex;
            internal uint /*DWORD*/ dwForwardType;
            internal uint /*DWORD*/ dwForwardProto;
            internal uint /*DWORD*/ dwForwardAge;
            internal uint /*DWORD*/ dwForwardNextHopAS;
            internal uint /*DWORD*/ dwForwardMetric1;
            internal uint /*DWORD*/ dwForwardMetric2;
            internal uint /*DWORD*/ dwForwardMetric3;
            internal uint /*DWORD*/ dwForwardMetric4;
            internal uint /*DWORD*/ dwForwardMetric5;
        };

        public enum MIB_IPFORWARD_TYPE : uint
        {
            MIB_IPROUTE_TYPE_OTHER = 1,
            MIB_IPROUTE_TYPE_INVALID = 2,
            MIB_IPROUTE_TYPE_DIRECT = 3,
            MIB_IPROUTE_TYPE_INDIRECT = 4
        }



        public enum MIB_IPFORWARD_PROTO : uint
        {
            MIB_IPPROTO_OTHER = 1,
            MIB_IPPROTO_LOCAL = 2,
            MIB_IPPROTO_NETMGMT = 3,
            MIB_IPPROTO_ICMP = 4,
            MIB_IPPROTO_EGP = 5,
            MIB_IPPROTO_GGP = 6,
            MIB_IPPROTO_HELLO = 7,
            MIB_IPPROTO_RIP = 8,
            MIB_IPPROTO_IS_IS = 9,
            MIB_IPPROTO_ES_IS = 10,
            MIB_IPPROTO_CISCO = 11,
            MIB_IPPROTO_BBN = 12,
            MIB_IPPROTO_OSPF = 13,
            MIB_IPPROTO_BGP = 14,
            MIB_IPPROTO_NT_AUTOSTATIC = 10002,
            MIB_IPPROTO_NT_STATIC = 10006,
            MIB_IPPROTO_NT_STATIC_NON_DOD = 10007
        }


        public static IPForwardTable ReadIPForwardTable(IntPtr tablePtr)
        {
            var result = (IPForwardTable)Marshal.PtrToStructure(tablePtr, typeof(IPForwardTable));

            MIB_IPFORWARDROW[] table = new MIB_IPFORWARDROW[result.Size];
            IntPtr p = new IntPtr(tablePtr.ToInt64() + Marshal.SizeOf(result.Size));
            for (int i = 0; i < result.Size; ++i)
            {
                table[i] = (MIB_IPFORWARDROW)Marshal.PtrToStructure(p, typeof(MIB_IPFORWARDROW));
                p = new IntPtr(p.ToInt64() + Marshal.SizeOf(typeof(MIB_IPFORWARDROW)));
            }
            result.Table = table;

            return result;
        }


        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int GetIpForwardTable(IntPtr /*PMIB_IPFORWARDTABLE*/ pIpForwardTable, ref int /*PULONG*/ pdwSize, bool bOrder);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        //public extern static int CreateIpForwardEntry(ref /*PMIB_IPFORWARDROW*/ Ip4RouteTable.PMIB_IPFORWARDROW pRoute);  Can do by reference or by Pointer
        public extern static int CreateIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int DeleteIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

        [DllImport("iphlpapi", CharSet = CharSet.Auto)]
        public extern static int SetIpForwardEntry(IntPtr /*PMIB_IPFORWARDROW*/ pRoute);

    }
}