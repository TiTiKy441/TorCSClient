using System.DirectoryServices.ActiveDirectory;
using System.Linq.Expressions;
using TorCSClient.Listener;
using TorCSClient.Proxy;

namespace TorCSClient.GUI
{
    internal partial class IconUserInterface : Form
    {

        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip contextMenu;

        private readonly ToolStripButton exit_button;
        private readonly ToolStripMenuItem use_proxy_button;
        private readonly ToolStripButton connect_button;
        private readonly ToolStripButton disconnect_button;
        private readonly ToolStripTextBox log_textbox;
        private readonly ToolStripButton tor_control_button;
        private readonly ToolStripButton settings_button;

        public IconUserInterface()
        {
            Name = "TorCSClient";
            Text = "TorCSClient";

            TorService.Instance.OnStartupStatusChange += TorService_OnStartupStatusChange;
            TorService.Instance.OnStatusChange += TorService_OnStatusChange;

            InitializeComponent();

            contextMenu = new ContextMenuStrip()
            {
                Items =
                {
                    new ToolStripButton()
                    {
                        Name = "exit_button",
                        Text = "Exit",
                        Checked = false,
                    },
                    new ToolStripMenuItem()
                    {
                        Name = "use_proxy_button",
                        Text = "Enable",
                        Checked = false,
                        CheckOnClick = true,
                        Enabled = false,
                    },
                    new ToolStripButton()
                    {
                        Name = "connect_button",
                        Text = "Connect",
                    },
                    new ToolStripButton()
                    {
                        Name = "disconnect_button",
                        Text = "Disconnect",
                        Enabled = false,
                    },
                    new ToolStripTextBox()
                    {
                        Name = "log_textbox",
                        AcceptsTab = false,
                        Enabled = false,
                        Text = "NoProcess",
                    },
                    new ToolStripButton()
                    {
                        Name = "tor_control_button",
                        Enabled = true,
                        Text = "Tor control",
                    },
                    new ToolStripButton()
                    {
                        Name = "settings_button",
                        Enabled = true,
                        Text = "Settings",
                    },
                }
            };

            notifyIcon = new NotifyIcon()
            {
                ContextMenuStrip = contextMenu,
                Icon = new Icon("images/icon_disconnected.ico"),
                Text = "TorCSClient",
                Visible = true,
            };


            exit_button = (ToolStripButton)contextMenu.Items.Find("exit_button", false).First();
            use_proxy_button = (ToolStripMenuItem)contextMenu.Items.Find("use_proxy_button", false).First();
            connect_button = (ToolStripButton)contextMenu.Items.Find("connect_button", false).First();
            disconnect_button = (ToolStripButton)contextMenu.Items.Find("disconnect_button", false).First();
            log_textbox = (ToolStripTextBox)contextMenu.Items.Find("log_textbox", false).First();
            tor_control_button = (ToolStripButton)contextMenu.Items.Find("tor_control_button", false).First();
            settings_button = (ToolStripButton)contextMenu.Items.Find("settings_button", false).First();

            exit_button.Click += ExitButton_Click;
            use_proxy_button.Click += UseProxyButton_Click;
            connect_button.Click += ConnectButton_Click;
            disconnect_button.Click += DisconnectButton_Click;
            tor_control_button.Click += OpenTorControlButton_Click;
            settings_button.Click += OpenSettings_button_Click;
            Size = new Size(0, 0);
            Visible = false;
            ShowInTaskbar = false;
            //Hide();
            WindowState = FormWindowState.Minimized;
            FormClosing += IconUserInterface_FormClosing;
        }

        private void OpenSettings_button_Click(object? sender, EventArgs e)
        {
            Settings.Instance.Invoke(() =>
            {
                if (Settings.Instance.WindowState == FormWindowState.Minimized)
                {
                    Settings.Instance.WindowState = FormWindowState.Normal;
                    Settings.Instance.Visible = true;
                    Settings.Instance.ShowInTaskbar = true;
                }
                else
                {
                    Settings.Instance.Visible = false;
                    //TorControl.Instance.ShowInTaskbar = false; - Reloads the form and makes it rerender every time

                    Settings.Instance.WindowState = FormWindowState.Minimized;
                }
            }
            );
        }

        private void OpenTorControlButton_Click(object? sender, EventArgs e)
        {
            TorControl.Instance.Invoke(() =>
            {
                if (TorControl.Instance.WindowState == FormWindowState.Minimized)
                {
                    TorControl.Instance.WindowState = FormWindowState.Normal;
                    TorControl.Instance.Visible = true;
                    TorControl.Instance.ShowInTaskbar = true;
                }
                else
                {
                    TorControl.Instance.Visible = false;
                    //TorControl.Instance.ShowInTaskbar = false; - Reloads the form and makes it rerender every time

                    TorControl.Instance.WindowState = FormWindowState.Minimized;
                }
            });
        }

        private void TorService_OnStatusChange(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                switch (TorService.Instance.Status)
                {
                    case ProxyStatus.Running:
                        notifyIcon.Icon = new Icon("images/icon_connected.ico");
                        use_proxy_button.Enabled = true;
                        if (Configuration.Instance.Get("StartEnabled").First() == "1")
                        {
                            use_proxy_button.Checked = MainListener.IsEnabled;
                            if (!MainListener.IsEnabled)
                            {
                                MessageBox.Show("Unable to enable tor");
                            }
                        }
                        disconnect_button.Enabled = true;
                        connect_button.Enabled = false;
                        break;

                    case ProxyStatus.Starting:
                        notifyIcon.Icon = new Icon("images/icon_connecting.ico");
                        use_proxy_button.Enabled = false;
                        use_proxy_button.Checked = false;
                        disconnect_button.Enabled = true;
                        connect_button.Enabled = false;
                        break;

                    case ProxyStatus.Disabled:
                        notifyIcon.Icon = new Icon("images/icon_disconnected.ico");
                        use_proxy_button.Enabled = false;
                        use_proxy_button.Checked = false;
                        disconnect_button.Enabled = false;
                        connect_button.Enabled = true;
                        log_textbox.Text = "NoProcess";
                        break;
                }
            });
        }

        private void TorService_OnStartupStatusChange(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                log_textbox.Text = TorService.Instance.StartupStatus;
            });
        }

        private void IconUserInterface_FormClosing(object? sender, FormClosingEventArgs e)
        {
            TorService.Instance.OnStartupStatusChange -= TorService_OnStartupStatusChange;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
        }

        private void DisconnectButton_Click(object? sender, EventArgs e)
        {
            if (!TorService.Instance.ProxyRunning) return;

            use_proxy_button.Checked = false;
            TorControl.Instance.Invoke(() =>
            {
                TorControl.Instance.disconnect_button.AccessibilityObject.DoDefaultAction();
            });
        }

        private void ConnectButton_Click(object? sender, EventArgs e)
        {
            use_proxy_button.Checked = false;
            TorControl.Instance.Invoke(() =>
            {
                TorControl.Instance.connect_button.AccessibilityObject.DoDefaultAction();
            });
        }

        private void UseProxyButton_Click(object? sender, EventArgs e)
        {
            if (TorService.Instance.Status != ProxyStatus.Running)
            {
                use_proxy_button.Checked = false;
                return;
            }
            if (!MainListener.EnableTor(use_proxy_button.Checked))
            {
                use_proxy_button.Checked = false;
                MainListener.EnableTor(false);
                MessageBox.Show("Error occured while enabling");
            }
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            use_proxy_button.Checked = false;
            TorService.Instance.OnStartupStatusChange -= TorService_OnStartupStatusChange;
            TorService.Instance.OnStatusChange -= TorService_OnStatusChange;
            TorControl.Instance.UnhookFromTorService();
            TorService.Instance.StopTor(true); // Forced shutdown
            TorService.Instance.WaitForEnd();

            Application.Exit();
            Environment.Exit(0); // Dont touch...
        }
    }
}