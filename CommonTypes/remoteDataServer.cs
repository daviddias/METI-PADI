using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

public interface MyRemoteDataInterface
{
    //Metodos auxiliares
    string MetodoOla();
    void showFilesinMutation();

    //usado pelo cliente
    Boolean prepareWrite(string clientID, string local_file_name, Byte[] byte_array);   //DONE
    Boolean commitWrite(string clientID, string local_file_name);                       //DONE
    byte[] read(string local_file_name, int semantic);                                  //SEMI-DONE (ignora a semantica)
    TransactionDTO prepareCreate(string transactionID, string clientID, string local_file_name);                     //DONE
    TransactionDTO commitCreate(string transactionID, string clientID, string local_file_name);                      //DONE
    Boolean prepareDelete(string clientID, string local_file_name);                     //DONE
    Boolean commitDelete(string clientID, string local_file_name);                      //DONE

    //usado pelo meta-server
    Boolean transferFile(string filename, string address);                              //TODO (after checkpoint)

    //usado pelo data-server
    Boolean receiveFile(string filename, byte[] file);                                  //TODO (after checkpoint)

    //usado pelo puppet-master
    void freeze();                                                                   //DONE
    void unfreeze();                                                                 //DONE
    void fail();                                                                     //DONE
    void recover();                                                                  //DONE
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

    //Lista de Ficheiros Mutantes
    public static List<MutationListItem> mutationList;

    //Construtor
    public MyRemoteDataObject(int dataServerNumber)
    {
        mutationList = new List<MutationListItem>();

        string path = Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%\\PADI-FS\\") + System.Diagnostics.Process.GetCurrentProcess().ProcessName + "-" + dataServerNumber;

        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        
        Directory.SetCurrentDirectory(path);
        log.Info("Data Server is up!");
        log.Info(Directory.GetCurrentDirectory());
    }

    //Metodos auxiliares
    public void showFilesinMutation() {
        foreach (MutationListItem item in mutationList){
            Console.WriteLine(item.clientID + " " + item.filename + "Content: " + System.Text.Encoding.Default.GetString(item.byte_array));
        }
    }

    public override object InitializeLifetimeService()
    {
        return null;
    }

    //METODOS REMOTOS
    public string MetodoOla()
    {
        return "[DATA_SERVER]   Ola eu sou o Data Server!";
    }

    //Usado pelo cliente
    public Boolean prepareWrite(string clientID, string local_file_name, Byte[] byte_array) {

        //showFilesinMutation();

        if (isfailed == true){
            Console.WriteLine("[DATA_SERVER: prepareWrite]    The server has failed!");
            return false;
        }

        if (isfrozen == true){

            Console.WriteLine("[DATA_SERVER: PrepareWrite]    The server is frozen!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (!File.Exists(local_file_name))
        {
            Console.WriteLine("[DATA_SERVER: PrepareWrite]    The file doesn't exist!");
            return false;
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == local_file_name) != null){
            Console.WriteLine("[DATA_SERVER: PrepareWrite]    File is being used by other process!");
            return false;
        }

        MutationListItem mutationEntry = new MutationListItem(local_file_name, clientID, byte_array);
        mutationList.Add(mutationEntry);

        Console.WriteLine("[DATA_SERVER: PrepareWrite]    Success!");
        return true; 
    }

    public Boolean commitWrite(string clientID, string local_file_name) {
        log.Info("Starting commitWrite from client: " + clientID);
        if (isfailed == true){
            Console.WriteLine("[DATA_SERVER: commitWrite]    The server has failed!");
            return false;
        }

        if (isfrozen == true){

            Console.WriteLine("[DATA_SERVER: commitWrite]    The server is frozen!");
            
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == local_file_name && i.clientID == clientID);
        if (item == null){
            Console.WriteLine("[DATA_SERVER: commitWrite]    The file is not prepared!");
            return false;
        }

        mutationList.Remove(item);
        File.WriteAllBytes(item.filename, item.byte_array);
        log.Info("Done commitWrite from client: " + clientID);
        return true; 
    }

    public byte[] read(string local_file_name, int semantic)
    {

        // this method is limited to 2^32 byte files (4.2 GB)
        byte[] bytes = null;

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: read]    The server has failed!");
            return bytes;
        }

        if (isfrozen == true)
        {
            Console.WriteLine("[DATA_SERVER: read]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        FileStream fs;

        fs = File.OpenRead(local_file_name);
        bytes = new byte[fs.Length];
        fs.Read(bytes, 0, Convert.ToInt32(fs.Length));
        fs.Close();

        Console.WriteLine("[DATA_SERVER: read]    Success!");
        return bytes;
    }

    public TransactionDTO prepareCreate(string transactionID, string clientID, string local_file_name) {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The server has is on 'fail'!");
            return new TransactionDTO(transactionID, clientID, local_file_name, false);
        }

        if (isfrozen == true)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (File.Exists(local_file_name))
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The file already exist!");
            return new TransactionDTO(transactionID, clientID, local_file_name, false);
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == local_file_name) != null)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    File is being used by other process!");
            return new TransactionDTO(transactionID, clientID, local_file_name, false);
        }

        MutationListItem mutationEntry = new MutationListItem(local_file_name, clientID, null);
        mutationList.Add(mutationEntry);

        Console.WriteLine("[DATA_SERVER: PrepareCreate]    Success!");
        return new TransactionDTO(transactionID, clientID, local_file_name, true); 
    }

    public TransactionDTO commitCreate(string transactionID, string clientID, string local_file_name)
    {
        //log.Info("Processing commitCreate by: " + clientID + " for file: " + local_file_name);

        if (isfailed == true){
            log.Info("[DATA_SERVER: commitCreate]    The server has failed!");
            return new TransactionDTO(transactionID, clientID, local_file_name, false);
        }

        if (isfrozen == true){
            log.Info("[DATA_SERVER: commitCreate]    The server is frozen!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == local_file_name && i.clientID == clientID);
        if (item == null){
            log.Info("[DATA_SERVER: commitCreate]    The file is not prepared!");
            return new TransactionDTO(transactionID, clientID, local_file_name, false);
        }

        mutationList.Remove(item);
        File.Create(local_file_name).Close();
        log.Info("Complete commitCreate by: " + clientID + " for file: " + local_file_name);

        //Console.WriteLine("[DATA_SERVER: commitCreate]    Success!");
        return new TransactionDTO(transactionID, clientID, local_file_name, true);
    }

    public Boolean prepareDelete(string clientID, string local_file_name) {
        
        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: prepareDelete]    The server has failed!");
            return false;
        }

        if (isfrozen == true)
        {

            Console.WriteLine("[DATA_SERVER: prepareDelete]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (!File.Exists(local_file_name))
        {
            Console.WriteLine("[DATA_SERVER: prepareDelete]    The file doesn't exist!");
            return false;
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == local_file_name) != null)
        {
            Console.WriteLine("[DATA_SERVER: prepareDelete]    File is being used by other process!");
            return false;
        }

        MutationListItem mutationEntry = new MutationListItem(local_file_name, clientID, null);
        mutationList.Add(mutationEntry);

        Console.WriteLine("[DATA_SERVER: PrepareDelete]    Success!");
        return true; 
    }

    public Boolean commitDelete(string clientID, string local_file_name) {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: commitDelete]    The server has failed!");
            return false;
        }

        if (isfrozen == true)
        {

            Console.WriteLine("[DATA_SERVER: commitDelete]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == local_file_name && i.clientID == clientID);
        if (item == null)
        {
            Console.WriteLine("[DATA_SERVER: commitDelete]    The file is not prepared!");
            return false;
        }

        mutationList.Remove(item);
        File.GetAccessControl(local_file_name);
        File.Delete(local_file_name);

        Console.WriteLine("[DATA_SERVER: commitDelete]    Success!");
        return true; 
    }

    //usado pelo meta-server
    public Boolean transferFile(string filename, string address) { return true; }

    //usado pelo data-server
    public Boolean receiveFile(string filename, byte[] file) { return true; }

    //usado pelo puppet-master
    public void freeze() {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: freeze]    Cannot freeze during server failure!");
            return;
        }
        isfrozen = true;
        Console.WriteLine("[DATA_SERVER: freeze]    Success!");
        return; 
    }

    public void unfreeze() {

        if (isfrozen == false){
            Console.WriteLine("[DATA_SERVER: unfreeze]    The server was not frozen!");
            return;
        }

        isfrozen = false;
        if (!Monitor.IsEntered(mutationList))
            Monitor.Enter(mutationList);

        Monitor.PulseAll(mutationList);
        Monitor.Exit(mutationList);
        Console.WriteLine("[DATA_SERVER: unfreeze]    Sucess!");
        return;
    }

    public void fail() {

        if (isfrozen == true){
            Console.WriteLine("[DATA_SERVER: fail]    Cannot fail while server is frozen!");
        }

        isfailed = true;
        Console.WriteLine("[DATA_SERVER: faile]    Success!");
        return; 
    }

    public void recover() {
        if (isfailed == false)
        {
            Console.WriteLine("[DATA_SERVER: recover]    The server was not failed!");
            return;
        }
        isfailed = false;
        Console.WriteLine("[DATA_SERVER: recover]    Success!");
        return;
    }
}