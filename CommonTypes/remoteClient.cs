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
using CommonTypes;


public interface remoteClientInterface { 
    //usado pelo puppet-master
    void open(string filename);                                                         //DONE
    void close(string filename);                                                        //DONE
    void create(string filename, int nbDataServers, int readQuorum, int writeQuorum);   //DONE                   
    void delete(string filename);                                                       //todo
    void write(int reg, byte[] byteArray);                                              //DONE
    void write(int reg, int byteArray);                                                 //DONE
    void read(int reg, int semantics, int byteArray);                                   //DONE
    void exeScript(List<string> commands);
    void copy(int reg1, int semantics, int reg2, string salt);
    string metodoOla(); //testing communication
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

        readQUORUM = new Dictionary<string, List<TransactionDTO>>();
        writeQUORUM = new Dictionary<string,int>();
        createQUORUM = new Dictionary<string, int>();
        deleteQUORUM = new Dictionary<string, int>();

        log.Info("Client: - " + this.clientID + " -  is up!");
    }

    /* (tune)I'm gonna live forever lalala*/
    public override object InitializeLifetimeService() { return null; }

    /* communication testing */
    public string metodoOla() { return "[CLIENT]   Ola eu sou o Client!"; }

    /* File is open */
    private Boolean isOpen(string filename)
    {
        foreach (FileHandler fh in fileRegister)
            if (fh.filenameGlobal == filename) //!Not sure if the comparison works in C# -> Check it!
                return true;
        return false;
    }

    /* Get filehandler from fileRegister */
    private FileHandler getFileHandler(string filename)
    {
        foreach (FileHandler fh in fileRegister)
            if (fh.filenameGlobal == filename)
                return fh;
        return null;
    }



    /* File is open */
    private int nextFreeByteArray()
    {
        for(int i = 0; i < byteArrayRegisterOLD.Capacity; i++)
        {
            if (byteArrayRegisterOLD.ElementAtOrDefault(i) == null)
                return i;
        }
        return -1;
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
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
   

        //2. Get File-Handle
        //---TODO--- Verificar se é possível criar (aka: se há Data-Servers suficientes para o request, caso não haja, temos de Bloquear
        log.Info(this.clientID + " CREATE ::  Sending create request to Meta-Server");
        FileHandler fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        while (fh.nbServers == 0) // if there aren't enough data servers meta server sends always nbServer = 0
        {
            System.Threading.Thread.Sleep(5000);
            fh = mdi.create(this.clientID, filename, nbDataServers, readQuorum, writeQuorum);
        }
        log.Info(this.clientID + " CREATE ::  Received the File Handler of file: " + fh.filenameGlobal);


        
        //3. Contact Data-Servers to Prepare
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

        //5. Save FileHandler
        fileRegister.Add(fh);
        

        //6. Tell Meta-Data Server to Confirm Creation
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdiConfirm.confirmCreate(this.clientID, filename, true); 
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
        if (fileRegister.Count >= Constants.MAX_FILES_OPENED) {
            Console.WriteLine("[CLIENT  open]:  Can't have 10 opened files at once!");
            return;
        }

        //3. Contact MetaServers to open
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        filehandler = mdi.open(clientID, filename);

        if (filehandler == null){
            Console.WriteLine("[CLIENT  open]:  MetaServer didn't opened the file!");
            return;
        }

        //openFiles.Add(filename, filehandler);
        fileRegister.Add(filehandler);
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
        foreach (FileHandler fh in fileRegister){
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
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdi.close(clientID, filehandler);

        //4. Remove from Open Files 
        fileRegister.Remove(filehandler);
        Console.WriteLine("[CLIENT  close]:  Success!");
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
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        log.Info(this.clientID + " DELETE ::  Meta-Server to Contact: " + Utils.whichMetaServer(filename));

        
        //2. Invoke delete on meta server
        log.Info(this.clientID + " DELETE ::  Sending create request to Meta-Server");
        FileHandler fh = mdi.delete(this.clientID, filename);
        if (fh == null) {
            log.Info(this.clientID + " DELETE :: Meta Server doesn't have a reference of the file to delete!"); 
            return;
        }
        log.Info(this.clientID + " DELETE ::  Received the File Handler of file: " + fh.filenameGlobal);

        //3. Contact data-server to prepare
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

        //5. Tell metaserver to confirm deletion
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(filename, metaServerPorts);
        mdiConfirm.confirmDelete(this.clientID, fh, true);
        log.Info(this.clientID + " DELETE ::  Confirmation Sent to Meta-Data Server, operation complete");
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
        // Alternative 2: Use the callback to get the return value
        prepareWriteRemoteAsyncDelegate del = (prepareWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " WRITE ::  Call Back Received - PrepareWrite for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (writeQUORUM.ContainsKey(assyncResult.transactionID)) { writeQUORUM[assyncResult.transactionID]++; }
            else { writeQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    public static void commitWriteRemoteAsyncCallBack(IAsyncResult ar)
    {
        // Alternative 2: Use the callback to get the return value
        commitWriteRemoteAsyncDelegate del = (commitWriteRemoteAsyncDelegate)((AsyncResult)ar).AsyncDelegate;
        TransactionDTO assyncResult = del.EndInvoke(ar);
        log.Info(assyncResult.clientID + " WRITE ::  Call Back Received - CommitWrite for transaction: " + assyncResult.transactionID);
        if (assyncResult.success)
        {
            if (writeQUORUM.ContainsKey(assyncResult.transactionID)) { writeQUORUM[assyncResult.transactionID]++; }
            else { writeQUORUM.Add(assyncResult.transactionID, 1); }
        }
        return;
    }

    // Remote Methods
    public void write(int reg, Byte[] byteArray)
    {
        FileHandler fh = null;

        // 1.Find if this client has this file opened
        if (fileRegister.Count == 0 || reg > fileRegister.Count || !isOpen(fileRegister[reg].filenameGlobal))
        {
            log.Info(this.clientID + " WRITE ::  File: with register " + reg + " is not open yet");
            return;
        }
        fh = fileRegister[reg];
        log.Info(this.clientID + " WRITE ::  File: " + fileRegister[reg].filenameGlobal + " is  open");
        
        //2. Find out which Meta-Server to Call 
        MyRemoteMetaDataInterface mdi = Utils.getMetaDataRemoteInterface(fh.filenameGlobal, metaServerPorts);
        log.Info(this.clientID + " WRITE ::  Meta-Server to Contact: " + Utils.whichMetaServer(fh.filenameGlobal));

        //3. If not available, try next one
        //TODO

        //4. Contact metaserver for write operation
        if (mdi.write(this.clientID, fh) == null)
        {
            log.Info(this.clientID + " WRITE ::  Meta-Server didn't allow Write on File: " + Utils.whichMetaServer(fh.filenameGlobal));
            return;
        }

        //5. Contact Data-Servers to Prepare
        log.Info(this.clientID + " WRITE ::  Iniciating 2PC for File: " + Utils.whichMetaServer(fh.filenameGlobal));
        string transactionID = Utils.generateTransactionID();
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            prepareWriteRemoteAsyncDelegate RemoteDel = new prepareWriteRemoteAsyncDelegate(di.prepareWrite);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.prepareWriteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            prepareDTO.filecontent = byteArray;
            IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);
            //di.prepareWrite(this.clientID, fh.dataServersFiles[dataServerPort], byteArray); //SYNC
        }
        log.Info(this.clientID + " WRITE ::  2PC 1st Phase Async Calls Sent");

        while (true)
        {
            System.Threading.Thread.Sleep(1000); // Wait 1s to avoid that the second server receive a commit before a prepare

            lock (writeQUORUM)
            {
                if (writeQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (writeQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info(this.clientID + " WRITE :: Reached necessary Quorum(TOTAL) of: " + fh.writeQuorum + " : number of machines that are prepared: " + writeQUORUM[transactionID]);
                        writeQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //6. Contact Data-Servers to Commit
        transactionID = Utils.generateTransactionID();
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            commitWriteRemoteAsyncDelegate RemoteDel = new commitWriteRemoteAsyncDelegate(di.commitWrite);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.commitWriteRemoteAsyncCallBack);
            TransactionDTO prepareDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(prepareDTO, RemoteCallback, null);    
            //di.commitWrite(this.clientID, fh.dataServersFiles[dataServerPort]); //SYNC
        }

        while (true)
        {
            System.Threading.Thread.Sleep(1000); // Wait 1s to avoid that the second server receive a commit before a prepare
            lock (writeQUORUM)
            {
                if (writeQUORUM.ContainsKey(transactionID)) //the fh.writeQuorum is the same as createQuorum
                {
                    if (writeQUORUM[transactionID] >= fh.writeQuorum)
                    {
                        log.Info(this.clientID + " WRITE :: Reached necessary Quorum(TOTAL) of: " + fh.writeQuorum + " : number of machines that finished commit: " + writeQUORUM[transactionID]);
                        writeQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }

        //7. Tell Meta-Data Server to Confirm Creation 
        MyRemoteMetaDataInterface mdiConfirm = Utils.getMetaDataRemoteInterface(fh.filenameGlobal, metaServerPorts);
        mdi.confirmWrite(this.clientID, fh, true);
        log.Info(this.clientID + " WRITE ::  Operation Success on File: " + Utils.whichMetaServer(fh.filenameGlobal));
        return;
    }

    /* To be used for reference of byteArrayRegister */
    public void write(int reg, int byteArrayRegisterIndex)
    {
        write(reg, byteArrayRegisterOLD[byteArrayRegisterIndex]);
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
        log.Info(assyncResult.clientID + " READ ::  Call Back Received - " + assyncResult.transactionID + " Success? -> " + assyncResult.success);
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
        log.Info(this.clientID + " READ :: Semantics - " + semantics.ToString());
        
        //1.Find if this client has this file opened
        if (fileRegister.Count == 0 || fileRegisterIndex > fileRegister.Count || !isOpen(fileRegister[fileRegisterIndex].filenameGlobal))
        {
            log.Info(this.clientID + " READ :: There is no file opened with that register - " + fileRegisterIndex);
            return;
        }
        log.Info(this.clientID + " READ :: We are going to Read File - " + fileRegister[fileRegisterIndex].filenameGlobal);
        
        FileHandler fh = fileRegister[fileRegisterIndex];

        //3. Contact Data-Server to read
        string transactionID = Utils.generateTransactionID(); 
        foreach (string dataServerPort in fh.dataServersPorts)
        {
            MyRemoteDataInterface di = Utils.getRemoteDataServerObj(dataServerPort);
            readRemoteAsyncDelegate RemoteDel = new readRemoteAsyncDelegate(di.read);
            AsyncCallback RemoteCallback = new AsyncCallback(remoteClient.ReadRemoteAsyncCallBack);
            TransactionDTO readDTO = new TransactionDTO(transactionID, this.clientID, fh.dataServersFiles[dataServerPort]);
            IAsyncResult RemAr = RemoteDel.BeginInvoke(readDTO, RemoteCallback, null);
            //content = di.read(fh.dataServersFiles[dataServerPort], semantics); //SYNC
        }
        log.Info(this.clientID + " READ :: READ Assync Calls Sent");


        byte[] content = null; //File Content
        while (true)
        {
            System.Threading.Thread.Sleep(1); // Wait 1ms to avoid that the second server receive a commit before a prepare

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
                        log.Info(this.clientID + " READ :: Reached necessary Quorum of: " + fh.readQuorum + " : number of machines that are prepared: " + readQUORUM[transactionID].Count);

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
                            log.Info(this.clientID + " READ ::  DEFAULT - Higher Version: " + higherVersion);
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
                                //Nota: Isto causa o problema que se tentar-mos usar um byteArrayRegister já usado, ele vai achar pode vir a achar que esse é o tal content que tem mais actualizado, corrigir isto para a versão beta
                                if (byteArrayRegisterOLD[byteArrayRegisterIndex] == null) { content = bufferDTO.filecontent; }
                                else { content = byteArrayRegisterOLD[byteArrayRegisterIndex]; }
                            }
                            else
                            {
                                content = bufferDTO.filecontent;  //sacar o conteúdo do loopDTO e actualizar o filehandler
                                fh.version = higherVersion;
                            }
                            log.Info(this.clientID + " READ ::  MONOTONIC - Version: " + higherVersion);
                        }
                        readQUORUM.Remove(transactionID);
                        break;
                    }
                }
            }
        }
        byteArrayRegisterOLD.Insert(byteArrayRegisterIndex, content); //update byte register

        log.Info(this.clientID + " READ :: Operation complete, file:  " + fileRegister[fileRegisterIndex].filenameGlobal + " has this content: \n\r " + System.Text.Encoding.Default.GetString(content));
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

    public void copy(int reg1, int semantics, int reg2, string salt)
    {
        //look for the first available byteRegister,if all are ocupied overwrite the oldest
        int reg = nextFreeByteArray();
        if(reg == -1)
        {
            log.Info(this.clientID + " ERROR: Copy :: All byteregisters ocupied");
            return;
        }
        read(reg1, semantics, reg);

        string s = System.Text.Encoding.Default.GetString(byteArrayRegisterOLD[reg]) + salt;
        write(reg2, System.Text.Encoding.UTF8.GetBytes(s));
    }
}
