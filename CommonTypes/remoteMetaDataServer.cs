﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using log4net;
using System.Collections;
using System.Runtime.Serialization.Formatters;




public interface MyRemoteMetaDataInterface{

    string MetodoOla();

    //usados pelos client
    FileHandler open(string clientID, string filename);
    void close(string ClientID, FileHandler filehandler);
    FileHandler create(string clientID, string filename, int nbServers, int readQuorum, int writeQuorum);
    void confirmCreate(string clientID, string filename, Boolean created);
    FileHandler delete(string clientID, string filename);
    void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted);
    FileHandler write(string clientID, FileHandler filehandler);
    void confirmWrite(string clientID, FileHandler filehander, Boolean wrote);
    void alive(); //usado pelo cliente para verificar que este meta-data está vivo
    
    void receiveAlive(string port); //usado pelo Data-Server para dizer que está vivo
    

    //usado pelo Puppet-Master
    void fail();
    void recover();
    void dump();
    void loadBalancing();


    //usado por outros Meta-Servers
    Boolean lockFile(string filename);
    Boolean unlockFile(string filename);
    void receiveUpdate(Dictionary<string, FileHandler>[] fileTable, Dictionary<string, DataServerInfo>[] newDataServersMap);
    void sendUpdate();


}





public class MyRemoteMetaDataObject : MarshalByRefObject, MyRemoteMetaDataInterface{

    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /* Atributes */
    static string localPort;
    static string aMetaServerPort;
    static string bMetaServerPort;
    static int whoAmI; //0, 1 ou 2 to identify which Meta-Server it is 
    static List<string> dataServersPorts = new List<string>();
    static Boolean recovering = false;
    

    // Dict used for LoadBalancing and File Allocation
    public static Dictionary<string, DataServerInfo> dataServersMap = new Dictionary<string, DataServerInfo>();
    
    
    
    static Boolean isfailed;

    //Array of fileTables containing file Handlers
    public static Dictionary<string, FileHandler>[] fileTables = new Dictionary<string, FileHandler>[6];
    
    /* Constructors */
    public MyRemoteMetaDataObject(){
        isfailed = false;

        for (int i = 0; i < 6; i++)
            fileTables[i] = new Dictionary<string, FileHandler>();

        string path = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%\\PADI-FS\\") + System.Diagnostics.Process.GetCurrentProcess().ProcessName + "-" + whoAmI;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Directory.SetCurrentDirectory(path);
        log.Info("Meta-Data Server is up!");
    }

    public MyRemoteMetaDataObject(string _localPort, string _aMetaServerPort, string _bMetaServerPort, string[] _dataServersPorts){

        isfailed = false;
        localPort = _localPort;
        aMetaServerPort = _aMetaServerPort;
        bMetaServerPort = _bMetaServerPort;

        for (int i = 0; i < _dataServersPorts.Length; i++)
        {
            //dataServersPorts.Add(_dataServersPorts[i]);
            // adicionar os dataServers ao dataServerMap 
            DataServerInfo dsinfo = new DataServerInfo();
            dsinfo.MachineHeat = 0;
            dsinfo.dataServer = _dataServersPorts[i];
            dataServersMap.Add(_dataServersPorts[i], dsinfo);
        }

        for (int i = 0; i < 6; i++)
            fileTables[i] = new Dictionary<string, FileHandler>();

        if (Convert.ToInt32(localPort) < Convert.ToInt32(aMetaServerPort)
            && Convert.ToInt32(localPort) < Convert.ToInt32(bMetaServerPort))
        { whoAmI = 0; }
        else
        {
            if (Convert.ToInt32(localPort) > Convert.ToInt32(aMetaServerPort)
                && Convert.ToInt32(localPort) > Convert.ToInt32(bMetaServerPort))
            { whoAmI = 2; }

            else { whoAmI = 1; }
        }
        string path = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%\\PADI-FS\\") + System.Diagnostics.Process.GetCurrentProcess().ProcessName + "-" + whoAmI;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        Directory.SetCurrentDirectory(path);

        log.Info("Meta Server " + whoAmI + " is up!");
    }


    /* Para a thread nunca se desligar */
    public override object InitializeLifetimeService(){ return null; }

    /* delegates */
    public delegate void prepareUpdateRemoteAsyncDelegate(Dictionary<string, FileHandler>[] newFileTable, Dictionary<string, DataServerInfo>[] newDataServersMap);
    public delegate void askUpdateRemoteAsyncDelegate();


    /* Logic */
    public string MetodoOla(){ return "[META_SERVER]   Ola eu sou o MetaData Server!"; }

    public void alive() { log.Info("Yep, I'm alive =)"); }

    /************************************************************************
     *              Invoked Methods by Clients
     ************************************************************************/
    public FileHandler open(string clientID, string Filename)
    {
        /* 1. Is MetaServer Able to Respond  (Fail)
         * 2. Does the file Exist yet?
         * 3. Add to the File Handle, the clientID who has it opened
         * 4. Tells other Meta-Data Servers to update
         * 5. Returns FileHandler
         */

        FileHandler fh;
  
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: open]    The server has is on 'fail'!");
            return null;
        }


        //2.Does the file exist?
        if (!fileTables[Utils.whichMetaServer(Filename)].ContainsKey(Filename))
        {
            log.Info("[METASERVER: open]    The file doesn't exist yet (error)!");
            return null; //TODO return exception here! 
        }

        fh = fileTables[Utils.whichMetaServer(Filename)][Filename];

        //3. Add to the File Handle, the clientID who has it opened
        if (!fh.isOpen)
            fh.isOpen = true;
        fh.byWhom.Add(clientID);

        //4. Increment access count to this file
        fh.nFileAccess++;

        //5. Tells the other MetaServers to update
        sendUpdate();

        log.Info("[METASERVER: open]    Success)!");
        return fh;
    }

    public void close(string ClientID, FileHandler filehandler){
        /* 1. Is MetaServer Able to Respond  (Fail)
         * 2. Has this client a lock in this file? (If yes, denied close)
         * 3. Updates the respective File-Handle by removing this user from the byWhom list
         * 4. Tells other meta-data
         */

        //String Filename = filehandler.fileName;

        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: close]    The server has is on 'fail'!");
            return;
        }


        //2. Has this client a lock in this file? (If yes, denied close)
        if (filehandler.isLocked)
        {
            log.Info("[METASERVER: close]    The File is locked!");
            return;
        }


        //3. Updates the respective File-Handle by removing this user from the byWhom list
        filehandler.byWhom.Remove(ClientID);

        //4. Increment access count to this file
        filehandler.nFileAccess++;

        //5. Tells the other MetaServers to update
        sendUpdate();

        log.Info("[METASERVER: close]    Success)!");
    }



    public FileHandler create(string clientID, string filename, int nbServers, int readQuorum, int writeQuorum)
    {
        FileHandler fh;
        //1. Is MetaServer Able to Respond (Fail)
        //if (isfailed)
        //{
        //    log.Info("[METASERVER: create]    The server has is on 'fail'!");
        //    return null;
        //}

        //2. Does the file already exists? 
        if (fileTables[Utils.whichMetaServer(filename)].ContainsKey(filename))
        {
            log.Info("[METASERVER: create]    File already exists");
            return null; //TODO return exception here! 
        }

        //3. Decide where the fill will be hosted
        //3.1 There are enought Data Servers in the system to meet the required replication?
        if (nbServers > dataServersMap.Count)
            //dataServersPorts.Count)
        {
            log.Info("[METASERVER: create]    There aren't enought Data Servers to meet the required replication");
            fh = new FileHandler(filename, 0, 0, new string[0], new string[0], readQuorum, writeQuorum, 1); // if there aren't enough data servers meta server sends always nbServer = 0 
            return fh;
        }
        
        //3.2 Select the first enougths DataServers to store the data - DUMB WAY
        string[] selectedDataServers = new string[nbServers];
        List<DataServerInfo> listOfDataServersAvailable = dataServersMap.Values.ToList();   
        for (int i = 0; i < nbServers; i++)
        {
            //selectedDataServers[i] = dataServersPorts[i];
            // %% TODO - Use info from Load Balacing(dataServerMap) to decide which will go
            selectedDataServers[i] = listOfDataServersAvailable[i].dataServer;
        }


        
        // 3.3 Generate localfilenames
        string[] localNames = new string[nbServers];
        for (int i = 0; i < nbServers; i++)
            localNames[i] = Utils.genLocalName("m-" + whoAmI);



        //4. Create File-Handler 
        //Console.WriteLine("Creating new File Handle");
        fh = new FileHandler(filename, 0, nbServers, selectedDataServers, localNames, readQuorum, writeQuorum, 1);
        //Console.WriteLine("Created new File Handle");
        
        //4.1 (new for load balancing) update DataServerInfo of dataServerMap
        foreach (String dsp in selectedDataServers)
        {
            dataServersMap[dsp].fileHandlers.Add(fh);
        }

        //5. Save the File-Handler
        fileTables[Utils.whichMetaServer(filename)].Add(filename, fh);

        //6. Lock File accross Meta-Data Servers
        //TODO

        //7. Return File-Handler
        log.Info("[METASERVER: create]    Success!");
        
        return fh;
    }

    public void confirmCreate(string clientID, string filename, Boolean created) 
    {
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: confirmCreate]    The server has is on 'fail'!");
            return;
        }

        //2. Faz unlock ao ficheiro
        fileTables[Utils.whichMetaServer(filename)][filename].isLocked = false;
        log.Info("[METASERVER: confirmCreate]    Success!");

        // 3. Send Updated metadata table to others Metadata Servers
        sendUpdate();
    }



    public FileHandler delete(string clientID, string filename)
    {

        //1. Is the metaserver able to respond?
        if (isfailed)
        {
            log.Info("[METASERVER: delete]    The server has is on 'fail'!");
            return null;
        }

        //2. Does the file exist?
        if (!fileTables[Utils.whichMetaServer(filename)].ContainsKey(filename))
        {
            log.Info("[METASERVER: delete]    File does not exist!");
            return null; //TODO return exception here! 
        }

        //3. Get filehandler
        FileHandler fh = fileTables[Utils.whichMetaServer(filename)][filename];

        //4. O ficheiro está bloqueado?
        if (fh.isLocked)
        {
            log.Info("[METASERVER: delete]    The File is locked!");
            return null;
        }

        //5.Faz lock ao ficheiro
        fh.isLocked = true;

        //6. Increment access count to this file
        fh.nFileAccess++;

        //7. Return the filehandler
        log.Info("[METASERVER: delete]    Success!");
        return fh;
    }

    public void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted) 
    {
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: confirmDelete]    The server has is on 'fail'!");
            return;
        }

        //2. Apaga metadata associada ao ficheiro
        if (deleted == true)
        {
            fileTables[Utils.whichMetaServer(filehandler.filenameGlobal)].Remove(filehandler.filenameGlobal);
            log.Info("[METASERVER: confirmDelete]    Success!");
            // 3. Send Updated metadata table to others Metadata Servers
            sendUpdate();
            return;
        }

        fileTables[Utils.whichMetaServer(filehandler.filenameGlobal)][filehandler.filenameGlobal].isLocked = false;
        log.Info("[METASERVER: confirmDelete]    File was not deleted!");
    }



    public FileHandler write(string clientID, FileHandler filehandler)
    {

        //Boolean flag = false; wut???

        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: write]    The server has is on 'fail'!");
            return null;
        }

        //2. Does the file already exists? 
        if (!fileTables[Utils.whichMetaServer(filehandler.filenameGlobal)].ContainsKey(filehandler.filenameGlobal))
        {
            log.Info("[METASERVER: write]    The file doesn't exist yet (error)!");
            return null; //TODO return exception here! 
        }

        //log.Info("[METASERVER: write]    File exists!");

        //3. O ficheiro está bloqueado?
        if (filehandler.isLocked){
            log.Info("[METASERVER: write]    The File is locked!");
            return null;
        }

        //4. O cliente tem o ficheiro aberto?
        // Esta verificacao ja é feita no lado do cliente!

        //Console.WriteLine("[METASERVER: write]    Client had opened the file!");


        //5.Faz lock ao ficheiro
        filehandler.isLocked = true;

        //6. Increment access count to this file
        filehandler.nFileAccess++;

        //8. Devolve o filehandler ao cliente
        log.Info("[METASERVER: write]    Success!");
        return filehandler;
    }
        
    public void confirmWrite(string clientID, FileHandler filehandler, Boolean wrote) 
    {
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: confirmWrite]    The server has is on 'fail'!");
            return;
        }

        //2. Faz unlock ao ficheiro
        filehandler.isLocked = false;
        log.Info("[METASERVER: confirmWrite]    Success!");

        //3. Updates file
        fileTables[Utils.whichMetaServer(filehandler.filenameGlobal)][filehandler.filenameGlobal].fileSize = filehandler.fileSize;
        fileTables[Utils.whichMetaServer(filehandler.filenameGlobal)][filehandler.filenameGlobal].version = filehandler.version;
        
        // 3. Send Updated metadata table to others Metadata Servers
        sendUpdate();
    }

    /************************************************************************
     *              Invoked Methods by Pupper-Master
     ************************************************************************/
    public void fail()
    {
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: confirmCreate]    The server has is on 'fail'!");
            return;
        }

        IChannel[] defaultTCPChannel = ChannelServices.RegisteredChannels;
        for (int channelCount = 0; channelCount < defaultTCPChannel.Length; channelCount++)
        {
            //Locate My registerd channel
            if (defaultTCPChannel[channelCount].ChannelName == localPort)
            {
                //Release(Unregister) the Channel assigned to this Instance
                ChannelServices.UnregisterChannel(defaultTCPChannel[channelCount]);
                break;
            }
        }

        log.Info("[METASERVER: fail]    Success!");
        return;
    }

    public void recover() {
        log.Info("Starting recovering procedure");

        if (isfailed)
        {
            log.Info("Registering again remote object for remote calls");
            BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
            provider.TypeFilterLevel = TypeFilterLevel.Full;
            IDictionary props = new Hashtable();
            //props["port"] = 8081;
            props["port"] = localPort;
            props["name"] = localPort;
            TcpChannel channel = new TcpChannel(props, null, provider);
            ChannelServices.RegisterChannel(channel, false);

        
            for (int i = 0; i < 6; i++)
            {
                StreamReader sw = new StreamReader("backup-m" + whoAmI + "_table-" + i);
                string s = sw.ReadLine();
                while(!s.Equals(""))
                {
                    char p = '|';
                    string[] parsed = s.Split(p);

                    string filenameGlobal = parsed[0];
                    long fileSize = long.Parse(parsed[1]);
                    int nbServers = int.Parse(parsed[2]);
                    string[] dataPorts = parsed[3].Split(':');
                    int readQuorum = int.Parse(parsed[4]);
                    int writeQuorum = int.Parse(parsed[5]);
                    long nFileAccess = long.Parse(parsed[6]);


                    string[] dataFileNames = parsed[7].Split(':');
                    string[] localNames = new string[nbServers];
                    for (int j = 0; j < nbServers; j++)
                        localNames[j] = dataFileNames[i].Split('>')[1];


                    FileHandler fh = new FileHandler(filenameGlobal, fileSize, nbServers, dataPorts, localNames, readQuorum, writeQuorum, nFileAccess);
                    fh.version =long.Parse(parsed[8]);
                    fh.isOpen = bool.Parse(parsed[9]);

                    fileTables[i].Add(filenameGlobal, fh);

                    s = sw.ReadLine();
                    }
                    sw.Close();
            }
        }
        log.Info("Going to Request UPDATE on recovering");
        askForUpdate();
    }

    public void dump()
    {
        if (isfailed == true)
        {
            log.Info("[METASERVER: dump]    The server is already on fail!");
            isfailed = true;
        }



        string s = "Metadata stored in this Metadata Server:\n\n";
        for (int i = 0; i < 6; i++)
        {
            if (i % 2 == 0)
            {
                if (i == whoAmI)
                    s += "### I'm the primary for these files ####\n";
                else
                    s += "### I'm not the primary for these files, but have them stored ###\n";
            }
            foreach (FileHandler fh in fileTables[i].Values)
                s += fh.ToString() + "\n";

            if (i % 2 != 0)
                s += "\t-----------------------------\n\n";
        }

        log.Info("[METASERVER: dump]    Success!");
        log.Info(s);

        System.Console.WriteLine("I know this DATA-SERVERS:");
        foreach (string port in dataServersMap.Keys)
        {
            System.Console.WriteLine(port);
        }

        return;
    }

    /************************************************************************
     *              Invoked Methods by other Meta-Data Servers
     ************************************************************************/
    public Boolean lockFile(string Filename) {

        if (fileTables[Utils.whichMetaServer(Filename)][Filename].isLocked)
            log.Info("[METASERVER: lockFile]    The File was already locked! (not normal)");

        fileTables[Utils.whichMetaServer(Filename)][Filename].isLocked = true;

        log.Info("[METASERVER: lockFile]    File Locked Successful!");
        return true; 
    }

    public Boolean unlockFile(string Filename) {
        if (!fileTables[Utils.whichMetaServer(Filename)][Filename].isLocked)
            log.Info("[METASERVER: lockFile]    The File was already locked! (not normal)");

        fileTables[Utils.whichMetaServer(Filename)][Filename].isLocked = false;

        log.Info("[METASERVER: lockFile]    File Locked Successful!");    
        return true; 
    }

    //public void updateHeatTable(List<HeatTableItem> table) { }

    public void sendUpdate()
    {
        MyRemoteMetaDataInterface[] mdi = new MyRemoteMetaDataInterface[2];
        mdi[0] = Utils.getRemoteMetaDataObj(aMetaServerPort);
        mdi[1] = Utils.getRemoteMetaDataObj(bMetaServerPort);

        Dictionary<string, DataServerInfo>[] dataServersMap_toSend = new Dictionary<string, DataServerInfo>[1];
        dataServersMap_toSend[0] = dataServersMap;

        for (int i = 0; i < 2; i++)
        {
            prepareUpdateRemoteAsyncDelegate RemoteUpdate = new prepareUpdateRemoteAsyncDelegate(mdi[i].receiveUpdate);
            IAsyncResult RemAr = RemoteUpdate.BeginInvoke(fileTables, dataServersMap_toSend, null, null);

            log.Info(" UPDATE SENDED::  Updated metadata table sended in background to others Metadata Servers");
        }

        // save the filetables to disk on a file

        for (int i = 0; i < 6; i++)
        {
            File.Create("backup-m" + whoAmI + "_table-" + i).Close();
            StreamWriter sw = new StreamWriter("backup-m" + whoAmI + "_table-" + i);
            string s = "";
            foreach (FileHandler fh in fileTables[i].Values)
            {
                s = (fh.filenameGlobal + "|" + fh.fileSize + "|" + fh.nbServers + "|");
                for (int j = 0; j < fh.nbServers; j++)
                {
                    s += fh.dataServersPorts[j];
                    if (j != fh.nbServers - 1)
                        s += ":";
                    else
                        s += "|";
                }
                s += (fh.readQuorum + "|" + fh.writeQuorum + "|" + fh.nFileAccess + "|");
                for (int j = 0; j < fh.nbServers; j++)
                {
                    s += fh.dataServersPorts[j] + ">" + fh.dataServersFiles[fh.dataServersPorts[j]];
                    if (j != fh.nbServers - 1)
                        s += ":";
                    else
                        s += "|";
                }
                s += fh.version + "|";
                s += fh.isOpen;
                sw.WriteLine(s);
            }

            /*s = "";
            for (int k = 0 ; k < dataServersPorts.Count - 1 ; k++)
            {
                s += dataServersPorts[k] + ":";
            }
            sw.WriteLine(s);*/
            sw.Close();
        }
    }

    public void receiveUpdate(Dictionary<string, FileHandler>[] newFileTable, Dictionary<string, DataServerInfo>[] newDataServersMap_received)
    {
        log.Info("receiveUpdated call received");

        for (int i = 0; i < 6; i++)
            if (recovering)
            {
                fileTables[i] = newFileTable[i];
            }
            else if (i != whoAmI * 2 || i != whoAmI * 2 + 1) // dont update the files that it is responsible
                fileTables[i] = newFileTable[i];

        recovering = false;

        Dictionary<string, DataServerInfo> newDataServersMap = newDataServersMap_received[0];

        foreach (string newPort in newDataServersMap.Keys) {
            if (!dataServersMap.ContainsKey(newPort)){
                dataServersPorts.Add(newPort);
            }
        }

        // save the filetables to disk on a file

        for (int i = 0; i < 6; i++)
        {
            File.Create("backup-m" + whoAmI + "_table-" + i).Close();
            StreamWriter sw = new StreamWriter("backup-m" + whoAmI + "_table-" + i);
            string s = "";
            foreach (FileHandler fh in fileTables[i].Values)
            {
                s = (fh.filenameGlobal + "|" + fh.fileSize + "|" + fh.nbServers + "|");
                for (int j = 0; j < fh.nbServers; j++)
                {
                    s += fh.dataServersPorts[j];
                    if (j != fh.nbServers - 1)
                        s += ":";
                    else
                        s += "|";
                }
                s += (fh.readQuorum + "|" + fh.writeQuorum + "|" + fh.nFileAccess + "|");
                for (int j = 0; j < fh.nbServers; j++)
                {
                    s += fh.dataServersPorts[j] + ">" + fh.dataServersFiles[fh.dataServersPorts[j]];
                    if (j != fh.nbServers - 1)
                        s += ":";
                    else
                        s += "|";
                }
                s += fh.version + "|";
                s += fh.isOpen;
                sw.WriteLine(s);
            }

            /*s = "";
            for (int k = 0; k < dataServersPorts.Count - 1; k++)
            {
                s += dataServersPorts[k] + ":";
            }
            sw.WriteLine(s);*/
            sw.Close();
        }

        log.Info(" UPDATE RECEIVED::  Updated metadata table sended in background to others Metadata Servers");
    }

    public void receiveAlive(string port)
    {
        //if (!dataServersPorts.Contains(port))
        //{
        //   dataServersPorts.Add(port);
        //}
        if (!dataServersMap.ContainsKey(port))
        {
            DataServerInfo dsinfo = new DataServerInfo();
            dsinfo.MachineHeat = 0;
            dsinfo.dataServer = port;
            dataServersMap.Add(port, dsinfo);
        }

    }

    public void askForUpdate()
    {
        MyRemoteMetaDataInterface[] mdi = new MyRemoteMetaDataInterface[2];
        mdi[0] = Utils.getRemoteMetaDataObj(aMetaServerPort);
        mdi[1] = Utils.getRemoteMetaDataObj(bMetaServerPort);

        recovering = true;

        for (int i = 0; i < 2; i++)
        {
            askUpdateRemoteAsyncDelegate RemoteUpdate = new askUpdateRemoteAsyncDelegate(mdi[i].sendUpdate);
            IAsyncResult RemAr = RemoteUpdate.BeginInvoke(null, null);

            log.Info(" REQUEST UPDATE SENDED::  Contacted other metaservers to ask for theis updates!");
        }


    }


    /************************************************************************************************
     * 
     *                                      LOAD BALANCING STUFF
     *
     ***********************************************************************************************/
    public void loadBalancing()
    {
        // Resume
        // 1. Calculate File-Heat and update File Handlers accordingly
        // 2. Put DataServerInfo objects in an array, Calculate Machine-Heat, sorted by ascending order
        // 3. Match High Half with Low Half (caution to check if it's even) use thermal Dissipation
        // 4. Calculate average heat, if goal is not reached, do cicle again ( to step 2 but before recalculate Machine-Heat again)
        // 5. Create struture <fileHandler, arrayOfnewDataServers>>
        // 6. Do the migrations
        // 7. Create dataServerMap again



        // 1 Calculate File-Heat and update File Handlers accordingly
        calculateFileHeat();
        //At this point, dataServerMap should have the DataServerInfo as well :)


        // 2. Put DataServerInfo objects in an array, Calculate Machine-Heat, sorted by ascending order
        

        // 3. Match High Half with Low Half (caution to check if it's even) use thermal Dissipation
        

        // 4. Calculate average heat, if goal is not reached, do cicle again ( to step 2 but before recalculate Machine-Heat again)
        
        // 5. Create struture <fileHandler, arrayOfnewDataServers>>
        
        // 6. Do the migrations
        
        // 7. Create dataServerMap again
    


    }


    // To be used in 1.
    public void calculateFileHeat()
    {
    


    }

    // To be used in 2.
    public long calculateMachineHeat()
    {


        return 1L;
    }

    // To be used in 3
    public void thermalDissipation(DataServerInfo a, DataServerInfo b)
    {



    }

    // To be used in 4.
    public long averageMachineHeat(DataServerInfo[] allDSI)
    {
        
        
        return 1L;
    }

    // To be used in 4.
    public long maxMachineHeat(DataServerInfo[] allDSI)
    {
        
        
        return 1L;
    }


}
