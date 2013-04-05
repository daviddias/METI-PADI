using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



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
    Boolean fail();
    Boolean recover();

    //usado por outros Meta-Servers
    Boolean lockFile(string filename);
    Boolean unlockFile(string filename);
    //TODO void updateHeatTable(List<HeatTableItem> table);
}





public class MyRemoteMetaDataObject : MarshalByRefObject, MyRemoteMetaDataInterface{

    /* Atributes */
    static string localPort;
    static string aMetaServerPort;
    static string bMetaServerPort;
    static int whoAmI; //0, 2 ou 4 to identify which Meta-Server it is 
    static string[] dataServersPorts;

    //Array of fileTables containing file Handles
    public static Dictionary<string, FileHandler>[] fileTables = new Dictionary<string, FileHandler>[6];
    
    /* Constructors */

    public MyRemoteMetaDataObject(){
        System.Console.WriteLine("Meta-Data Server is up!");
    }

    public MyRemoteMetaDataObject(string _localPort, string _aMetaServerPort, 
        string _bMetaServerPort, string[] _dataServersPorts){
        
        localPort = _localPort;
        aMetaServerPort = _aMetaServerPort;
        bMetaServerPort = _bMetaServerPort;
        dataServersPorts = _dataServersPorts;
        if (Convert.ToInt32(localPort) < Convert.ToInt32(aMetaServerPort) 
            && Convert.ToInt32(localPort) < Convert.ToInt32(bMetaServerPort))
        {whoAmI = 0;}
        else if (Convert.ToInt32(localPort) > Convert.ToInt32(aMetaServerPort)
            && Convert.ToInt32(localPort) > Convert.ToInt32(bMetaServerPort))
        {whoAmI = 1;}
        else {whoAmI = 2;}

        Console.WriteLine("Meta Server " + whoAmI + "is up!");
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

        return null;
        //FileHandler fh = new FileHandler();
        //return fh;
    }

    public void close(string ClientID, FileHandler filehandler) {
        /* 1. Is MetaServer Able to Respond  (Fail)
         * 2. Has this client a lock in this file? (If yes, denied close)
         * 3. Updates the respective File-Handle by removing this user from the byWhom list
         * 4. Tells other meta-data
         */


    }

    public FileHandler create(string clientID, string filename, int nbServers, int readQuorum, int writeQuorum)
    {
        Console.WriteLine("Entered Meta-Server for Create");
        //1. Is MetaServer Able to Respond (Fail)
        //TODO

        //2. Does the file already exists?
        Console.WriteLine("Check if the File already exists");
//        if (fileTables[Utils.whichMetaServer(filename)].ContainsKey(filename))
//        { 
//            Console.WriteLine("The File already exists!");
//            return null; //TODO return exception here! 
//        }
        Console.WriteLine("The File didn't exist yet");

        //3. Decide where the fill will be hosted
        //TODO - Use info from Load Balacing to decide
        //Using all of them, by this I mean the only one

        //4. Create File-Handler 
        Console.WriteLine("Creating new File Handle");
        FileHandler fh = new FileHandler(filename, 0, nbServers, dataServersPorts, readQuorum, writeQuorum, 1);
        Console.WriteLine("Created new File Handle");
        
        //5. Save the File-Handler

        //6. Lock File accross Meta-Data Servers
        //TODO

        //7. Return File-Handler
        Console.WriteLine("Returning File Handle");
        return fh;
       
    }

    public void confirmCreate(string clientID, string filename, Boolean created) 
    {
        //1. Is MetaServer Able to Respond (Fail)
        //TODO

        //2. Unlock the File
        //TODO  



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
        return null;
        //FileHandler fh = new FileHandler();
        //return fh;
    }
    public void confirmWrite(string clientID, FileHandler filehander, Boolean wrote) 
    {
        /*
         * 
         */ 
    }

    /************************************************************************
     *              Invoked Methods by Pupper-Master
     ************************************************************************/
    public Boolean fail() { return true; }

    public Boolean recover() { return true; }

    /************************************************************************
     *              Invoked Methods by other Meta-Data Servers
     ************************************************************************/
    public Boolean lockFile(string Filename) { return true; }

    public Boolean unlockFile(string Filename) { return true; }

    //public void updateHeatTable(List<HeatTableItem> table) { }
}
