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
using System.Threading;
using log4net;



public interface remoteClientInterface { 
    //usado pelo puppet-master
    void open(string filename);                                                         
    void close(string filename);                                                        
    void create(string filename, int nbDataServers, int readQuorum, int writeQuorum);                      
    void delete(string filename);                                                       
    void write(int reg, byte[] byteArray);                                              
    void write(int reg, int byteArray);                                                 
    void read(int reg, int semantics, int byteArray);                                   
    void exeScript(List<string> commands);
    void copy(int reg1, int semantics, int reg2, string salt);
    string metodoOla(); //this method exists for communication testing purposes
    void dump();                                                                       
}

public class remoteClient : MarshalByRefObject, remoteClientInterface
{
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    
    // Atributes
    public string clientID;
    public string[] metaServerPorts = new string[6];
    
    // File-Register and Byte-Array Register
    public static List<FileHandler> fileRegister;                   
    public static List<byte[]> byteArrayRegisterOLD;
    public static List<ByteArrayRecord> byteArrayRegister;

    //Construtor
    public remoteClient(string ID, string[] metaServerPorts)
    {
        clientID = ID;
        this.metaServerPorts[0] = metaServerPorts[0];
        this.metaServerPorts[1] = metaServerPorts[0];
        this.metaServerPorts[2] = metaServerPorts[1];
        this.metaServerPorts[3] = metaServerPorts[1];
        this.metaServerPorts[4] = metaServerPorts[2];
        this.metaServerPorts[5] = metaServerPorts[2];

        fileRegister = new List<FileHandler>(Constants.MAX_FILES_OPENED);
        
        byteArrayRegisterOLD = new List<byte[]>(Constants.MAX_FILES_OPENED);
        byteArrayRegister = new List<ByteArrayRecord>(Constants.MAX_FILES_OPENED);

        for (int i = 0; i < Constants.MAX_FILES_OPENED; i++)
        {
            byteArrayRegisterOLD.Add(null);
        }

        readQUORUM = new Dictionary<string, List<TransactionDTO>>();
        writeQUORUM = new Dictionary<string,int>();
        createQUORUM = new Dictionary<string, int>();
        deleteQUORUM = new Dictionary<string, int>();

        log.Info("CLIENT :: " + this.clientID + " -  is up!");
    }

    /* (tune)I'm gonna live forever lalala*/
    public override object InitializeLifetimeService() { return null; }

    /* communication testing */
    public string metodoOla() { return "[CLIENT]   Ola eu sou o Client!"; }

    /* File is open */
    private Boolean isOpen(string filename)
    {
        foreach (FileHandler fh in fileRegister)
            if (fh.filenameGlobal == filename) 
                return true;
        return false;
    }

    /* Get filehandler from fileRegister */
    private FileHandler getFileHandler(string filename)
    {
        foreach (FileHandler fh in fileRegister)
            if (fh.filenameGlobal == filename)
                return fh;
        log.Info("CLIENT :: Couldn't find a File Handler with this filename: " + filename + " on client");
        return null;
    }



    /* File is open */
    private int nextFreeByteArrayRegister()
    {
        for(int i = 0; i < byteArrayRegisterOLD.Capacity; i++)
        {
            if (byteArrayRegisterOLD[i] == null)
                return i;
        }
        log.Info("CLIENT :: All ByteArraysRegisters are occupied"); 
        return -1;
    }

    /**************************************************************************************************
     *          
     *                              Invoked Methods by Pupper Master
     *              
     **************************************************************************************************/


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
        log.Info("CLIENT :: c-" + assyncResult.clientID + " CREATE" + " Call Back Received - PrepareCreate for transaction: " + assyncResult.transactionID);
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
        log.Info("CLIENT :: c-" + assyncResult.clientID + " CREATE" + " Call Back Received - CommitCreate for transaction: " + assyncResult.transactionID);
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
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
   
        //2. Get File-Handle
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Sending create request to Meta-Server");
        FileHandler fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        while (fh.nbServers == 0) // if there aren't enough data servers meta server sends always nbServer = 0
        {
            System.Threading.Thread.Sleep(5000);
            fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        }
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Received the File Handler of file: " + fh.filenameGlobal);


        //3. Contact Data-Servers to Prepare
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Initiating 2PC");
        //System.Threading.Thread.Sleep(2000);
        string transactionID = Utils.generateTransactionID();
        sendPREPARECREATE:
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareCreateRemoteAsyncDelegate RemoteDel = new prepareCreateRemoteAsyncDelegate(di.prepareCreate);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareCreateRemoteAsyncCallBack);
            TransactionDTO prepateDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(prepateDTO, RemoteCallback, null);
            }
            catch
            {
                log.Info("O Data Server " + dataServerPort + " não faz mal, se não atingirmos o quorum tentamos de novo");
            }
            //di.prepareCreate(this.clientID, filename); SYNC
        }
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " 2PC 1st Phase - Prepare Create Assync Calls Sent");


        DateTime timeSentPrepareCreate = DateTime.Now;

        while (true)
        {

            if(DateTime.Now.Subtract(timeSentPrepareCreate).TotalSeconds > 10.0){
                log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Didn't reach quorum in time, sending assyncs again");
                goto sendPREPARECREATE;
            }
            System.Threading.Thread.Sleep(10); // Wait 10ms to avoid that the second server receive a commit before a prepare
            lock (createQUORUM)
            {
                if (createQUORUM.ContainsKey(transactionID) ) //the fh.writeQuorum is the same as createQuorum
                {
                    if (createQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Reached necessary Quorum of: " + fh.writeQuorum + " : number of machines that are prepared: " + createQUORUM[transactionID]);
                        createQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //4. Contact Data-Servers to Commit
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
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " 2PC 2nd Phase - Commit Create Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(10); // Wair 10ms to avoid that the second server receive a commit before a prepare
            lock (createQUORUM)
            {
                if (createQUORUM.ContainsKey(transactionID)) //Write Quorum is the same as Create on filehandler
                {
                    if (createQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Reached necessary Quorum of: " + fh.writeQuorum + " : number of machines that are prepared to commit: " + createQUORUM[transactionID]);
                        createQUORUM.Remove(transactionID);
                        break;
                    }
                }
                   
            }
        }

        //5. Save FileHandler
        fileRegister.Add(fh);
        
        //6. Tell Meta-Data Server to Confirm Creation
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdiConfirm.confirmCreate(this.clientID, filename, true);
        log.Info("CLIENT :: c-" + this.clientID + " CREATE" + " Confirmation Sent to Meta-Data Server, OPERATION COMPLETE");
        return;
    }


    /*------------------------------------------------------------------------         
     *                                   OPEN
     *-----------------------------------------------------------------------*/
    
    public void open(string filename)
    {
        log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " Opening file: " + filename);
        FileHandler filehandler;

        //1. Check if file is already opened.
        if (isOpen(filename))
        {
            log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " The file is already opened!");
            return;
        }

        //2. Check if there aren't 10 files already opened
        if (fileRegister.Count >= Constants.MAX_FILES_OPENED) {
            log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " Can't have 10 opened files at once!");
            return;
        }

        //3. Contact MetaServers to open
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        filehandler = mdi.open(clientID, filename);

        if (filehandler == null){
            log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " MetaServer didn't opened the file!");
            return;
        }

        //openFiles.Add(filename, filehandler);
        fileRegister.Add(filehandler);
        log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " OPERATION SUCCESS");
        return;
    }


    /*------------------------------------------------------------------------         
     *                                   CLOSE
     *-----------------------------------------------------------------------*/

    public void close(string filename)
    {
        log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " Closing file: " + filename);
        FileHandler filehandler = null;
        //1. Check if file is really open
        foreach (FileHandler fh in fileRegister){
            if (fh.filenameGlobal == filename){
                filehandler = fh;
                break;
            }
        }

        if (filehandler == null)
        {
            log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " The file you want to close isn't open!");
            return;
        }

        //3. Contact MetaServers to close
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdi.close(clientID, filehandler);

        //4. Remove from Open Files 
        fileRegister.Remove(filehandler);
        log.Info("CLIENT :: c-" + this.clientID + " OPEN" + " OPERATION SUCCESS");
        return;
    }



    /*------------------------------------------------------------------------         
     *                                   DELETE
     *-----------------------------------------------------------------------*/

    //Asynchronous calls delegates
    public delegate TransactionDTO prepareDeleteRemoteAsyncDelegate(TransactionDTO dto);
    public delegate TransactionDTO commitDeleteRemoteAsyncDelegate(TransactionDTO dto);

    private static Dictionary<string, int> deleteQUORUM;  //Key - Transaction ID ; int - number of responses

    public static void prepareDeleteRemoteAsyncCallBack(IAsyncResult ar)
    {
        prepareDeleteRemoteAsyncDelegate del = (prepareDeleteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;        
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info("CLIENT :: c-" + assyncResult.clientID + " DELETE" + " Call Back Received - PrepareDelete for transaction: " + assyncResult.transactionID);
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
        log.Info("CLIENT :: c-" + assyncResult.clientID + " DELETE" + " Call Back Received - CommitDelete for transaction: " + assyncResult.transactionID);
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
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Meta-Server to Contact: " + Utils.whichMetaServer(filename));

        
        //2. Invoke delete on meta server
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Sending create request to Meta-Server");
        FileHandler fh = mdi.delete(this.clientID, filename);
        if (fh == null) {
            log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Meta Server doesn't have a reference of the file to delete!"); 
            return;
        }
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Received the File Handler of file: " + fh.filenameGlobal);

        //3. Contact data-server to prepare
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Initiating 2PC");
        string transactionID = Utils.generateTransactionID(); 
        sendPREPAREDELETE:
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareDeleteRemoteAsyncDelegate RemoteDel = new prepareDeleteRemoteAsyncDelegate(di.prepareDelete);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareDeleteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);
            }
            catch
            {
                log.Info("O Data Server " + dataServerPort + " não faz mal, se não atingirmos o quorum tentamos de novo");
            }
                //di.prepareDelete(this.clientID, fh.dataServersFiles[dataServerPort]); SYNC
        }
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " 2PC 1st Phase - Prepare Create Assync Calls Sent");

        DateTime timeSentPrepareCreate = DateTime.Now;

        while (true)
        {

            if (DateTime.Now.Subtract(timeSentPrepareCreate).TotalSeconds > 10.0)
            {
                log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Didn't reach quorum in time, sending assyncs again");
                goto sendPREPAREDELETE;
               
            }
            System.Threading.Thread.Sleep(10); // Wait 10ms to avoid that the second server receive a commit before a prepare

            lock (deleteQUORUM)
            {
                if (deleteQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (deleteQUORUM[transactionID] >= fh.nbServers)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Reached necessary Quorum(TOTAL) of: " + fh.nbServers + " : number of machines that are prepared: " + deleteQUORUM[transactionID]);
                        deleteQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //4. Contact data-servers to commit
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
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " 2PC 2nd Phase - Commit Delete Assync Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(10); // Wair 1ms to avoid that the second server receive a commit before a prepare

            lock (deleteQUORUM)
            {
                if (deleteQUORUM.ContainsKey(transactionID)) //Write Quorum is the same as Create on filehandler
                {
                    if (deleteQUORUM[transactionID] >= fh.nbServers)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Reached necessary Quorum(TOTAL) of: " + fh.nbServers + " : number of machines that are prepared to commit: " + deleteQUORUM[transactionID]);
                        deleteQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //5. Tell metaserver to confirm deletion
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdiConfirm.confirmDelete(this.clientID, fh, true);
        log.Info("CLIENT :: c-" + this.clientID + " DELETE" + " Confirmation Sent to Meta-Data Server, operation complete");
        return;
    }


    /*------------------------------------------------------------------------         
     *                                   WRITE
     *-----------------------------------------------------------------------*/

    // Quorun processing dictionaries
    private static Dictionary<string, int> writeQUORUM;  //Key - Transaction ID ; int - number of responses

    // Delegates
    public delegate TransactionDTO prepareWriteRemoteAsyncDelegate(TransactionDTO dto);
    public delegate TransactionDTO commitWriteRemoteAsyncDelegate(TransactionDTO dto);


    // Callbacks
    public static void prepareWriteRemoteAsyncCallBack(IAsyncResult ar)
    {    
        prepareWriteRemoteAsyncDelegate del = (prepareWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info("CLIENT :: c-" + assyncResult.clientID + " WRITE" + " Call Back Received - PrepareWrite for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (writeQUORUM.ContainsKey(assyncResult.transactionID)) { writeQUORUM[assyncResult.transactionID]++; }
            else { writeQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    public static void commitWriteRemoteAsyncCallBack(IAsyncResult ar)
    {
        commitWriteRemoteAsyncDelegate del = (commitWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info("CLIENT :: c-" + assyncResult.clientID + " WRITE" + " Call Back Received - CommitWrite for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (writeQUORUM.ContainsKey(assyncResult.transactionID)) { writeQUORUM[assyncResult.transactionID]++; }
            else { writeQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    // Remote Methods
    public void write(int fileRegisterIndex, int byteArrayRegisterIndex, Byte[] byteArray)
    {
        FileHandler fh = null;

        // 1.Find if this client has this file opened
        if (fileRegister.Count == 0 || fileRegisterIndex > fileRegister.Count || fileRegister[fileRegisterIndex] == null)
        {
            log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " File: with register " + fileRegisterIndex + " is not open yet");
            return;
        }
        fh = fileRegister[fileRegisterIndex];
        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " File: " + fileRegister[fileRegisterIndex].filenameGlobal + " is  open");
        

        //2. Find out which Meta-Server to Call 
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(fh.filenameGlobal, metaServerPorts);
        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + "Meta-Server to Contact: " + Utils.whichMetaServer(fh.filenameGlobal));


        //3. Contact metaserver for write operation & obtain the the most updated fileHandler (just because of versioning)
        fh = mdi.write(this.clientID, fh);
        if (fh == null)
        {
            log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Meta-Server didn't allow Write on File: " + Utils.whichMetaServer(fh.filenameGlobal));
            return;
        }

        // Update the version
        fh.version = fh.version + 1;

        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " The version before prepare: " + fh.version);


        //4. Contact Data-Servers to Prepare
        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Iniciating 2PC for File: " + Utils.whichMetaServer(fh.filenameGlobal));
        string transactionID = Utils.generateTransactionID();
        sendPREPAREWRITE:
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareWriteRemoteAsyncDelegate RemoteDel = new prepareWriteRemoteAsyncDelegate(di.prepareWrite);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareWriteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);    
            prepareDTO.version = fh.version;
            prepareDTO.filecontent = byteArray;
            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);
            }
            catch
            {
                log.Info("O Data Server " + dataServerPort + " não faz mal, se não atingirmos o quorum tentamos de novo");
            }
            //di.prepareWrite(this.clientID, fh.dataServersFiles[dataServerPort], byteArray); //SYNC
        }
        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " 2PC 1st Phase Async Calls Sent");

        DateTime timeSentPrepareCreate = DateTime.Now;

        while (true)
        {
            if (DateTime.Now.Subtract(timeSentPrepareCreate).TotalSeconds > 10.0)
            {
                log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Didn't reach quorum in time, sending assyncs again");
                goto sendPREPAREWRITE;
            }
            System.Threading.Thread.Sleep(10); // Wait 10s to avoid that the second server receive a commit before a prepare

            lock (writeQUORUM)
            {
                if (writeQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (writeQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Reached necessary Quorum(TOTAL) of: " + fh.writeQuorum + " : number of machines that are prepared: " + writeQUORUM[transactionID]);
                        writeQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //5. Contact Data-Servers to Commit
        transactionID = Utils.generateTransactionID();
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            commitWriteRemoteAsyncDelegate RemoteDel = new commitWriteRemoteAsyncDelegate(di.commitWrite);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.commitWriteRemoteAsyncCallBack);
            TransactionDTO commitDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            commitDTO.version = fh.version;
            IAsyncResult RemAr = RemoteDel.BeginInvoke(commitDTO, RemoteCallback, null);    
            //di.commitWrite(this.clientID, fh.dataServersFiles[dataServerPort]); //SYNC
        }

        while (true)
        {
            System.Threading.Thread.Sleep(10); // Wait 1s to avoid that the second server receive a commit before a prepare
            lock (writeQUORUM)
            {
                if (writeQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (writeQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Reached necessary Quorum(TOTAL) of: " + fh.writeQuorum + " : number of machines that finished commit: " + writeQUORUM[transactionID]);
                        writeQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //6. Updates File Size 
        fh.fileSize = byteArray.LongLength;

        //7. Tell Meta-Data Server to Confirm Creation 
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(fh.filenameGlobal, metaServerPorts);
        mdi.confirmWrite(this.clientID, fh, true);
        
        //Update client file handler and save the content in ByteArrayRegister
        fh.isLocked = false; //update the file handler on client side 
        fileRegister[fileRegisterIndex] = fh;
        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Version after commit: " + fh.version);

        byteArrayRegisterOLD[byteArrayRegisterIndex] = byteArray;

        log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " Operation Success on File: " + Utils.whichMetaServer(fh.filenameGlobal));
        return;
    }


    /* To be used for reference of byteArrayRegister */
    public void write(int fileRegisterIndex, int byteArrayRegisterIndex)
    {
        write(fileRegisterIndex, byteArrayRegisterIndex, byteArrayRegisterOLD[byteArrayRegisterIndex]);
    }

    /* To be used without reference of byteArrayRegister */
    public void write(int fileRegisterIndex, Byte[] byteArray)
    {
        int byteArrayRegisterIndex = nextFreeByteArrayRegister();
        if (byteArrayRegisterIndex == -1)
        {
            log.Info("CLIENT :: c-" + this.clientID + " WRITE" + " All ByteArrayRegisters are full, won't do the write");
            return;
        }
        write(fileRegisterIndex, byteArrayRegisterIndex, byteArray);
    }


    /*------------------------------------------------------------------------         
     *                                   READ
     *-----------------------------------------------------------------------*/

    // Delegates
    public delegate TransactionDTO readRemoteAsyncDelegate(TransactionDTO dto);

    private static Dictionary<string, List<TransactionDTO>> readQUORUM; //String - Transaction ID ; TransactionDTO

    public static void ReadRemoteAsyncCallBack(IAsyncResult ar)
    {
        readRemoteAsyncDelegate del = (readRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info("CLIENT :: c-" + assyncResult.clientID + " READ" + " Call Back Received - " + assyncResult.transactionID + " Success? -> " + assyncResult.success);
        if (assyncResult.success) { 
            if(readQUORUM.ContainsKey(assyncResult.transactionID)){
                 readQUORUM[assyncResult.transactionID].Add(assyncResult); 
            }
            else{
                List<TransactionDTO> ltdto = new List<TransactionDTO>();
                ltdto.Add(assyncResult);
                readQUORUM.Add(assyncResult.transactionID, ltdto);
            }
        }
        return;
    }


    public void read(int fileRegisterIndex, int semantics, int byteArrayRegisterIndex) 
    {
        log.Info("CLIENT :: c-" + this.clientID + " Read" + " Semantics - " + semantics.ToString());
        
        //1.Find if this client has this file opened
        if (fileRegister.Count == 0 || fileRegisterIndex > fileRegister.Count || !isOpen(fileRegister[fileRegisterIndex].filenameGlobal))
        {
            log.Info("CLIENT :: c-" + this.clientID + " Read" + " There is no file opened with that register - " + fileRegisterIndex);
            return;
        }
        log.Info("CLIENT :: c-" + this.clientID + " Read" + " We are going to Read File - " + fileRegister[fileRegisterIndex].filenameGlobal);
        
        FileHandler fh = fileRegister[fileRegisterIndex];

        //3. Contact Data-Server to read
        string transactionID = Utils.generateTransactionID(); 
        sendREAD:
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            readRemoteAsyncDelegate RemoteDel = new readRemoteAsyncDelegate(di.read);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.ReadRemoteAsyncCallBack);
            TransactionDTO readDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            try
            {
                IAsyncResult RemAr = RemoteDel.BeginInvoke(readDTO, RemoteCallback, null);
            }
            catch
            {
                log.Info("O Data Server " + dataServerPort + " não faz mal, se não atingirmos o quorum tentamos de novo");
            }
            //content = di.read(fh.dataServersFiles[dataServerPort], semantics); //SYNC
        }
        log.Info("CLIENT :: c-" + this.clientID + " Read" + " Assync Calls Sent");

        DateTime timeSentPrepareCreate = DateTime.Now;

        byte[] content = null; //File Content
        while (true)
        {
            if (DateTime.Now.Subtract(timeSentPrepareCreate).TotalSeconds > 10.0)
            {
                log.Info("CLIENT :: c-" + this.clientID + " READ" + " Didn't reach quorum in time, sending assyncs again");
                goto sendREAD;
            }
            System.Threading.Thread.Sleep(10); // Wait 10ms to avoid that the second server receive a commit before a prepare

            lock (readQUORUM)
            {
                //log.Info("INSIDE LOCK");
                if (readQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    //log.Info("READQUORUM HAS TRANSACTION WITH TRANSATION ID CORRECT");
                    //log.Info("COUNT: " + readQUORUM[transactionID].Count());
                    //log.Info("READQUORUM expected: " + fh.readQuorum);
                    if (readQUORUM[transactionID].Count() >= fh.readQuorum)
                    {
                        log.Info("CLIENT :: c-" + this.clientID + " Read" + " Reached necessary Quorum of: " + fh.readQuorum + " : number of machines that are prepared: " + readQUORUM[transactionID].Count);

                        long higherVersion = 0L;
                        TransactionDTO bufferDTO = null;
                        foreach (TransactionDTO loopDTO in readQUORUM[transactionID])
                        {
                            if (higherVersion <= loopDTO.version)
                            {
                                bufferDTO = loopDTO;
                                higherVersion = loopDTO.version;
                            }
                        }
                        if (semantics == Constants.DEFAULT) //Gives the most recent version of the Quorum
                        {
                            content = bufferDTO.filecontent;
                            log.Info("CLIENT :: c-" + this.clientID + " Read" + " DEFAULT - Higher Version: " + higherVersion);
                            fh.version = higherVersion;
                        }
                        if (semantics == Constants.MONOTONIC) //Gives the most recent version of the Quorum, if this one is older than last read, return last read instead
                        {
                            if (higherVersion < fh.version) 
                            {
                                //Como é menor: 
                                //1. Verificar se temos já no byteArrayRegister ou se está a null  
                                //2. Se estiver a null usar o do loop(porque nunca tinha lido) 
                                //3. Se não estiver a null usar o que esta no byteArrayRegister
                                if (byteArrayRegisterOLD[byteArrayRegisterIndex] == null) { content = bufferDTO.filecontent; }
                                else { content = byteArrayRegisterOLD[byteArrayRegisterIndex]; }
                            }
                            else
                            {
                                content = bufferDTO.filecontent;  //sacar o conteúdo do loopDTO e actualizar o filehandler
                                fh.version = higherVersion;
                            }
                            log.Info("CLIENT :: c-" + this.clientID + " MONOTONIC - Version: " + higherVersion);
                        }
                        readQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }
        byteArrayRegisterOLD.Insert(byteArrayRegisterIndex, content); //update byte register

        log.Info("CLIENT :: c-" + this.clientID + " Read" + " Operation complete, file:  " + fileRegister[fileRegisterIndex].filenameGlobal + " has this content: \n\r " + System.Text.Encoding.Default.GetString(content));
        return;
    }


    public void exeScript(List<string> commands)
    {
        foreach (string line in commands)
        {
            if (line.StartsWith("#"))
                continue;

            String[] p = { " ", "\t", ", " };
            string[] parsed = line.Split(p, StringSplitOptions.None);

            switch (parsed[0])
            {
                case "CREATE": create(parsed[2], Convert.ToInt32(parsed[3]), Convert.ToInt32(parsed[4]), Convert.ToInt32(parsed[5])); break;
                case "DELETE": delete(parsed[2]); break;
                case "OPEN": open(parsed[2]); break;
                case "CLOSE": close(parsed[2]); break;
                case "READ":
                    int DEFAULT = 1;
                    int MONOTONIC = 2;
                    int semantic;
                    switch (parsed[3])
                    {
                        case "default": semantic = DEFAULT; break;
                        case "monotonic": semantic = MONOTONIC; break;
                        default: semantic = DEFAULT; break;
                    }
                    read(Convert.ToInt32(parsed[2]), semantic, Convert.ToInt32(parsed[4])); break;
                case "WRITE":
                    if (parsed[3].StartsWith("\""))
                    {
                        for (int i = 4; i < parsed.Length; i++) { parsed[3] += " " + parsed[i]; }
                        Byte[] bytes = System.Text.Encoding.UTF8.GetBytes(parsed[3]);
                        write(Convert.ToInt32(parsed[2]), bytes);
                    }
                    else { write(Convert.ToInt32(parsed[2]), Convert.ToInt32(parsed[3])); }
                    break;
            }
        }
    }


    /***************************************************************
     *                          COPY
     **************************************************************/ 

    public void copy(int reg1, int semantics, int reg2, string salt)
    {
        //look for the first available byteRegister,if all are ocupied overwrite the oldest
        int reg = nextFreeByteArrayRegister();
        if(reg == -1)
        {
            log.Info("CLIENT :: c-" + this.clientID + " COPY" + " All ByteArrayRegisters are occupied");
            return;
        }
        read(reg1, semantics, reg);

        string s = System.Text.Encoding.Default.GetString(byteArrayRegisterOLD[reg]) + salt;
        write(reg2, System.Text.Encoding.UTF8.GetBytes(s));
    }

    public void dump() {


        /*public static List<FileHandler> fileRegister;                   
        public static List<byte[]> byteArrayRegisterOLD;
        public static List<ByteArrayRecord> byteArrayRegister;*/

        System.Console.WriteLine();
        System.Console.WriteLine("_________________[CLIENT  DUMP]________________");
        System.Console.WriteLine();
        System.Console.WriteLine();

        System.Console.WriteLine("Opened files by this client:");
        System.Console.WriteLine();

        FileHandler fh;
        for (int i = 0; i < fileRegister.Count; i++)
        {
                fh = fileRegister[i];
                System.Console.WriteLine("[" + i + "]       Filename: " + fh.filenameGlobal + "    Version: " + fh.version);
        }

        System.Console.WriteLine();

        System.Console.WriteLine("Byte-Array records in this client:");
        System.Console.WriteLine();

        for (int i = 0; i < 10; i++)
        {
            if(byteArrayRegisterOLD[i] != null)
                System.Console.WriteLine("[" + i + "]    Content: " + System.Text.Encoding.Default.GetString(byteArrayRegisterOLD[i]));
            i++;
        }

        System.Console.WriteLine();
    }

}
