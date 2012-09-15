using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace WDMsgServer
{
    public partial class DBInfoForm : Form
    {
        public DBInfoForm()
        {
            InitializeComponent();
        }

        private void cbx_modify_CheckStateChanged(object sender, EventArgs e)
        {
            if (cbx_modify.CheckState == CheckState.Checked)
            {
                grb_account.Enabled = true;
                grb_connect.Enabled = true;
            }
            else
            {
                grb_account.Enabled = false;
                grb_connect.Enabled = false;
            }
        }
    }
}