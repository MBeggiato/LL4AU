using System;
using System.Windows;
using Busylight;
using System.Windows.Interop;
using RestSharp;
using System.DirectoryServices.AccountManagement;
using Newtonsoft.Json;
using System.Web;
using Forms = System.Windows.Forms;
using static LL4AU.Util;
using System.IO;

namespace LL4AU {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow : Window {
        private SDK light;
        private BusylightColor lastColor;
        private RestClient rest;
        private bool useWebexStatus = true;
        private bool partyMode = false;

        private Settings settings;
        public MainWindow() {
            _notifyIcon = new Forms.NotifyIcon();


            #region Settings
            if (File.Exists("settings.json")) {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json")) ?? new Settings();
            }
            else {
                settings = new Settings();
                File.WriteAllText("settings.json", JsonConvert.SerializeObject(settings));
            }
            #endregion




            initTray();
            InitializeComponent();

            light = new SDK();
            lastColor = SdColors.Free;
            ComponentDispatcher.ThreadFilterMessage += ComponentDispatcher_ThreadFilterMessage;


            rest = new RestClient(new RestClientOptions("https://webexapis.com/v1"));
            rest.AddDefaultHeader("Authorization", settings.Token);


            System.Windows.Threading.DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += new EventHandler(Timer_Elapsed);
            timer.Start();
        }

        private void Timer_Elapsed(object sender, EventArgs e) {
            if (useWebexStatus == true) {
                string status = GetUserStatus();
                lblWebexStatus.Content = status;
                if (status == "call") {
                    ChangeLightColor(SdColors.Call);
                }
                else if (status == "DoNotDisturb") {
                    ChangeLightColor(SdColors.DoNotDisturb);
                }
                else if (status == "unknown") {
                    ChangeLightColor(SdColors.DoNotDisturb);
                }
                else if (status == "active") {
                    ChangeLightColor(SdColors.Free);
                }
                else if (status == "meeting") {
                    ChangeLightColor(SdColors.Meeting);
                }
                else {
                    ChangeLightColor(lastColor);
                }
            }

        }

        private string GetUserStatus() {
            var request = new RestRequest($"people?email={HttpUtility.UrlEncode(UserPrincipal.Current.EmailAddress)}");
            dynamic response = JsonConvert.DeserializeObject(rest.Get(request).Content ?? "{}") ?? "{}";
            return Convert.ToString(response.items[0].status);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            ChangeLightColor(SdColors.DoNotDisturb);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            ChangeLightColor(SdColors.Free);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            ChangeLightColor(SdColors.Call);
        }

        public void ChangeLightColor(BusylightColor busylightColor) {
            light.Light(busylightColor);
            lastColor = busylightColor;
        }
        private void Button_Click_3(object sender, RoutedEventArgs e) {
            light.Blink(BusylightColor.Red, 2, 2);
            useWebexStatus = false;
            chkBxWebexStatus.IsChecked = false;
        }
        private void chkBxWebexStatus_Checked(object sender, RoutedEventArgs e) {
            useWebexStatus = true;
        }

        private void chkBxWebexStatus_Unchecked(object sender, RoutedEventArgs e) {
            useWebexStatus = false;
        }

        #region Windows detect lock

        private const int NOTIFY_FOR_THIS_SESSION = 0;
        private const int WM_WTSSESSION_CHANGE = 0x2b1;
        private const int WTS_SESSION_LOCK = 0x7;
        private const int WTS_SESSION_UNLOCK = 0x8;

        public event EventHandler MachineLocked;
        public event EventHandler MachineUnlocked;

        void ComponentDispatcher_ThreadFilterMessage(ref MSG msg, ref bool handled) {
            if (msg.message == WM_WTSSESSION_CHANGE) {
                int value = msg.wParam.ToInt32();
                if (value == WTS_SESSION_LOCK) {
                    OnMachineLocked(EventArgs.Empty);
                }
                else if (value == WTS_SESSION_UNLOCK) {
                    OnMachineUnlocked(EventArgs.Empty);
                }
            }
        }

        protected virtual void OnMachineLocked(EventArgs e) {
            EventHandler temp = MachineLocked;
            if (temp != null) {
                temp(this, e);
            }
            light.Light(BusylightColor.Yellow);
        }

        protected virtual void OnMachineUnlocked(EventArgs e) {
            EventHandler temp = MachineUnlocked;
            if (temp != null) {
                temp(this, e);
            }
            light.Light(lastColor);
        }
        #endregion

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            e.Cancel = true;
            Hide();
        }


        #region Tray Icon

        private readonly Forms.NotifyIcon _notifyIcon;

        private void initTray() {
            _notifyIcon.Icon = new System.Drawing.Icon("Resources/traffic-lights.ico");
            _notifyIcon.Text = "LL4AU";
            _notifyIcon.DoubleClick += _notifyIcon_Click;
            _notifyIcon.ContextMenuStrip = new Forms.ContextMenuStrip();
            
            _notifyIcon.ContextMenuStrip.Items.Add("Beschäftigt", null, BeschaeftigtClicked);
            _notifyIcon.ContextMenuStrip.Items.Add("Frei", null, FreiClicked);
            _notifyIcon.ContextMenuStrip.Items.Add("Maximieren", null, MaxClicked);
            _notifyIcon.ContextMenuStrip.Items.Add("Beenden", null, BeendenClicked);
            _notifyIcon.Visible = true;
        }

        private void MaxClicked(object? sender, EventArgs e) {
            WindowState = WindowState.Normal;
            Activate();
            Show();
        }

        private void FreiClicked(object? sender, EventArgs e) {
            ChangeLightColor(SdColors.Free);
            useWebexStatus = false;
            chkBxWebexStatus.IsChecked = false;
        }

        private void BeschaeftigtClicked(object? sender, EventArgs e) {
            ChangeLightColor(SdColors.DoNotDisturb);
            useWebexStatus = false;
            chkBxWebexStatus.IsChecked = false;
        }
    

        private void BeendenClicked(object? sender, EventArgs e) {
            Application.Current.Shutdown();

        }

        private void _notifyIcon_Click(object? sender, EventArgs e) {
            WindowState = WindowState.Normal;
            Activate();
            Show();
        }
        #endregion

        private void window_Closed(object sender, EventArgs e) {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            
        }
    }

}
