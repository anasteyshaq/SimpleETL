using ETL.BusinessLogic;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ETL.PresentationLayer
{
    public partial class Form1 : Form
    {
        private ETLProcess etl = new ETLProcess(ConfigurationManager.AppSettings["path"]);
        public Form1()
        {
            InitializeComponent();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            
            else MessageBox.Show("Configuration file is not available");
        }
    }
}
