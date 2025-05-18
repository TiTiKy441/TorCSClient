namespace TorCSClient.GUI
{
    partial class Settings
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Settings));
            use_dns_checkbox = new CheckBox();
            filter_type_combobox = new ComboBox();
            proxifyre_remove_button = new Button();
            proxifyre_add_button = new Button();
            filter_type_label = new Label();
            proxifyre_apps = new ListView();
            icon = new ColumnHeader();
            executable = new ColumnHeader();
            path = new ColumnHeader();
            SuspendLayout();
            // 
            // use_dns_checkbox
            // 
            use_dns_checkbox.AutoSize = true;
            use_dns_checkbox.Location = new Point(263, 12);
            use_dns_checkbox.Name = "use_dns_checkbox";
            use_dns_checkbox.RightToLeft = RightToLeft.Yes;
            use_dns_checkbox.Size = new Size(112, 24);
            use_dns_checkbox.TabIndex = 0;
            use_dns_checkbox.Text = "?Use tor dns";
            use_dns_checkbox.UseVisualStyleBackColor = true;
            use_dns_checkbox.CheckStateChanged += use_dns_checkbox_CheckStateChanged;
            // 
            // filter_type_combobox
            // 
            filter_type_combobox.DropDownStyle = ComboBoxStyle.DropDownList;
            filter_type_combobox.FormattingEnabled = true;
            filter_type_combobox.Items.AddRange(new object[] { "Everything (System proxy)", "Selected apps (ProxiFyre)" });
            filter_type_combobox.Location = new Point(12, 39);
            filter_type_combobox.Name = "filter_type_combobox";
            filter_type_combobox.Size = new Size(363, 28);
            filter_type_combobox.TabIndex = 1;
            filter_type_combobox.SelectedIndexChanged += filter_type_combobox_SelectedIndexChanged;
            // 
            // proxifyre_remove_button
            // 
            proxifyre_remove_button.Location = new Point(281, 418);
            proxifyre_remove_button.Name = "proxifyre_remove_button";
            proxifyre_remove_button.Size = new Size(94, 29);
            proxifyre_remove_button.TabIndex = 4;
            proxifyre_remove_button.Text = "Remove";
            proxifyre_remove_button.UseVisualStyleBackColor = true;
            proxifyre_remove_button.Click += proxifyre_remove_button_Click;
            // 
            // proxifyre_add_button
            // 
            proxifyre_add_button.Location = new Point(12, 418);
            proxifyre_add_button.Name = "proxifyre_add_button";
            proxifyre_add_button.Size = new Size(94, 29);
            proxifyre_add_button.TabIndex = 3;
            proxifyre_add_button.Text = "Add";
            proxifyre_add_button.UseVisualStyleBackColor = true;
            proxifyre_add_button.Click += proxifyre_add_button_Click;
            // 
            // filter_type_label
            // 
            filter_type_label.AutoSize = true;
            filter_type_label.Location = new Point(12, 16);
            filter_type_label.Name = "filter_type_label";
            filter_type_label.Size = new Size(59, 20);
            filter_type_label.TabIndex = 6;
            filter_type_label.Text = "Use for:";
            // 
            // proxifyre_apps
            // 
            proxifyre_apps.Alignment = ListViewAlignment.Left;
            proxifyre_apps.CheckBoxes = true;
            proxifyre_apps.Columns.AddRange(new ColumnHeader[] { icon, executable, path });
            proxifyre_apps.FullRowSelect = true;
            proxifyre_apps.LabelWrap = false;
            proxifyre_apps.Location = new Point(12, 73);
            proxifyre_apps.Name = "proxifyre_apps";
            proxifyre_apps.Size = new Size(363, 339);
            proxifyre_apps.Sorting = SortOrder.Ascending;
            proxifyre_apps.TabIndex = 7;
            proxifyre_apps.UseCompatibleStateImageBehavior = false;
            proxifyre_apps.View = View.Details;
            proxifyre_apps.ItemChecked += proxifyre_apps_ItemChecked;
            // 
            // icon
            // 
            icon.Text = "icon";
            icon.Width = 40;
            // 
            // executable
            // 
            executable.Text = "executable";
            executable.Width = 84;
            // 
            // path
            // 
            path.Text = "path";
            path.Width = 235;
            // 
            // Settings
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(387, 459);
            Controls.Add(proxifyre_apps);
            Controls.Add(filter_type_label);
            Controls.Add(proxifyre_remove_button);
            Controls.Add(proxifyre_add_button);
            Controls.Add(filter_type_combobox);
            Controls.Add(use_dns_checkbox);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "Settings";
            ShowInTaskbar = false;
            Text = "Settings";
            FormClosing += Settings_FormClosing;
            FormClosed += Settings_FormClosed;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private CheckBox use_dns_checkbox;
        private ComboBox filter_type_combobox;
        private Button proxifyre_remove_button;
        private Button proxifyre_add_button;
        private Label filter_type_label;
        private ListView proxifyre_apps;
        private ColumnHeader icon;
        private ColumnHeader executable;
        private ColumnHeader path;
    }
}