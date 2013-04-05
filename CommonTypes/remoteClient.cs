using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;




public interface remoteClientInterface { 

    //usado pelo puppet-master
    void open(string filename);                         //TODO
    void create(string filename, int nbDataServers, int readQuorum, int writeQuorum);                      
    void delete(string filename);                       //TODO
    void write(string filename, byte[] byte_array);     //TODO

    //testing communication
    string metodoOla();
}



public class remoteClient : MarshalByRefObject, remoteClientInterface
{
    
    FileHandler[] openFiles;

    public string[] metaServerPort = new string[6];
    public string MS0_Address;
    public string MS1_Address;
    public string MS2_Address;

    public const string DS0_Address = "localhost:7081/MyRemoteDataObjectName";

    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;


    //Atributos
    public string clientID;
    
    //Construtor
    public remoteClient(string ID, string[] metaServerPorts)
    {
        openFiles = new FileHandler[10];
        clientID = ID;
        this.metaServerPort[0] = metaServerPorts[0];
        this.metaServerPort[1] = metaServerPorts[0];
        this.metaServerPort[2] = metaServerPorts[1];
        this.metaServerPort[3] = metaServerPorts[1];
        this.metaServerPort[4] = metaServerPorts[2];
        this.metaServerPort[5] = metaServerPorts[2];

        System.Console.WriteLine("Client: - " + clientID  +" -  is up!");
    }

    public override object InitializeLifetimeService()
    {
        return null;
    }

    /* communication testing */
    public string metodoOla()
    {
        return "[CLIENT]   Ola eu sou o Client!";
    }



    /************************************************************************
     *              Invoked Methods by Pupper Master
     ************************************************************************/
    public void open(string filename) {

        MyRemoteMetaDataInterface meta_obj = null;
        int whichMetaServer = Utils.whichMetaServer(filename);
        meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[whichMetaServer]);
        meta_obj.open(clientID, filename);

        return; 
    }
    public void create(string filename, int nbDataServers, int readQuorum, int writeQuorum) 
    { 
        //1. Find out which Meta-Server to Call
        Console.WriteLine("Discovering the right MetaData Server");
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
        Console.WriteLine("Meta-Data Server: " + Utils.whichMetaServer(filename));
        

        //2. If not available, try next one
        //TODO

        //3. Get File-Handle
        Console.WriteLine("Request the File Handle");
        FileHandler fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        Console.WriteLine("File Handle Request sucess");

        //4. Save File-Handle
        //TODO - Implement a function to do this (also one to remove)


        //5. Contact Data-Servers to Prepare
        Console.WriteLine("Launching Prepare to Commit");
        foreach(string dataServerPort in fh.dataServersPorts){
           MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.prepareCreate(this.clientID, filename);
        }
        Console.WriteLine("Launched Prepare to Commit");

        //6. Contact Data-Servers to Commit
        foreach(string dataServerPort in fh.dataServersPorts){
           MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.commitCreate(this.clientID, filename);
        }

        
        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmCreate(this.clientID, filename, true);
        
        return; 
    } 

    public void delete(string filename) 
    { 
        return; 
    }
    public void write(string filename, byte[] byte_array) 
    {
        return;
    }
}
