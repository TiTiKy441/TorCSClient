using Microsoft.Win32;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using TorCSClient.Proxy;

namespace TorCSClient.GUI
{
    partial class IconUserInterface
    {
        private System.ComponentModel.IContainer components = null;


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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            SuspendLayout();
            // 
            // IconUserInterface
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(148, 0);
            Enabled = false;
            Name = "IconUserInterface";
            Text = "TorCSClient";
            ResumeLayout(false);
        }

        #endregion
    }
}