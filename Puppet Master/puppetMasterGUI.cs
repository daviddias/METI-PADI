using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Collections;

namespace Puppet_Master
{
    public partial class puppetMasterGUI : Form
    {
        /****************************************************************************************
         *                                  Attributes
         ****************************************************************************************/
        
        int firstMetaServerPort = 8000;
        int firstClientPort = 8100;
        int firstDataServerPort = 9000;

        int nbDataServers;
        int nbClients;

        String[] listOfDataServerPorts;
        String[] listOfMetaServerPorts;
        String[] listOfClientPorts;

        // Dictonary of Running Processes Key=processID (e.g. c-1) Value=Process
        public Dictionary<string, Process> runningProcesses = new Dictionary<string, Process>();

        TcpChannel channel;
        

        /****************************************************************************************
        *                                  GUI functions
        ****************************************************************************************/


        public puppetMasterGUI()
        {
            InitializeComponent();
            /* Initialize TCP Channel */
            channel = new TcpChannel();
            ChannelServices.RegisterChannel(channel, false);
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
            catch (IndexOutOfRangeException) {  return;  }
            String nextSept = lines[0];
            lines = lines.Where((val, idx) => idx != 0).ToArray();
            scriptTextBox.Lines = lines;
            currentStep.Text = nextSept;

            String[] p = {" ", "\t" ,", "};
            string[] parsed = nextSept.Split(p, StringSplitOptions.None);

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
                case "READ": read(parsed[1],  Convert.ToInt32(parsed[2]), parsed[3], Convert.ToInt32(parsed[4])); break;
                case "WRITE":
                    if (parsed[3].StartsWith("\""))
                    {
                        for (int i = 4; i < parsed.Length; i++)
                            parsed[3] += " " + parsed[i];
                        write(parsed[1], Convert.ToInt32(parsed[2]), parsed[3]);
                    }
                    else
                    {
                        write(parsed[1], Convert.ToInt32(parsed[2]), Convert.ToInt32(parsed[3]));
                    }
                    break;
                case "DUMP": dump(parsed[1]); break;
                case "HELLO": hello(parsed[1]); break;
            }
        }


        private void start(int nbClients, int nbDataServers)
        {
            //Start DataServers
            //Start Meta-Data Servers (3)
            //Start Clients
            String dataServerPath = Environment.CurrentDirectory.Replace("Puppet Master", "Data-Server");
            dataServerPath += "/Data-Server.exe";
            String metaServerPath = Environment.CurrentDirectory.Replace("Puppet Master", "Meta-Data Server");
            metaServerPath += "/Meta-Data Server.exe";
            String clientPath = Environment.CurrentDirectory.Replace("Puppet Master", "Client");
            clientPath += "/Client.exe";

            //outputBox.Text = dataServerPath;    

            String listOfDataServerPorts = "";
            String listOfMetaServerPorts = "";
            String listOfClientPorts = "";

            //Data-Servers - Args <PortLocal>
            for (int i = 0; i < nbDataServers; i++)
            {
                runningProcesses.Add("d-" + i , new Process());
                runningProcesses["d-" + i].StartInfo.Arguments = (firstDataServerPort + i).ToString();
                listOfDataServerPorts += (firstDataServerPort + i).ToString() + " ";
                runningProcesses["d-" + i].StartInfo.FileName = dataServerPath;
                runningProcesses["d-" + i].Start();
                Console.WriteLine("Data-Server Started");
                System.Threading.Thread.Sleep(1000);
            }

           
            

            //Meta-Data Servers - Args <MetaDataPortLocal> <MetaDataPortOtherA> <MetaDataPortOtherB> [DataServerPort] [DataServer Port] [DataServer Port]...
            String meta0 = (firstMetaServerPort + 0).ToString() + " " + (firstMetaServerPort + 1).ToString() + " " + (firstMetaServerPort + 2).ToString();
            runningProcesses.Add("m-" + 0, new Process());
            runningProcesses["m-" + 0].StartInfo.Arguments = meta0 + " " +  listOfDataServerPorts;
            runningProcesses["m-" + 0].StartInfo.FileName = metaServerPath;
            runningProcesses["m-" + 0].Start();

            Console.WriteLine("Meta-Server 0 Started");
            System.Threading.Thread.Sleep(1000);

            String meta1 = (firstMetaServerPort + 1).ToString() + " " + (firstMetaServerPort + 0).ToString() + " " + (firstMetaServerPort + 2).ToString();
            runningProcesses.Add("m-" + 1, new Process());
            runningProcesses["m-" + 1].StartInfo.Arguments = meta1 + " " + listOfDataServerPorts;
            runningProcesses["m-" + 1].StartInfo.FileName = metaServerPath;
            runningProcesses["m-" + 1].Start();

            Console.WriteLine("Meta-Server 1 Started");
            System.Threading.Thread.Sleep(1000);

            String meta2 = (firstMetaServerPort + 2).ToString() + " " + (firstMetaServerPort + 0).ToString() + " " + (firstMetaServerPort + 1).ToString();
            runningProcesses.Add("m-" + 2, new Process());
            runningProcesses["m-" + 2].StartInfo.Arguments = meta2 + " " + listOfDataServerPorts;
            runningProcesses["m-" + 2].StartInfo.FileName = metaServerPath;
            runningProcesses["m-" + 2].Start();

            Console.WriteLine("Meta-Server 2 Started");
            System.Threading.Thread.Sleep(1000);


            listOfMetaServerPorts = meta0; //Meta0 Contem a ordem certa de Meta-Servers que corresponde as responsabilidades para serem entregues aos clientes

            //Clients - Args <clientPort> <clientID> <meta0Port> <meta1Port> <meta2Port> 
            for (int k = 0; k < nbClients; k++)
            {
                runningProcesses.Add("c-" + k, new Process());
                runningProcesses["c-" + k].StartInfo.Arguments = (firstClientPort + k).ToString() + " " +  ("c-" + k + " ") + listOfMetaServerPorts;
                listOfClientPorts += (firstClientPort + k).ToString() + " ";
                runningProcesses["c-" + k].StartInfo.FileName = clientPath;
                runningProcesses["c-" + k].Start();
            }

            this.nbClients = nbClients;
            this.nbDataServers = nbDataServers;
            
            this.listOfDataServerPorts = listOfDataServerPorts.Split(' ');
            this.listOfMetaServerPorts = listOfMetaServerPorts.Split(' ');
            this.listOfClientPorts = listOfClientPorts.Split(' ');
        }



        private void fail(string process)
        {
            remoteClientInterface rci;
            MyRemoteMetaDataInterface mdi;
            MyRemoteDataInterface dsi;
            // Clients
            if(process.StartsWith("c-"))
                rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Metadata Servers
            if(process.StartsWith("m-"))
                mdi = Utils.getRemoteMetaDataObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Data Servers
            if(process.StartsWith("d-"))
                dsi = Utils.getRemoteDataServerObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);

        }

        private void recover(string process)
        {
            remoteClientInterface rci;
            MyRemoteMetaDataInterface mdi;
            MyRemoteDataInterface dsi;
            // Clients
            if (process.StartsWith("c-"))
                rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Metadata Servers
            if (process.StartsWith("m-"))
                mdi = Utils.getRemoteMetaDataObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Data Servers
            if (process.StartsWith("d-"))
                dsi = Utils.getRemoteDataServerObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
        }

        private void freeze(string process)
        {
            remoteClientInterface rci;
            MyRemoteMetaDataInterface mdi;
            MyRemoteDataInterface dsi;
            // Clients
            if (process.StartsWith("c-"))
                rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Metadata Servers
            if (process.StartsWith("m-"))
                mdi = Utils.getRemoteMetaDataObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Data Servers
            if (process.StartsWith("d-"))
                dsi = Utils.getRemoteDataServerObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
        }

        private void unfreeze(string process)
        {
            remoteClientInterface rci;
            MyRemoteMetaDataInterface mdi;
            MyRemoteDataInterface dsi;
            // Clients
            if (process.StartsWith("c-"))
                rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Metadata Servers
            if (process.StartsWith("m-"))
                mdi = Utils.getRemoteMetaDataObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            // Data Servers
            if (process.StartsWith("d-"))
                dsi = Utils.getRemoteDataServerObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
        }


        private void create(string process, string filename, int nbDataServers, int readQuorum, int writeQuorum)
        {
            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            rci.create(filename, nbDataServers, readQuorum, writeQuorum);       
        }

        private void open(string process,string filename)
        {
            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            rci.open(filename);
        }

        private void close(string process, string filename)
        {
            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            rci.close(filename); 
        }

        private void read(string process, int reg, string semantics, int byteArrayRegister)
        {
            int DEFAULT = 1;
            int MONOTONIC = 2;

            int semantic;

            switch (semantics) {
                case "default": semantic = DEFAULT; break;
                case "monotonic": semantic = MONOTONIC; break;
                default: semantic = DEFAULT; break;
            }

            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            rci.read(reg, semantic, byteArrayRegister);
        }

        private void write(string process, int reg, string content)
        {
            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(content);
            rci.write(reg, bytes); 
        }

        private void write(string process, int reg, int byteArray)
        {
            remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
            rci.write(reg, byteArray); 
        }

        private void dump(string process)
        {
        }

        /*Communication Testing Method*/
        private void hello(string process)
        {
            outputBox.Text = process;

            System.Threading.Thread.Sleep(2000);
            
            if(process[0] == 'c')
            {
                remoteClientInterface rci = Utils.getRemoteClientObj(listOfClientPorts[(int)Char.GetNumericValue(process[2])]);
                string result = rci.metodoOla();
                outputBox.Text = result;
            }
            if (process[0] == 'm')
            {
            }
            if (process[0] == 'd')
            {
            }
        }


        

    }
}
