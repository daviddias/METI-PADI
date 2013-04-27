using log4net;
using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using CommonTypes;
using System.Runtime.Serialization.Formatters;



public interface MyRemoteDataInterface
{
    //Metodos auxiliares
    string MetodoOla();
    void showFilesinMutation();

    //usado pelo cliente
    TransactionDTO prepareWrite(TransactionDTO dto);                                    //DONE
    TransactionDTO commitWrite(TransactionDTO dto);                                     //DONE
    TransactionDTO read(TransactionDTO dto);                                            //DONE
    TransactionDTO prepareCreate(TransactionDTO dto);                                   //DONE
    TransactionDTO commitCreate(TransactionDTO dto);                                    //DONE
    TransactionDTO prepareDelete(TransactionDTO dto);                                   //DONE
    TransactionDTO commitDelete(TransactionDTO dto);                                    //DONE

    //usado pelo meta-server
    TransactionDTO transferFile(TransactionDTO dto, string address);                    //DONE 

    //usado pelo data-server
    TransactionDTO receiveFile(TransactionDTO dto);                                     //DONE

    //usado pelo puppet-master
    void freeze();                                                                      //DONE
    void unfreeze();                                                                    //DONE
    void fail();                                                                        //DONE
    void recover();                                                                     //DONE
    void dump();
}

public class MutationListItem {
    public string filename;
    public string clientID;
    public byte[] byte_array;

    public MutationListItem() { }

    public MutationListItem(string name, string ID, byte[] b_array) {
        filename = name;
        clientID = ID;
        byte_array = b_array;
    }
}



public class MyRemoteDataObject : MarshalByRefObject, MyRemoteDataInterface
{
    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
   
    //Semanticas
    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;
    
    //Estados do servidor
    public static Boolean isfailed = false;
    public static Boolean isfrozen = false;

    // Momento do freeze ou fail do server para fazer os timeouts dos prepares
    public static DateTime timeOff = DateTime.Now;

    // timeout definido para fazer drop dos prepares quando o se unfreeze ou recover
    public const int TIMEOUT = 1; // em segundos


    //Lista de Ficheiros Mutantes
    public static List<MutationListItem> mutationList;
    public static Dictionary<String, int> fileAndVersion;

    // first port metaserver (hardcoded)
    public static int firstMetaserverPort = 8000;

    public static int firstDataServerPort = 9000;

    public static int myNumber;

    //Construtor
    public MyRemoteDataObject(int dataServerNumber)
    {
        mutationList = new List<MutationListItem>();
        fileAndVersion = new Dictionary<string, int>();


        string path = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%\\PADI-FS\\") + System.Diagnostics.Process.GetCurrentProcess().ProcessName + "-" + dataServerNumber;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        Directory.SetCurrentDirectory(path);
        log.Info("Data Server is up!");
        log.Info(Directory.GetCurrentDirectory());

        myNumber = dataServerNumber;
    }

    //Metodos auxiliares
    public void showFilesinMutation() {
        foreach (MutationListItem item in mutationList){
            Console.WriteLine(item.clientID + " " + item.filename + "Content: " + System.Text.Encoding.Default.GetString(item.byte_array));
        }
    }

    public override object InitializeLifetimeService() { return null;  }

    // Communication Test Method
    public string MetodoOla()  { return "[DATA_SERVER]   Ola eu sou o Data Server!";  }


    /************************************************************************
     *          
     *                           Remote Methods
     *              
     ************************************************************************/


    //Usado pelo cliente
    public TransactionDTO prepareWrite(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);
        //showFilesinMutation();

        if (isfailed == true){
            log.Info("WRITE :: PrepareWrite : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true){
            log.Info("WRITE :: PrepareWrite : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (!File.Exists(dto.filenameForDataServer))
        {
            log.Info("WRITE :: PrepareWrite : File(LocalName): " + dto.filenameForDataServer + " does not exist");
            newDTO.success = false;
            return newDTO;
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == dto.filenameForDataServer) != null){
            log.Info("WRITE :: PrepareWrite : File(LocalName): " + dto.filenameForDataServer + " is already being manipulated");
            newDTO.success = false;
            return newDTO;
        }

        MutationListItem mutationEntry = new MutationListItem(dto.filenameForDataServer, dto.clientID, dto.filecontent);
        mutationList.Add(mutationEntry);

        log.Info("WRITE :: PrepareWrite : Operation complete for File(LocalName): " + dto.filenameForDataServer);
        newDTO.success = true;
        return newDTO;
    }

    public TransactionDTO commitWrite(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);
        
        if (isfailed == true){
            log.Info("WRITE :: CommitWrite : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true){
            log.Info("WRITE :: CommitWrite : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == dto.filenameForDataServer && i.clientID == dto.clientID);
        if (item == null){
            log.Info("WRITE :: CommitWrite : Client: " + dto.clientID + " is trying to Commit the file(localname): " + dto.filenameForDataServer +" without preparing");
            newDTO.success = false;
            return newDTO;
        }

        mutationList.Remove(item);
        File.WriteAllBytes(item.filename, item.byte_array);
        fileAndVersion[dto.filenameForDataServer]++;
        log.Info("WRITE :: CommiteWrite : Operation complete from Client: " + dto.clientID + " file(localname): " + dto.filenameForDataServer + " version: " + fileAndVersion[dto.filenameForDataServer]);
        newDTO.success = true;
        return newDTO;
    }






    public TransactionDTO  read(TransactionDTO dto)
    {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);
        // this method is limited to 2^32 byte files (4.2 GB)
        byte[] bytesRead = null;

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: read]    The server has failed!");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            Console.WriteLine("[DATA_SERVER: read]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        FileStream fs;

        fs = File.OpenRead(dto.filenameForDataServer);
        bytesRead = new byte[fs.Length];
        fs.Read(bytesRead, 0, Convert.ToInt32(fs.Length));
        fs.Close();

        Console.WriteLine("[DATA_SERVER: read]    Success! : Content Read- " +  System.Text.Encoding.Default.GetString(bytesRead));
        newDTO.success = true;
        newDTO.filecontent = bytesRead;
        newDTO.version = fileAndVersion[dto.filenameForDataServer];
        return newDTO;
    }






    public TransactionDTO prepareCreate(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

        if (isfailed == true)
        {
            log.Info("CREATE :: PrepareCreate : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            log.Info("CREATE :: PrepareCreate : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (File.Exists(dto.filenameForDataServer))
        {
            log.Info("CREATE :: PrepareCreate : The file requested to create already exists");
            newDTO.success = false;
            return newDTO;
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == dto.filenameForDataServer) != null)
        {
            log.Info("CREATE :: PrepareCreate : The file requested to create is already being manipulated by other process");
            newDTO.success = false;
            return newDTO;
        }

        MutationListItem mutationEntry = new MutationListItem(dto.filenameForDataServer, dto.clientID, null);
        mutationList.Add(mutationEntry);

        log.Info("CREATE :: PrepareCreate : Operation Complete");
        newDTO.success = true;
        return newDTO;
    }

    public TransactionDTO commitCreate(TransactionDTO dto)
    {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

     
        if (isfailed == true){
            log.Info("CREATE :: CommitCreate : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true){
            log.Info("CREATE :: CommitCreate : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == dto.filenameForDataServer && i.clientID == dto.clientID);
        if (item == null){
            log.Info("CREATE :: CommitCreate : There was no request before for 'Prepare' can't fast forward");          
            newDTO.success = false;
            return newDTO;
        }

        mutationList.Remove(item);
        File.Create(dto.filenameForDataServer).Close();

        fileAndVersion.Add(dto.filenameForDataServer, 0);

        log.Info("CREATE :: CommitCreate : Operation complete by Client: " + dto.clientID + " for file: " + dto.filenameForDataServer);
        newDTO.success = true;
        return newDTO;
    }










    public TransactionDTO prepareDelete(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

        if (isfailed == true)
        {
            log.Info("DELETE :: PrepareDelete : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            log.Info("DELETE :: PrepareDelete : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        // Verifica a existencia do ficheiro
        if (!File.Exists(dto.filenameForDataServer))
        {
            log.Info("DELETE :: PrepareDelete : The requested file in this Data-Server does not exist, NAME: " + dto.filenameForDataServer);
            newDTO.success = false;
            return newDTO;
        }

        // Verifica se o ficheiro já esta a ser alterado
        if (mutationList.Find(f => f.filename == dto.filenameForDataServer) != null)
        {
            log.Info("DELETE :: PrepareDelete : The requested file in this Data-Server is being manipulated by another process");
            newDTO.success = false;
            return newDTO;
        }

        MutationListItem mutationEntry = new MutationListItem(dto.filenameForDataServer, dto.clientID, null);
        mutationList.Add(mutationEntry);

        log.Info("DELETE :: PrepareDelete : Operation Complete");
        newDTO.success = true;
        return newDTO;
    }

    public TransactionDTO commitDelete(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

        if (isfailed == true)
        {
            log.Info("DELETE :: CommitDelete : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            log.Info("DELETE :: CommitDelete : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == dto.filenameForDataServer && i.clientID == dto.clientID);
        if (item == null)
        {
            log.Info("DELETE :: CommitDelete : Client: " + dto.clientID + " is trying to commit without prepare");
            newDTO.success = false;
            return newDTO;
        }

        mutationList.Remove(item);
        File.GetAccessControl(dto.filenameForDataServer);
        File.Delete(dto.filenameForDataServer);

        fileAndVersion.Remove(dto.filenameForDataServer);

        log.Info("DELETE :: CommitDelete : Operation Complete");
        newDTO.success = true;
        return newDTO;
    }




    /*------------------------------------------------------------------------         
     *                           TRANSFER FILES
     *-----------------------------------------------------------------------*/


    public TransactionDTO transferFile(TransactionDTO dto, string address) {
        
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

        // this method is limited to 2^32 byte files (4.2 GB)
        byte[] bytesRead = null;

        if (isfailed == true)
        {
            log.Info("TRANSFER :: TransferFile : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            log.Info("TRANSFER :: transferFile : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        // Verifica a existencia do ficheiro
        if (!File.Exists(dto.filenameForDataServer))
        {
            log.Info("TRANSFER :: transferFile : The requested file in this Data-Server does not exist, NAME: " + dto.filenameForDataServer);
            newDTO.success = false;
            return newDTO;
        }

        // Verifica se o ficheiro já esta a ser alterado
        if (mutationList.Find(f => f.filename == dto.filenameForDataServer) != null)
        {
            log.Info("TRANSFER :: transferFile : The requested file in this Data-Server is being manipulated by another process");
            newDTO.success = false;
            return newDTO;
        }

        string port = Utils.getPortOfAddress(address);

        FileStream fs;

   
        fs = File.OpenRead(dto.filenameForDataServer);
        bytesRead = new byte[fs.Length];
        fs.Read(bytesRead, 0, Convert.ToInt32(fs.Length));
        fs.Close();
        log.Info("TRANSFER :: transferFile : Bytes from file to send were read successfully!");

        dto.filecontent = bytesRead;

        MyRemoteDataInterface di = Utils.getRemoteDataServerObj(port);
        newDTO.success = di.receiveFile(dto).success;
        log.Info("TRANSFER :: transferFile : Destination DataServer returned success = " + newDTO.success);

        if (!newDTO.success)
            return newDTO;
           
        File.GetAccessControl(dto.filenameForDataServer);
        File.Delete(dto.filenameForDataServer);

        log.Info("TRANSFER :: transferFile : File deleted successfuly! Operation Successful");

        return newDTO;
    }

    //usado pelo data-server
    public TransactionDTO receiveFile(TransactionDTO dto) {
        TransactionDTO newDTO = new TransactionDTO(dto.transactionID, dto.clientID, dto.filenameForDataServer);

        if (isfailed == true)
        {
            log.Info("TRANSFER :: receiveFile : This server is 'failed' can't comply with the request");
            newDTO.success = false;
            return newDTO;
        }

        if (isfrozen == true)
        {
            log.Info("TRANSFER :: receiveFile : This server is 'frozen' can't comply with the request right now");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        if (File.Exists(dto.filenameForDataServer))
        {
            MutationListItem item = mutationList.Find(i => i.filename == dto.filenameForDataServer);
            if (item == null)
            {
                log.Info("TRANSFER :: receiveFile : Another process is mutating the file! receiveFile Failed!");
                newDTO.success = false;
                return newDTO;
            }

            log.Info("TRANSFER :: receiveFile : The file exists and it's is going to be overwritten!");
            File.WriteAllBytes(dto.filenameForDataServer, dto.filecontent);
            newDTO.success = true;
            return newDTO;
        }

        if (!File.Exists(dto.filenameForDataServer))
        {
            log.Info("TRANSFER :: receiveFile : The file does not exist and it is going to be created!");
            File.Create(dto.filenameForDataServer).Close();
            File.WriteAllBytes(dto.filenameForDataServer, dto.filecontent);
            newDTO.success = true;
            return newDTO;
        }

        log.Info("TRANSFER :: receiveFile : ATENTION! Something went wrong receiving the file!");


        newDTO.success = false;
        return newDTO; 
    }







    //usado pelo puppet-master
    public void freeze() {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: freeze]    Cannot freeze during server failure!");
            return;
        }
        isfrozen = true;
        timeOff = DateTime.Now; // save actual time
        Console.WriteLine("[DATA_SERVER: freeze]    Success!");
        return; 
    }

    public void unfreeze() {

        //if (isfrozen == false)
        //{
        //    Console.WriteLine("[DATA_SERVER: unfreeze]    The server was not frozen!");
        //    return;
        //}

        if (!Monitor.IsEntered(mutationList))
            Monitor.Enter(mutationList);

        Console.Write("Passaram " + Convert.ToDouble(DateTime.Now.Subtract(timeOff).TotalSeconds.ToString()).ToString() + " segundos\n\n");

        Monitor.PulseAll(mutationList);
        Monitor.Exit(mutationList);

        System.Threading.Thread.Sleep(500);

        // drop prepares if is off for time more than timeout
        if (isfrozen && Convert.ToInt32(Convert.ToDouble(DateTime.Now.Subtract(timeOff).TotalSeconds.ToString())) > TIMEOUT)
        {
            mutationList.Clear();
            log.Info("[DATA_SERVER: unfreeze]    Mutation list was cleared due to time-out expiration! (" + Convert.ToDouble(DateTime.Now.Subtract(timeOff).TotalSeconds.ToString()).ToString() + " seconds)");
        }

        isfrozen = false;
        imAlive();

        Console.WriteLine("[DATA_SERVER: unfreeze]    Sucess!");
        return;
    }

    public void fail() {

        //1. Is MetaServer Able to Respond (Fail)
        if (isfailed)
        {
            log.Info("[DATA_SERVER: fail]    The server is on 'fail'!");
            return;
        }

        IChannel[] defaultTCPChannel = ChannelServices.RegisteredChannels;
        for (int channelCount = 0; channelCount < defaultTCPChannel.Length; channelCount++)
        {
            //Locate My registerd channel
            if (defaultTCPChannel[channelCount].ChannelName == Convert.ToString(firstDataServerPort+myNumber))
            {
                //Release(Unregister) the Channel assigned to this Instance
                ChannelServices.UnregisterChannel(defaultTCPChannel[channelCount]);
                break;
            }
        }

        

        isfailed = true;
        timeOff = DateTime.Now; // save actual time
        Console.WriteLine("[DATA_SERVER: fail]    Success!");
        return; 
    }

    public void recover() {
        //if (isfailed == false)
        //{
        //    Console.WriteLine("[DATA_SERVER: recover]    The server was not failed!");
        //    return;
        //}

        BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
        provider.TypeFilterLevel = TypeFilterLevel.Full;
        IDictionary props = new Hashtable();
        //props["port"] = 8081;
        props["port"] = Convert.ToString(firstDataServerPort + myNumber);
        props["name"] = Convert.ToString(firstDataServerPort + myNumber);
        TcpChannel channel = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channel, false);

        isfailed = false;
        imAlive();
        Console.WriteLine("[DATA_SERVER: recover]    Success!");
        return;
    }

    private void imAlive()
    {
        MyRemoteMetaDataInterface[] mdi = new MyRemoteMetaDataInterface[3];
        for(int i = 0; i < 3; i++)
            mdi[i] = Utils.getRemoteMetaDataObj((firstMetaserverPort + i).ToString());

        for (int i = 0; i < 3; i++)
        {
            UpdateRemoteAsyncDelegate RemoteUpdate = new UpdateRemoteAsyncDelegate(mdi[i].receiveAlive);
            IAsyncResult RemAr = RemoteUpdate.BeginInvoke((firstDataServerPort + myNumber).ToString(), null, null);

            log.Info(" UPDATE SENDED::  Updated metadata table sended in background to Metadata Servers");
        }
    }

    public void dump() {

        System.Console.WriteLine();
        System.Console.WriteLine("_________________[DATA SERVER  DUMP]________________");
        System.Console.WriteLine();
        System.Console.WriteLine();

        System.Console.WriteLine("Files in this Data Server:");
        System.Console.WriteLine();

        foreach (string filename in fileAndVersion.Keys) {
            System.Console.WriteLine("      Filename: " + filename + "    Version: " + fileAndVersion[filename]);
        }
        System.Console.WriteLine();

        System.Console.WriteLine("Files being used by other processes:");
        System.Console.WriteLine();

        foreach (MutationListItem item in mutationList)
        {
            System.Console.WriteLine("     Filename: " + item.filename + "  Client ID: " + item.clientID);
        }
        System.Console.WriteLine();

    }

    /* delegates */
    public delegate void UpdateRemoteAsyncDelegate(string port);
}