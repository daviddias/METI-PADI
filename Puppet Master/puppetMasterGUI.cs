using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Puppet_Master
{
    public partial class puppetMasterGUI : Form
    {
        public puppetMasterGUI()
        {
            InitializeComponent();
        }

        private void openScriptFile_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.OpenFileDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToOpen = FD.FileName;
                Console.WriteLine(fileToOpen);

                //System.IO.FileInfo File = new System.IO.FileInfo(FD.FileName);

                //OR

                //System.IO.StreamReader reader = new System.IO.StreamReader(fileToOpen);
                //etc
            }
        }
    }
}
