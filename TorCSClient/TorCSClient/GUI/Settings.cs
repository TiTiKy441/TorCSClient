using System.Data;
using TorCSClient.Listener;
using TorCSClient.Network.ProxiFyre;

namespace TorCSClient.GUI
{
    public partial class Settings : Form
    {

        public static Settings Instance { get; private set; }

        private static readonly Size IconSize = new(30, 30);

        private readonly Bitmap WhiteImage;

        public Settings()
        {
            Instance = this;
            InitializeComponent();
            WindowState = FormWindowState.Minimized;
            MaximizeBox = false;
            //ControlBox = false;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MinimizeBox = true;

            // This is a placeholder image for cases when we werent been able to get the icon of the exe in AddAppToProxiFyreList();
            WhiteImage = new(IconSize.Width, IconSize.Height);
            using Graphics gr = Graphics.FromImage(WhiteImage);
            gr.Clear(Color.White);

            proxifyre_apps.SmallImageList = new ImageList();

            filter_type_combobox.SelectedIndex = Configuration.Instance.GetInt("NetworkFilterType");
            use_dns_checkbox.Checked = Configuration.Instance.GetFlag("UseTorDNS");
            proxifyre_apps.BeginUpdate();
            foreach (string exe in ProxiFyreService.Instance.GetApps())
            {
                AddAppToProxiFyreList(exe);
            }
            proxifyre_apps.EndUpdate();
            ProxiFyreService.Instance.OnStartRequested += ProxiFyre_OnStartRequested;
        }

        private void Settings_FormClosed(object? sender, FormClosedEventArgs e)
        {
            UpdateProxiFyreList(false, false);
            ProxiFyreService.Instance.OnStartRequested -= ProxiFyre_OnStartRequested;
        }

        private void ProxiFyre_OnStartRequested(object? sender, EventArgs e)
        {
            Invoke(() =>
            {
                UpdateProxiFyreList(true, false);
            });
        }

        private void Settings_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (IconUserInterface.Instance.ExitRequestedFlag) return;
            e.Cancel = true;
            Instance.Visible = false;
            Instance.WindowState = FormWindowState.Minimized;
        }

        private void proxifyre_add_button_Click(object sender, EventArgs e)
        {
            string[] filePaths = Array.Empty<string>();

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "exe files (*.exe)|*.exe";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;
                openFileDialog.Multiselect = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    filePaths = openFileDialog.FileNames;

                    var fileStream = openFileDialog.OpenFile();
                }
            }
            proxifyre_apps.BeginUpdate();
            filePaths.ToList().ForEach(file => AddAppToProxiFyreList(file));
            proxifyre_apps.EndUpdate();
            UpdateProxiFyreList();
        }

        private void proxifyre_remove_button_Click(object sender, EventArgs e)
        {
            if (proxifyre_apps.Items.Count == 0) return;
            if (proxifyre_apps.SelectedIndices.Count == 0) return;
            proxifyre_apps.SelectedIndices.Cast<int>().ToList().ForEach(i => proxifyre_apps.Items.RemoveAt(i));
            UpdateProxiFyreList();
        }

        private void ReloadTor()
        {
            if (!MainListener.IsEnabled) return;
            MainListener.EnableTor(false);
            MainListener.EnableTor(true);
        }

        private void AddAppToProxiFyreList(string pathToApp)
        {
            Bitmap? iconOrig = Icon.ExtractAssociatedIcon(pathToApp)?.ToBitmap();
            Bitmap iconComplete = iconOrig == null ? WhiteImage : new Bitmap(iconOrig, IconSize);
            proxifyre_apps.SmallImageList?.Images.Add(iconComplete);
            int? iconIndex = proxifyre_apps.SmallImageList?.Images.Count - 1;
            string file = Path.GetFileName(pathToApp);
            ListViewItem listViewItem = new(new string[] { "", file, pathToApp });
            if (iconIndex != null) listViewItem.ImageIndex = (int)iconIndex;
            listViewItem.Checked = true;
            proxifyre_apps.Items.Add(listViewItem);
        }

        public void UpdateProxiFyreList(bool check = true, bool reloadTor = true)
        {
            string[] oldApps = ProxiFyreService.Instance.GetApps();
            string[] newApps = proxifyre_apps.Items.Cast<ListViewItem>().Where(x => (x != null && (!check || x.Checked))).Select(x => x.SubItems[2].Text).ToArray();
            if (!oldApps.SequenceEqual(newApps))
            {
                ProxiFyreService.Instance.SetApps(newApps);
                proxifyre_apps.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
                if (reloadTor) ReloadTor();
            }
        }

        private void proxifyre_apps_ItemChecked(object sender, ItemCheckedEventArgs e)
        {
            UpdateProxiFyreList();
        }

        private void use_dns_checkbox_CheckStateChanged(object sender, EventArgs e)
        {
            if (Configuration.Instance.GetInt("NetworkFilterType") == 0)
            {
                use_dns_checkbox.Checked = true;
            }
            if (!Utils.IsAdministrator())
            {
                use_dns_checkbox.Checked = false;
            }
            Configuration.Instance.SetFlag("UseTorDNS", use_dns_checkbox.Checked);
            ReloadTor();
        }

        private void filter_type_combobox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (filter_type_combobox.SelectedIndex == 1)
            {
                Configuration.Instance.SetFlag("UseTorAsSystemProxy", false);
            }
            if (filter_type_combobox.SelectedIndex == 0)
            {
                use_dns_checkbox.Checked = true;
                Configuration.Instance.SetFlag("UseTorAsSystemProxy", true);
                Configuration.Instance.SetFlag("UseTorDNS", true);
            }
            Configuration.Instance.SetInt("NetworkFilterType", filter_type_combobox.SelectedIndex);
            ReloadTor();
        }
    }
}
