using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



public interface MyRemoteMetaDataInterface{

    string MetodoOla();

    //usados pelos client
    FileHandler open(string clientID, string Filename);
    void close(string ClientID, FileHandler filehandler);
    FileHandler create(string clientID, string Filename, int NBServers, int Read_Quorun, int Write_Quorun);
    void confirmCreate(string clientID, FileHandler filehandler, Boolean created);
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
    string localPort;
    string aMetaServerPort;
    string bMetaServerPort;
    int whoAmI; //0, 2 ou 4 to identify which Meta-Server it is 
    string[] dataServersPorts;

    //Array of fileTables containing file Handles
    public Dictionary<string, FileHandler>[] fileTables = new Dictionary<string, FileHandler>[6];
    
    /* Constructors */

    public MyRemoteMetaDataObject(){
        System.Console.WriteLine("Meta-Data Server is up!");
    }

    public MyRemoteMetaDataObject(string localPort, string aMetaServerPort, 
        string bMetaServerPort, string[] dataServersPorts){
        
        this.localPort = localPort;
        this.aMetaServerPort = aMetaServerPort;
        this.bMetaServerPort = bMetaServerPort;
        this.dataServersPorts = dataServersPorts;
        if (Convert.ToInt32(localPort) < Convert.ToInt32(aMetaServerPort) 
            && Convert.ToInt32(localPort) < Convert.ToInt32(bMetaServerPort))
        {whoAmI = 0;}
        else if (Convert.ToInt32(localPort) > Convert.ToInt32(aMetaServerPort)
            && Convert.ToInt32(localPort) > Convert.ToInt32(bMetaServerPort))
        {whoAmI = 4;}
        else {whoAmI = 2;}

        Console.WriteLine("Meta Server " + whoAmI + "is up!");
    }


    /* Para a thread nunca se desligar */
    public override object InitializeLifetimeService(){ return null; }


    /* Logic */
    public string MetodoOla(){
        return "[META_SERVER]   Ola eu sou o MetaData Server!";
    }

    //TODO int whoIsResponsible(string filename); Retorna um int
    //    que representa o número do responsável (para sabermos qual tabela actualizar)
    



    /************************************************************************
     *              Get Remote Object Reference Methods
     ************************************************************************/

    //TODO to other meta-data server
    //TODO to other data-server


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

    public FileHandler create(string clientID, string Filename, int NBServers, int Read_Quorun, int Write_Quorun)
    {
        /* 1. Is MetaServer Able to Respond (Fail)
         * 2. Does the file already exists?
         * 3. Create File-Handler and lock accross Meta-Data Serverss
         * 4. Return File-Handler
         */
        return null;
        //FileHandler fh = new FileHandler();
        //return fh;
    }

    public void confirmCreate(string clientID, FileHandler filehandler, Boolean created) 
    {
        /* 1. Is MetaServer Able to Respond (Fail)
         * 2. Unlock the File
         */ 
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
