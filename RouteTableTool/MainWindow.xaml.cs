using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows;
using CodeCowboy.NetworkRoute;

namespace RouteTableTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<string> ips = new List<string>();
        private string processname = "";
        private bool loop = false;
        private BackgroundWorker ipsworker;

        private ObservableCollection<string> IPSList = new ObservableCollection<string>();
        private ObservableCollection<interfacelist> InterfaceList = new ObservableCollection<interfacelist>();
        private ObservableCollection<string> GateWayList = new ObservableCollection<string>();

        private JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
        private List<string> recent = new List<string>();
        private List<string> save = new List<string>();
        public MainWindow()
        {
            InitializeComponent();
            ReadSettings();
            IPList.ItemsSource = IPSList;

            ipsworker = new BackgroundWorker
            {
                WorkerSupportsCancellation = true
            };
            
            
            ProcessName.Text = processname;
            GetNetworkInterface();
            Interfaces.ItemsSource = InterfaceList;
            Interfaces.DisplayMemberPath = "UI";
            Interfaces.SelectedValuePath = "INDEX";
            GateWays.ItemsSource = GateWayList;
        }

        private void GetNetworkInterface()
        {
            InterfaceList.Clear();
            GateWayList.Clear();
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface adapter in nics)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();
                InterfaceList.Add(new interfacelist { UI = adapter.Description, INDEX = properties.GetIPv4Properties().Index });
                GatewayIPAddressInformationCollection gateways = properties.GatewayAddresses;
                if (gateways.Count > 0)
                {
                    foreach (GatewayIPAddressInformation address in gateways)
                    {
                        GateWayList.Add(address.Address.ToString());
                    }
                }
            }
        }

        private void WriteSettings()
        {
            string settingPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
            Dictionary<string, object> settings = new Dictionary<string, object>
            {
                {
                    "settings",
                    new Dictionary<string, object>
                    {
                        { "process", processname }
                    }
                },
                {
                    "routes",
                    new Dictionary<string, object>
                    {
                        { "recent", recent },
                        { "save", save }
                    }
                }
            };
            File.WriteAllText(settingPath, javaScriptSerializer.Serialize(settings));
        }
        private void ReadSettings()
        {
            string settingPath = AppDomain.CurrentDomain.BaseDirectory + "settings.json";
            if (!File.Exists(settingPath))
            {
                WriteSettings();
            }
            else
            {
                string settingString = File.ReadAllText(settingPath);
                dynamic settings = javaScriptSerializer.Deserialize<dynamic>(settingString);
                try
                {
                    processname = settings["settings"]["process"];
                    foreach (string s in settings["routes"]["recent"])
                    {
                        recent.Add(s);
                    }
                    foreach (string s in settings["routes"]["save"])
                    {
                        save.Add(s);
                    }
                }
                catch
                { }
            }
        }

        private void DoWork(object sender, DoWorkEventArgs e)
        {
            while (loop)
            {
                List<string> tasklist = new List<string>();
                List<string> pids = new List<string>();
                List<string> netstat = new List<string>();
                try
                {
                    using (Process ProcPid = new Process())
                    {
                        ProcPid.StartInfo.UseShellExecute = false;
                        ProcPid.StartInfo.FileName = "tasklist.exe";
                        ProcPid.StartInfo.Arguments = string.Format("/fi \"imagename eq {0}\" /nh /fo csv", processname);
                        ProcPid.StartInfo.CreateNoWindow = true;
                        ProcPid.StartInfo.RedirectStandardOutput = true;
                        ProcPid.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ProcPid.Start();
                        StreamReader reader = ProcPid.StandardOutput;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            tasklist.Add(line);
                        }
                        reader.Close();
                        ProcPid.WaitForExit();
                        ProcPid.Close();
                    }
                }
                finally
                {
                    foreach (string sline in tasklist)
                    {
                        if (sline != null && sline.StartsWith("\""))
                        {
                            pids.Add(sline.Trim().Split(',')[1].TrimStart('"').TrimEnd('"'));
                        }
                    }
                    Utils.Purge(ref pids);
                }


                try
                {
                    using (Process ProcIP = new Process())
                    {
                        ProcIP.StartInfo.UseShellExecute = false;
                        ProcIP.StartInfo.FileName = "netstat.exe";
                        ProcIP.StartInfo.Arguments = "-ano";
                        ProcIP.StartInfo.CreateNoWindow = true;
                        ProcIP.StartInfo.RedirectStandardOutput = true;
                        ProcIP.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                        ProcIP.Start();
                        StreamReader reader = ProcIP.StandardOutput;
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            netstat.Add(line);
                        }
                        reader.Close();
                        ProcIP.WaitForExit();
                        ProcIP.Close();
                    }
                }
                finally
                {
                    netstat.RemoveAt(0);
                    foreach (string sline in netstat)
                    {
                        foreach (string pid in pids)
                        {
                            if (sline != null && sline.EndsWith(pid))
                            {
                                string line = sline.Trim().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)[2];
                                ips.Add(line.Substring(0, line.IndexOf(':')));
                            }
                        }
                    }
                    ips.RemoveAll(x => x.StartsWith("[") || x.StartsWith("*") || x.StartsWith("0.0.0.0") || x.StartsWith("127.0.0.1"));
                    Utils.Purge(ref ips);
                    Dispatcher.Invoke(() => {
                        IPSList.Clear();
                        foreach (string ip in ips)
                        {
                            IPSList.Add(ip);
                        }
                    });
                    GC.SuppressFinalize(this);
                }
                Thread.Sleep(2000);
            }
        }

        private void ActionStart_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ProcessName.Text) && !ipsworker.IsBusy)
            {
                processname = ProcessName.Text.Trim();
                loop = true;
                ipsworker.DoWork += new DoWorkEventHandler(DoWork);
                ipsworker.RunWorkerAsync();
            }
            ActionStart.IsEnabled = false;
        }

        private void ActionStop_Click(object sender, RoutedEventArgs e)
        {
            loop = false;
            ipsworker.CancelAsync();
            ipsworker.DoWork -= DoWork;
            ActionStart.IsEnabled = true;
            Utils.FlushMemory();
        }

        private void ActionRefresh_Click(object sender, RoutedEventArgs e)
        {
            GateWays.SelectedIndex = -1;
            Interfaces.SelectedIndex = -1;
            GetNetworkInterface();
        }

        private void CreateRoute_Click(object sender, RoutedEventArgs e)
        {
            if(GateWays.SelectedIndex < 0 || Interfaces.SelectedIndex < 0 || ips.Count < 1) return;
            foreach (string ip in ips)
            {
                Ip4RouteEntry routeEntry = new Ip4RouteEntry
                {
                    DestinationIP = IPAddress.Parse(ip),
                    SubnetMask = IPAddress.Parse("255.255.255.255"),
                    GatewayIP = IPAddress.Parse(GateWays.SelectedValue as string),
                    InterfaceIndex = Convert.ToInt32(Interfaces.SelectedValue)
                };
                if (!Ip4RouteTable.RouteExists(ip))
                {
                    Ip4RouteTable.CreateRoute(routeEntry);
                }
            }
            processname = ProcessName.Text.Trim();
            recent = new List<string>(ips);
            WriteSettings();

        }
        private void DeleteRoute_Click(object sender, RoutedEventArgs e)
        {
            foreach (string ip in recent)
            {
                Ip4RouteTable.DeleteRoute(ip);
            }
        }

        private void ActionClear_Click(object sender, RoutedEventArgs e)
        {
            ips.Clear();
            Dispatcher.Invoke(() => {
                IPSList.Clear();
            });
        }

        private void ActionSave_Click(object sender, RoutedEventArgs e)
        {
            save = new List<string>(ips);
            WriteSettings();
        }

        private void ActionImport_Click(object sender, RoutedEventArgs e)
        {
            ips = ips.Union(save).ToList();
            Dispatcher.Invoke(() => {
                IPSList.Clear();
                foreach (string ip in ips)
                {
                    IPSList.Add(ip);
                }
            });
        }
    }
}
