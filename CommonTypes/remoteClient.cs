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
    FileHandler open(string filename);                                                  //DONE
    void close(string filename);                                                        //DONE
    void create(string filename, int nbDataServers, int readQuorum, int writeQuorum);   //DONE                   
    void delete(string filename);                                                       //todo
    void write(int reg, byte[] byteArray);                                             //todo
    void write(int reg, int byteArray);                                             //todo
    byte[] read(int reg, int semantics, int byteArray);                                                //DONE

    //testing communication
    string metodoOla();

}



public class remoteClient : MarshalByRefObject, remoteClientInterface
{

    public static Dictionary<string, FileHandler> openFiles = new Dictionary<string, FileHandler>();
    public const int MAX_FILES_OPENED = 10;

    public string[] metaServerPort = new string[6];
    public string MS0_Address;
    public string MS1_Address;
    public string MS2_Address;

    public const string DS0_Address = "localhost:7081/MyRemoteDataObjectName";

    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;


    //Atributos
    public string clientID;

    // Register where the filehandlers are saved in Client
    List<FileHandler> register = new List<FileHandler>(10);

    // Register where the byte-arrays are saved in Client
    List<byte[]> bytes = new List<byte[]>(10);





    //Construtor
    public remoteClient(string ID, string[] metaServerPorts)
    {
        clientID = ID;
        this.metaServerPort[0] = metaServerPorts[0];
        this.metaServerPort[1] = metaServerPorts[0];
        this.metaServerPort[2] = metaServerPorts[1];
        this.metaServerPort[3] = metaServerPorts[1];
        this.metaServerPort[4] = metaServerPorts[2];
        this.metaServerPort[5] = metaServerPorts[2];

        System.Console.WriteLine("Client: - " + clientID + " -  is up!");
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

    /* File is open */
    private Boolean isOpen(string filename)
    {
        foreach (FileHandler fh in register)
            if (fh.fileName == filename)
                return true;
        return false;
    }

    /* Get filehandler from register */
    private FileHandler getFileHandler(string filename)
    {
        foreach (FileHandler fh in register)
            if (fh.fileName == filename)
                return fh;
        return null;
    }

    /************************************************************************
     *              Invoked Methods by Pupper Master
     ************************************************************************/
    public FileHandler open(string filename)
    {

        FileHandler filehandler;

        //1. Check if file is already opened.
        if (isOpen(filename))
        {
            Console.WriteLine("[CLIENT  open]:  The file is already opened!");
            return null;
        }

        //2. Check if there aren't 10 files already opened
        if (register.Count >= MAX_FILES_OPENED) {
            Console.WriteLine("[CLIENT  open]:  Can't have 10 opened files at once!");
            return null;
        }

        //3. Contact MetaServers to open
        MyRemoteMetaDataInterface meta_obj = null;
        int whichMetaServer = Utils.whichMetaServer(filename);
        meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[whichMetaServer]);
        filehandler = meta_obj.open(clientID, filename);

        if (filehandler == null){
            Console.WriteLine("[CLIENT  open]:  MetaServer didn't opened the file!");
            return null;
        }

        
        //openFiles.Add(filename, filehandler);
        register.Add(filehandler);
        Console.WriteLine("[CLIENT  open]:  Success!");
        return filehandler;
    }

    public void close(string filename)
    {

        //1. Check if file is really open
        if (!openFiles.ContainsKey(filename))
        {
            Console.WriteLine("[CLIENT  close]:  The file you want to close isn't open!");
            return;
        }

        //3. Contact MetaServers to close
        MyRemoteMetaDataInterface meta_obj = null;
        int whichMetaServer = Utils.whichMetaServer(filename);
        meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[whichMetaServer]);
        FileHandler filehandler = meta_obj.open(clientID, filename);
        meta_obj.close(clientID, filehandler);

        //4. Remove from Open Files 
        openFiles.Remove(filename);
        Console.WriteLine("[CLIENT  close]:  Success!");
        return;
    }

    public void create(string filename, int nbDataServers, int readQuorum, int writeQuorum)
    {
        //1. Find out which Meta-Server to Call
        //Console.WriteLine("Discovering the right MetaData Server");
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
        //Console.WriteLine("Meta-Data Server: " + Utils.whichMetaServer(filename));


        //2. If not available, try next one
        //TODO

        //3. Get File-Handle
        //Console.WriteLine("Request the File Handle");
        FileHandler fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        //Console.WriteLine("File Handle Request sucess");

        //4. Save File-Handle
        //TODO - Implement a function to do this (also one to remove)


        //5. Contact Data-Servers to Prepare
        //Console.WriteLine("Launching Prepare to Commit");
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.prepareCreate(this.clientID, filename);
        }
        //Console.WriteLine("Launched Prepare to Commit");

        //6. Contact Data-Servers to Commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.commitCreate(this.clientID, filename);
        }


        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmCreate(this.clientID, filename, true);

        Console.WriteLine("[CLIENT  create] Success!");
        return;
    }

    public void delete(string filename)
    {
        return;
    }

    public void write(int reg, Byte[] byteArray)
    {

        FileHandler fh = null;

        //1.Find if this client has this file opened
        if (!isOpen(register[reg].fileName))
        {
            Console.WriteLine("[CLIENT  write]:  File is not yet opened!");
            return;
        }

        fh = register[reg];

        //2. Find out which Meta-Server to Call
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(fh.fileName)]);


        //3. If not available, try next one
        //TODO

        //4. Cotact metaserver for write operation
        if (mdi.write(this.clientID, fh) == null)
        {
            Console.WriteLine("[CLIENT  write]  Metaserve did not gave permission to write!");
            return;
        }

        //5. Contact Data-Servers to Prepare
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.prepareWrite(this.clientID, fh.fileName, byteArray);
        }

        //6. Contact Data-Servers to Commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.commitWrite(this.clientID, fh.fileName);
        }


        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmWrite(this.clientID, fh, true);

        Console.WriteLine("[CLIENT  write]  Success");
        return;
    }

    public void write(int reg, int byteArray)
    {
        write(reg, bytes[byteArray]);
    }

    public byte[] read(int reg, int semantics, int byteArray) {

        byte[] content;

        FileHandler fh = null;

        //1.Find if this client has this file opened
        if (!isOpen(register[reg].fileName))
        {
            Console.WriteLine("[CLIENT  read]:  File is not yet opened!");
            return null;
        }

        fh = register[reg];

        //3. Contact Data-Server to read
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            content = di.read(@"C:\" + fh.fileName, semantics);

            if (content != null)
                Console.WriteLine("[CLIENT  read]:  Success!");
            bytes.Insert(byteArray, content);
            return content;
        }

        Console.WriteLine("[CLIENT  read]:  Success!");
        return null;
    }
}
