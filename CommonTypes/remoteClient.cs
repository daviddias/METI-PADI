using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using log4net;




public interface remoteClientInterface { 

    //usado pelo puppet-master
    void open(string filename);                                                          //DONE
    void close(string filename);                                                        //DONE
    void create(string filename, int nbDataServers, int readQuorum, int writeQuorum);   //DONE                   
    void delete(string filename);                                                       //todo
    void write(int reg, byte[] byteArray);                                              //DONE
    void write(int reg, int byteArray);                                                 //DONE
    void read(int reg, int semantics, int byteArray);                                    //DONE

    //testing communication
    string metodoOla();

}



public class remoteClient : MarshalByRefObject, remoteClientInterface
{

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
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        

    // Register where the filehandlers are saved in Client
    public static List<FileHandler> register;

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

        bytes = new List<byte[]>(10);
        register = new List<FileHandler>(10);

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
        Console.WriteLine("[CLIENT isOpened] Count:" + register.Count);
            
        {
            System.Console.WriteLine("[CLIENT isOpened] No open files in this client!");
            return false;
        }
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
    public void open(string filename)
    {

        FileHandler filehandler;

        //1. Check if file is already opened.
        if (isOpen(filename))
        {
            Console.WriteLine("[CLIENT  open]:  The file is already opened!");
            return;
        }

        //2. Check if there aren't 10 files already opened
        if (register.Count >= MAX_FILES_OPENED) {
            Console.WriteLine("[CLIENT  open]:  Can't have 10 opened files at once!");
            return;
        }

        //3. Contact MetaServers to open
        MyRemoteMetaDataInterface meta_obj = null;
        int whichMetaServer = Utils.whichMetaServer(filename);
        meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[whichMetaServer]);
        filehandler = meta_obj.open(clientID, filename);

        if (filehandler == null){
            Console.WriteLine("[CLIENT  open]:  MetaServer didn't opened the file!");
            return;
        }

        
        //openFiles.Add(filename, filehandler);
        register.Add(filehandler);
        Console.WriteLine("[CLIENT  open]:  Success!");
        return;
    }

    public void close(string filename)
    {
        FileHandler filehandler = null;
        //1. Check if file is really open
        foreach (FileHandler fh in register){
            if (fh.fileName == filename){
                filehandler = fh;
                break;
            }
        }


        if (filehandler == null)
        {
            Console.WriteLine("[CLIENT  close]:  The file you want to close isn't open!");
            return;
        }

        //3. Contact MetaServers to close
        MyRemoteMetaDataInterface meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
        meta_obj.close(clientID, filehandler);


        //4. Remove from Open Files 
        register.Remove(filehandler);
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
        //log.Info("Launching Prepare to Commit");
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.prepareCreate(this.clientID, filename);
        }
        //log.Info("Launched Prepare to Commit");


        //6. Contact Data-Servers to Commit
        //log.Info("Going to start commit");
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            //log.Info("ENTREI NO LOOP DE COMMIT");
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            //log.Info("VOU INVOCAR O COMMIT CREATE : ATRIBUTOS :  " + this.clientID + "   " + filename);
            
            di.commitCreate(this.clientID, filename);
            
        }
        //log.Info("Commit with Data Servers done");

        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmCreate(this.clientID, filename, true);

        log.Info("[CLIENT  create] Success!");
        return;
    }

    public void delete(string filename)
    {

        //1. Find out which meta server to call
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);

        //2. If not available try next one
        //TODO

        //3. Invoke delete on meta server
        FileHandler fh = mdi.delete(this.clientID, filename);

        if (fh == null) {
            Console.WriteLine("[CLIENT  delete] Meta Server failed to delete!");
            return;
        }

        //4. Contact data-server to prepare
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.prepareDelete(this.clientID, fh.fileName);
        }

        //5. Contact data-servers to commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {

            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.commitDelete(this.clientID, fh.fileName);
        }

        //6. Tell metaserver to confirm deletion
        mdi.confirmDelete(this.clientID, fh, true);

        Console.WriteLine("[CLIENT  delete]  Success");
        return;

    }

    public void write(int reg, Byte[] byteArray)
    {
        FileHandler fh = null;

        //1.Find if this client has this file opened
        if (register.Count == 0 || !isOpen(register[reg].fileName))
        {
            Console.WriteLine("[CLIENT  write]:  File is not yet opened!");
            return;
        }

        Console.WriteLine("[CLIENT  write]:  Fucheiro ta aberto!");

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

    public void read(int reg, int semantics, int byteArray) {
        //Console.WriteLine("[CLIENT  read]:  Chamou o read!");
        byte[] content;
        
        //1.Find if this client has this file opened
        if (register.Count == 0 || !isOpen(register[reg].fileName))
        {
            Console.WriteLine("[CLIENT  read]:  File is not yet opened!");
            return;
        }
        //Console.WriteLine("[CLIENT  read]:  O ficheiro esta aberto!");

        FileHandler fh = register[reg];

        //3. Contact Data-Server to read
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            content = di.read(fh.fileName, semantics);

            if (content != null)
            {
                Console.WriteLine("[CLIENT  read]:  Success! " + System.Text.Encoding.Default.GetString(content));
                bytes.Insert(byteArray, content);
                return;
            }
        }

        Console.WriteLine("[CLIENT  read]:  Data Server could not read the file!");
        return;
    }
}
