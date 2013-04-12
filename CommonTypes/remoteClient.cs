using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Net.Sockets;
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
    string metodoOla(); //testing communication
}


public class remoteClient : MarshalByRefObject, remoteClientInterface
{
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
    public const int MAX_FILES_OPENED = 10;

    public string[] metaServerPort = new string[6];
    // Read Modes
    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;

    // Atributes
    public string clientID;
        
    // File-Register and Byte-Array Register
    public static List<FileHandler> register;                   
    public static List<byte[]> byteArrayRegister = new List<byte[]>(10);

   
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

        byteArrayRegister = new List<byte[]>(10);
        register = new List<FileHandler>(10);

        readQuorum = new Dictionary<string, int>();
        writeQUORUM = new Dictionary<string,int>();
        createQUORUM = new Dictionary<string, int>();
        deleteQUORUM = new Dictionary<string, int>();

        System.Console.WriteLine("Client: - " + clientID + " -  is up!");
    }

    /* Live forever */
    public override object InitializeLifetimeService() { return null; }

    /* communication testing */
    public string metodoOla() { return "[CLIENT]   Ola eu sou o Client!"; }

    /* File is open */
    private Boolean isOpen(string filename)
    {
        foreach (FileHandler fh in register)
            if (fh.filenameGlobal == filename) //!Not sure if the comparison works in C# -> Check it!
                return true;
        return false;
    }

    /* Get filehandler from register */
    private FileHandler getFileHandler(string filename)
    {
        foreach (FileHandler fh in register)
            if (fh.filenameGlobal == filename)
                return fh;
        return null;
    }










    /************************************************************************
     *          
     *                 Invoked Methods by Pupper Master
     *              
     ************************************************************************/






    /*------------------------------------------------------------------------         
     *                                   CREATE
     *-----------------------------------------------------------------------*/

    // Delegates
    public delegate TransactionDTO prepareCreateRemoteAsyncDelegate(TransactionDTO dto);
    public delegate TransactionDTO commitCreateRemoteAsyncDelegate(TransactionDTO dto);

    private static Dictionary<string, int> createQUORUM;  //Key - Transaction ID ; int - number of responses


    // Callbacks
    public static void prepareCreateRemoteAsyncCallBack(IAsyncResult ar)
    {
        prepareCreateRemoteAsyncDelegate del = (prepareCreateRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " CREATE ::  Call Back Received - PrepareCreate for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (createQUORUM.ContainsKey(assyncResult.transactionID)) { createQUORUM[assyncResult.transactionID]++; }
            else { createQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    public static void commitCreateRemoteAsyncCallBack(IAsyncResult ar)
    {  
        commitCreateRemoteAsyncDelegate del = (commitCreateRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " CREATE ::  Call Back Received - CommitCreate for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (createQUORUM.ContainsKey(assyncResult.transactionID)) { createQUORUM[assyncResult.transactionID]++; }
            else { createQUORUM.Add(assyncResult.transactionID, 1); }      
        }
        return;
    }

    // Remote method
    public void create(string filename, int nbDataServers, int readQuorum, int writeQuorum)
    {
        //1. Find out which Meta-Server to Call
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
        log.Info(this.clientID + " CREATE ::  Meta-Server to contact: " + Utils.whichMetaServer(filename));

        //2. If not available, try next one
        //TODO

        //3. Get File-Handle
        log.Info(this.clientID + " CREATE ::  Sending create request to Meta-Server");
        FileHandler fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        if (fh == null)
        {
            log.Info(this.clientID + " CREATE :: Meta Server didn't create the file!"); // Change this to know the reason
            return;
        }
        log.Info(this.clientID + " CREATE ::  Received the File Handler of file: " + fh.filenameGlobal);
        

        //4. Save File-Handle
        //TODO - Implement a function to do this (also one to remove)


        //5. Contact Data-Servers to Prepare
        log.Info(this.clientID + " CREATE ::  Initiating 2PC");
        string transactionID = Utils.generateTransactionID(); 
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareCreateRemoteAsyncDelegate RemoteDel = new prepareCreateRemoteAsyncDelegate(di.prepareCreate);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareCreateRemoteAsyncCallBack);
            TransactionDTO prepateDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(prepateDTO, RemoteCallback, null);
            //di.prepareCreate(this.clientID, filename); SYNC
        }
        log.Info(this.clientID + " CREATE ::  2PC 1st Phase - Prepare Create Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(1); // Wait 1ms to avoid that the second server receive a commit before a prepare

            lock (createQUORUM)
            {
                if (createQUORUM.ContainsKey(transactionID) ) //the fh.writeQuorum is the same as createQuorum
                {
                    if (createQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info(this.clientID + " CREATE :: Reached necessary Quorum of: " + fh.writeQuorum + " : number of machines that are prepared: " + createQUORUM[transactionID]);
                        createQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //6. Contact Data-Servers to Commit
        transactionID = Utils.generateTransactionID(); //Generating new transaction ID for Commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            commitCreateRemoteAsyncDelegate RemoteDel = new commitCreateRemoteAsyncDelegate(di.commitCreate);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.commitCreateRemoteAsyncCallBack);
            TransactionDTO commitDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(commitDTO, RemoteCallback, null);
            //di.commitCreate(this.clientID, filename);
        }
        log.Info(this.clientID + " CREATE ::  2PC 2nd Phase - Commit Create Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(1); // Wair 1ms to avoid that the second server receive a commit before a prepare

            lock (createQUORUM)
            {
                if (createQUORUM.ContainsKey(transactionID)) //Write Quorum is the same as Create on filehandler
                {
                    if (createQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info(this.clientID + " CREATE :: Reached necessary Quorum of: " + fh.writeQuorum + " : number of machines that are prepared to commit: " + createQUORUM[transactionID]);
                        createQUORUM.Remove(transactionID);
                        break;
                    }
                }
                   
            }
        }

        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmCreate(this.clientID, filename, true); 
        //TODO need to check if this guy is still up!
        log.Info(this.clientID + " CREATE :: Confirmation Sent to Meta-Data Server, operation complete");
        return;
    }



    /*------------------------------------------------------------------------         
     *                                   OPEN
     *-----------------------------------------------------------------------*/
    
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
        MyRemoteMetaDataInterface meta_obj = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
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


    /*------------------------------------------------------------------------         
     *                                   CLOSE
     *-----------------------------------------------------------------------*/

    public void close(string filename)
    {
        FileHandler filehandler = null;
        //1. Check if file is really open
        foreach (FileHandler fh in register){
            if (fh.filenameGlobal == filename){
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





    /*------------------------------------------------------------------------         
     *                                   DELETE
     *-----------------------------------------------------------------------*/

    //Asynchronous calls delegates
    //public delegate TransactionDTO prepareDeleteRemoteAsyncDelegate(string clientID, string local_file_name);
    //public delegate TransactionDTO commitDeleteRemoteAsyncDelegate(string clientID, string local_file_name);
    public delegate TransactionDTO prepareDeleteRemoteAsyncDelegate(TransactionDTO dto);
    public delegate TransactionDTO commitDeleteRemoteAsyncDelegate(TransactionDTO dto);

    private static Dictionary<string, int> deleteQUORUM;  //Key - Transaction ID ; int - number of responses

    public static void prepareDeleteRemoteAsyncCallBack(IAsyncResult ar)
    {
        prepareDeleteRemoteAsyncDelegate del = (prepareDeleteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " DELETE ::  Call Back Received - PrepareDelete for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (deleteQUORUM.ContainsKey(assyncResult.transactionID)) { deleteQUORUM[assyncResult.transactionID]++; }
            else { deleteQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    public static void commitDeleteRemoteAsyncCallBack(IAsyncResult ar)
    {
        commitDeleteRemoteAsyncDelegate del = (commitDeleteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " DELETE ::  Call Back Received - CommitDelete for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (deleteQUORUM.ContainsKey(assyncResult.transactionID)) { deleteQUORUM[assyncResult.transactionID]++; }
            else { deleteQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;    
    }

    public void delete(string filename)
    {

        //1. Find out which meta server to call
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(filename)]);
        log.Info(this.clientID + " DELETE ::  Meta-Server to Contact: " + Utils.whichMetaServer(filename));

        //2. If not available try next one
        //TODO

        //3. Invoke delete on meta server
        log.Info(this.clientID + " DELETE ::  Sending create request to Meta-Server");
        FileHandler fh = mdi.delete(this.clientID, filename);
        if (fh == null) {
            log.Info(this.clientID + " DELETE :: Meta Server doesn't have a reference of the file to delete!"); 
            return;
        }
        log.Info(this.clientID + " DELETE ::  Received the File Handler of file: " + fh.filenameGlobal);

        //4. Contact data-server to prepare
        log.Info(this.clientID + " DELETE ::  Initiating 2PC");
        string transactionID = Utils.generateTransactionID(); 
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareDeleteRemoteAsyncDelegate RemoteDel = new prepareDeleteRemoteAsyncDelegate(di.prepareDelete);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareDeleteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);
            //di.prepareDelete(this.clientID, fh.dataServersFiles[dataServerPort]); SYNC
        }
        log.Info(this.clientID + " DELETE ::  2PC 1st Phase - Prepare Create Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(1); // Wait 1ms to avoid that the second server receive a commit before a prepare

            lock (deleteQUORUM)
            {
                if (deleteQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (deleteQUORUM[transactionID] >= fh.nbServers)
                    {
                        log.Info(this.clientID + " DELETE :: Reached necessary Quorum(TOTAL) of: " + fh.nbServers + " : number of machines that are prepared: " + deleteQUORUM[transactionID]);
                        deleteQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }


        //5. Contact data-servers to commit
        transactionID = Utils.generateTransactionID(); //Generating new transaction ID for Commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            commitDeleteRemoteAsyncDelegate RemoteDel = new commitDeleteRemoteAsyncDelegate(di.commitDelete);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.commitDeleteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);
            //di.commitDelete(this.clientID, fh.dataServersFiles[dataServerPort]); // SYNC
        }
        log.Info(this.clientID + " DELETE ::  2PC 2nd Phase - Commit Delete Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(1); // Wair 1ms to avoid that the second server receive a commit before a prepare

            lock (deleteQUORUM)
            {
                if (deleteQUORUM.ContainsKey(transactionID)) //Write Quorum is the same as Create on filehandler
                {
                    if (deleteQUORUM[transactionID] >= fh.nbServers)
                    {
                        log.Info(this.clientID + " DELETE :: Reached necessary Quorum(TOTAL) of: " + fh.nbServers + " : number of machines that are prepared to commit: " + deleteQUORUM[transactionID]);
                        deleteQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }


        //6. Tell metaserver to confirm deletion
        mdi.confirmDelete(this.clientID, fh, true);
        //TODO if not there, tell another!

        log.Info(this.clientID + " DELETE ::  Confirmation Sent to Meta-Data Server, operation complete");
        return;
    }






    /*------------------------------------------------------------------------         
     *                                   WRITE
     *-----------------------------------------------------------------------*/

    // Quorun processing dictionaries
    private static Dictionary<string, int> writeQUORUM;  //Key - Transaction ID ; int - number of responses

    // Delegates
    public delegate TransactionDTO prepareWriteRemoteAsyncDelegate(string clientID, string local_file_name, byte[] byte_array);
    public delegate TransactionDTO commitWriteRemoteAsyncDelegate(string clientID, string local_file_name);


    // Callbacks
    public static void prepareWriteRemoteAsyncCallBack(IAsyncResult ar)
    {
        // Alternative 2: Use the callback to get the return value
        prepareWriteRemoteAsyncDelegate del = (prepareWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        Console.WriteLine("\r\n**SUCCESS**: Result of the remote AsyncCallBack: " + del.EndInvoke(ar));

        return;
    }

    public static void commitWriteRemoteAsyncCallBack(IAsyncResult ar)
    {
        // Alternative 2: Use the callback to get the return value
        commitWriteRemoteAsyncDelegate del = (commitWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        Console.WriteLine("\r\n**SUCCESS**: Result of the remote AsyncCallBack: " + del.EndInvoke(ar));

        return;
    }

    // Remote Methods
    public void write(int reg, Byte[] byteArray)
    {
        FileHandler fh = null;

        //1.Find if this client has this file opened
        if (register.Count == 0 || !isOpen(register[reg].filenameGlobal))
        {
            Console.WriteLine("[CLIENT  write]:  File is not yet opened!");
            return;
        }

        Console.WriteLine("[CLIENT  write]:  Fucheiro ta aberto!");

        fh = register[reg];

        //2. Find out which Meta-Server to Call
        MyRemoteMetaDataInterface mdi = Utils.getRemoteMetaDataObj(metaServerPort[Utils.whichMetaServer(fh.filenameGlobal)]);


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
            di.prepareWrite(this.clientID, fh.dataServersFiles[dataServerPort], byteArray);
        }

        //6. Contact Data-Servers to Commit
        foreach (string dataServerPort in fh.dataServersPorts)
        {

            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            di.commitWrite(this.clientID, fh.dataServersFiles[dataServerPort]);
        }


        //7. Tell Meta-Data Server to Confirm Creation 
        mdi.confirmWrite(this.clientID, fh, true);

        Console.WriteLine("[CLIENT  write]  Success");
        return;
    }

    /* To be used for reference of byteArrayRegister */
    public void write(int reg, int byteArrayRegisterIndex)
    {
        write(reg, byteArrayRegister[byteArrayRegisterIndex]);
    }








    /*------------------------------------------------------------------------         
     *                                   READ
     *-----------------------------------------------------------------------*/

    private static Dictionary<string, int> readQuorum; //String - Transaction ID ; int - Counter //TODO mudar para o Value ser um array de ints da dimensão nbservers, com as várias versões


    public void read(int reg, int semantics, int byteArray) {
        //Console.WriteLine("[CLIENT  read]:  Chamou o read!");
        byte[] content;
        
        //1.Find if this client has this file opened
        if (register.Count == 0 || !isOpen(register[reg].filenameGlobal))
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
            content = di.read(fh.dataServersFiles[dataServerPort], semantics);

            if (content != null)
            {
                Console.WriteLine("[CLIENT  read]:  Success! " + System.Text.Encoding.Default.GetString(content));
                byteArrayRegister.Insert(byteArray, content);
                return;
            }
        }

        Console.WriteLine("[CLIENT  read]:  Data Server could not read the file!");
        return;
    }
}
