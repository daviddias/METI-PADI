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

        /**
         * Opens a Script File and puts it's steps onto Script Text Box
         */ 
        private void openScriptFile_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.OpenFileDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string fileToOpen = FD.FileName;
                String[] allLines = File.ReadAllLines(fileToOpen);
                scriptTextBox.Lines = allLines;                
            }
        }

        private void runNextStep_Click(object sender, EventArgs e)
        {
            executeNextStep();
        }






        /****************************************************************************************
         *                                  Logic functions
         ****************************************************************************************/

        private void executeNextStep()
        {
            //Read Next Line from Input
            //Parse it
            //Process it
            String[] lines = scriptTextBox.Lines;
            try { if (lines[0] == "") { return; } }
            catch (IndexOutOfRangeException e) {  return;  }
            String nextSept = lines[0];
            lines = lines.Where((val, idx) => idx != 0).ToArray();
            scriptTextBox.Lines = lines;
            currentStep.Text = nextSept;

            char[] p = { ' ', '\t' };
            string[] parsed = nextSept.Split(p);

            switch (parsed[0])
            {
                case "START": start(Convert.ToInt32(parsed[1]), Convert.ToInt32(parsed[2])); break;
                case "FAIL": fail(parsed[1]); break;
                case "RECOVER": recover(parsed[1]); break;
                case "FREEZE": freeze(parsed[1]); break;
                case "UNFREEZE": unfreeze(parsed[1]); break;
                case "CREATE": create(parsed[1], parsed[2], Convert.ToInt32(parsed[3]), Convert.ToInt32(parsed[4]), Convert.ToInt32(parsed[5])); break;
                case "OPEN": open(parsed[1], parsed[2]); break;
                case "CLOSE": close(parsed[1], parsed[2]); break;
                case "READ": read(parsed[1], Convert.ToInt32(parsed[2]), parsed[3], Convert.ToInt32(parsed[4])); break;
                case "WRITE": write(parsed[1], Convert.ToInt32(parsed[2]), Convert.ToInt32(parsed[3])); break;
                case "DUMP": dump(parsed[1]); break;
            }
        }


        private void start(int nbClients, int nbDataServers)
        {
            //Start DataServers
            //Start Meta-Data Servers (3)
            //Start Clients


            string currentDirectory = Environment.CurrentDirectory;
            string path = currentDirectory.Replace("Puppet Master", 

            
            /*
        private void startServer(int n, int port)
        {
            runningServers.Add(n, new Process());
            string currentDirectory = Environment.CurrentDirectory;
            string path = currentDirectory.Replace("PuppetMaster", "Server");
            path += "/Server.exe";
            runningServers[n].StartInfo.Arguments = port.ToString() ;
            runningServers[n].StartInfo.FileName = path;
            runningServers[n].Start();
            numberOfServers++;
        }
*/

        }

        private void fail(string process)
        {

        }

        private void recover(string process)
        {

        }

        private void freeze(string process)
        {

        }

        private void unfreeze(string process)
        {
 
        }


        private void create(string process, string filename, int nbDataServers, int readQuorum, int writeQuorum)
        {

        }

        private void open(string process,string filename)
        {

        }

        private void close(string process, string filename)
        {

        }

        private void read(string process, int fileRegister, string semantics, int byteArrayRegister)
        {
        }

        private void write(string process, int fileRegister, int byteArrayRegister)
        {
        }

        private void dump(string process)
        {
        }


    }
}
