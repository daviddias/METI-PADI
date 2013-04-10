using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using log4net;




public interface MyRemoteMetaDataInterface{

    string MetodoOla();

    //usados pelos client
    FileHandler open(string clientID, string filename);
    void close(string ClientID, FileHandler filehandler);
    FileHandler create(string clientID, string filename, int nbServers, int readQuorum, int writeQuorum);
    void confirmCreate(string clientID, string filename, Boolean created);
    FileHandler delete(string clientID, FileHandler filehandler);
    void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted);
    FileHandler write(string clientID, FileHandler filehandler);
    void confirmWrite(string clientID, FileHandler filehander, Boolean wrote);

    //usado pelo Puppet-Master
    void fail();
    void recover();
    void dump();

    //usado por outros Meta-Servers
    Boolean lockFile(string filename);
    Boolean unlockFile(string filename);
    //TODO void updateHeatTable(List<HeatTableItem> table);
}





public class MyRemoteMetaDataObject : MarshalByRefObject, MyRemoteMetaDataInterface{

    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    /* Atributes */
    static string localPort;
    static string aMetaServerPort;
    static string bMetaServerPort;
    static int whoAmI; //0, 2 ou 4 to identify which Meta-Server it is 
    static string[] dataServersPorts;
    static Boolean isfailed;

    //Array of fileTables containing file Handlers
    public static Dictionary<string, FileHandler>[] fileTables = new Dictionary<string, FileHandler>[6];
    
    /* Constructors */

    public MyRemoteMetaDataObject(){
        isfailed = false;

        for (int i = 0; i < 6; i++)
            fileTables[i] = new Dictionary<string, FileHandler>();

        log.Info("Meta-Data Server is up!");
    }

    public MyRemoteMetaDataObject(string _localPort, string _aMetaServerPort, string _bMetaServerPort, string[] _dataServersPorts){

        isfailed = false;
        localPort = _localPort;
        aMetaServerPort = _aMetaServerPort;
        bMetaServerPort = _bMetaServerPort;
        dataServersPorts = _dataServersPorts;

        for (int i = 0; i < 6; i++)
            fileTables[i] = new Dictionary<string, FileHandler>();

            if (Convert.ToInt32(localPort) < Convert.ToInt32(aMetaServerPort)
                && Convert.ToInt32(localPort) < Convert.ToInt32(bMetaServerPort))
            { whoAmI = 0; }
            else if (Convert.ToInt32(localPort) > Convert.ToInt32(aMetaServerPort)
                && Convert.ToInt32(localPort) > Convert.ToInt32(bMetaServerPort))
            { whoAmI = 1; }
            else { whoAmI = 2; }

        log.Info("Meta Server " + whoAmI + " is up!");
    }


    /* Para a thread nunca se desligar */
    public override object InitializeLifetimeService(){ return null; }


    /* Logic */
    public string MetodoOla(){
        return "[META_SERVER]   Ola eu sou o MetaData Server!";
    }



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

        //4. Tells the other MetaServers to update
        //TODO

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

        //4. Tells the other MetaServers to update
        //TODO

        log.Info("[METASERVER: close]    Success)!");
    }

    public FileHandler create(string clientID, string filename, int nbServers, int readQuorum, int writeQuorum)
    {
        FileHandler fh;
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: create]    The server has is on 'fail'!");
            return null;
        }

        //2. Does the file already exists? 
        if (fileTables[Utils.whichMetaServer(filename)].ContainsKey(filename))
        {
            log.Info("[METASERVER: create]    File already exists");
            return null; //TODO return exception here! 
        }

        //3. Decide where the fill will be hosted
        //TODO - Use info from Load Balacing to decide
        //Using all of them, by this I mean the only one

        //4. Create File-Handler 
        //Console.WriteLine("Creating new File Handle");
        fh = new FileHandler(filename, 0, nbServers, dataServersPorts, readQuorum, writeQuorum, 1);
        //Console.WriteLine("Created new File Handle");
        
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
    }

    public FileHandler delete(string clientID, FileHandler filehandler)
    {
        /*
         *  
         */
        return null;
        //FileHandler fh = new FileHandler();
        //return fh;
    }

    public void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted) 
    {
        /*
         * 
         */ 
    }

    public FileHandler write(string clientID, FileHandler filehandler)
    {

        Boolean flag = false;

        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: write]    The server has is on 'fail'!");
            return null;
        }

        //2. Does the file already exists? 
        if (!fileTables[Utils.whichMetaServer(filehandler.fileName)].ContainsKey(filehandler.fileName))
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

        //6. Devolve o filehandler ao cliente
        log.Info("[METASERVER: write]    Success!");
        return filehandler;
    }
        
    public void confirmWrite(string clientID, FileHandler filehander, Boolean wrote) 
    {
        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[METASERVER: confirmWrite]    The server has is on 'fail'!");
            return;
        }

        //2. Faz unlock ao ficheiro
        filehander.isLocked = false;
        log.Info("[METASERVER: confirmWrite]    Success!");
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



        log.Info("[METASERVER: fail]    Success!");
        return;
    }

    public void recover() {
        if (isfailed == false)
        {
            log.Info("[METASERVER: recover]    The server was not failed!");
            return;
        }
        isfailed = false;
        return;
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

        return;
    }

    /************************************************************************
     *              Invoked Methods by other Meta-Data Servers
     ************************************************************************/
    public Boolean lockFile(string Filename) { return true; }

    public Boolean unlockFile(string Filename) { return true; }

    //public void updateHeatTable(List<HeatTableItem> table) { }
}
