using System;
using System.IO;
using System.Collections.Generic;
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
    Boolean prepareCreate(string clientID, string local_file_name);                     //DONE
    Boolean commitCreate(string clientID, string local_file_name);                      //DONE
    Boolean prepareDelete(string clientID, string local_file_name);                     //DONE
    Boolean commitDelete(string clientID, string local_file_name);                      //DONE

    //usado pelo meta-server
    Boolean transferFile(string filename, string address);                              //TODO (after checkpoint)

    //usado pelo data-server
    Boolean receiveFile(string filename, byte[] file);                                  //TODO (after checkpoint)

    //usado pelo puppet-master
    Boolean freeze();                                                                   //DONE
    Boolean unfreeze();                                                                 //DONE
    Boolean fail();                                                                     //DONE
    Boolean recover();                                                                  //DONE
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

public class Request {
    
    public List<Object> arguments;
    public int function;

    public Request(int func, List<Object> arg) {
        function = func;
        arguments = arg;
    }
}

public class MyRemoteDataObject : MarshalByRefObject, MyRemoteDataInterface
{
    //Semanticas
    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;

    //Mapeamento dos pedidos
    public const int PREPARECREATE = 1;
    public const int PREPAREWRITE = 2;
    public const int PREPAREDELETE = 3;
    public const int COMMITCREATE = 4;
    public const int COMMITWRITE = 5;
    public const int COMMITDELETE = 6;
    public const int READ = 7;
    public const int TRANSFERFILE = 8;
    public const int RECEIVEFILE = 9;
    public const int FAIL = 10;
    
    //Estados do servidor
    public Boolean isfailed = false;
    public Boolean isfrozen = false;

    //Lista de Ficheiros Mutantes
    public List<MutationListItem> mutationList;

    //Lista de pedidos pendentes
    List<Request> pendingRequests;

    //Construtor
    public MyRemoteDataObject()
    {
        mutationList = new List<MutationListItem>();
        pendingRequests = new List<Request>();

        System.Console.WriteLine("Data Server is up!");
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
            Console.WriteLine("[DATA_SERVER: PrepareWrite]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
            return false;
        }

        if (isfrozen == true){

            Console.WriteLine("[DATA_SERVER: PrepareWrite]    The server is frozen!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (!File.Exists(@"C:\" + local_file_name))
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
    
        if (isfailed == true){
            Console.WriteLine("[DATA_SERVER: commitWrite]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
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
        File.WriteAllBytes(@"C:\" + item.filename, item.byte_array);
        Console.WriteLine("[DATA_SERVER: commitWrite]    Success!");
        return true; 
    }

    public byte[] read(string local_file_name, int semantic)
    {

        // this method is limited to 2^32 byte files (4.2 GB)
        byte[] bytes = null;

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: read]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
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
       
        return bytes;
    }

    public Boolean prepareCreate(string clientID, string local_file_name) {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
            return false;
        }

        if (isfrozen == true)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        //Verifica a existencia do ficheiro
        if (File.Exists(@"C:\" + local_file_name))
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The file already exist!");
            return false;
        }

        //Verifica se o ficheiro ja esta a ser alterado
        if (mutationList.Find(f => f.filename == local_file_name) != null)
        {
            Console.WriteLine("[DATA_SERVER: PrepareCreate]    File is being used by other process!");
            return false;
        }

        MutationListItem mutationEntry = new MutationListItem(local_file_name, clientID, null);
        mutationList.Add(mutationEntry);

        Console.WriteLine("[DATA_SERVER: PrepareCreate]    Success!");
        return true; 
    }

    public Boolean commitCreate(string clientID, string local_file_name) {

        if (isfailed == true){
            Console.WriteLine("[DATA_SERVER: commitCreate]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
            return false;
        }

        if (isfrozen == true){

            Console.WriteLine("[DATA_SERVER: commitCreate]    The server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        MutationListItem item = mutationList.Find(i => i.filename == local_file_name && i.clientID == clientID);
        if (item == null){
            Console.WriteLine("[DATA_SERVER: commitCreate]    The file is not prepared!");
            return false;
        }

        mutationList.Remove(item);
        File.Create(@"C:\" + local_file_name).Close();

        Console.WriteLine("[DATA_SERVER: commitCreate]    Success!");
        return true; 
    }

    public Boolean prepareDelete(string clientID, string local_file_name) {
        
        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: prepareDelete]    The server has failed!");
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
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
        if (!File.Exists(@"C:\" + local_file_name))
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
            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
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
        File.GetAccessControl(@"C:\" + local_file_name);
        File.Delete(@"C:\" + local_file_name);

        Console.WriteLine("[DATA_SERVER: commitDelete]    Success!");
        return true; 
    }

    //usado pelo meta-server
    public Boolean transferFile(string filename, string address) { return true; }

    //usado pelo data-server
    public Boolean receiveFile(string filename, byte[] file) { return true; }

    //usado pelo puppet-master
    public Boolean freeze() {

        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: freeze]    Cannot freeze during server failure!");
            return false;
        }
        isfrozen = true;
        return true; 
    }

    public Boolean unfreeze() {

        if (isfrozen == false){
            Console.WriteLine("[DATA_SERVER: unfreeze]    The server was not frozen!");
            return false;
        }

        isfrozen = false;
        if (!Monitor.IsEntered(this.mutationList))
            Monitor.Enter(this.mutationList);

        Monitor.PulseAll(mutationList);
        Monitor.Exit(mutationList);
        Console.WriteLine("[DATA_SERVER: unfreeze]    Sucesso!");
        return true;
    }

    public Boolean fail() {

        if (isfrozen == true){
            Console.WriteLine("[DATA_SERVER: fail]    Cannot fail while server is frozen!");

            Monitor.Enter(mutationList);
            Monitor.Wait(mutationList);
            Monitor.Exit(mutationList);
        }

        isfailed = true;
        return true; 
    }

    public Boolean recover() {
        if (isfrozen == false)
        {
            Console.WriteLine("[DATA_SERVER: recover]    The server was not frozen!");
            return false;
        }

        isfrozen = false;
        if (!Monitor.IsEntered(this.mutationList))
            Monitor.Enter(this.mutationList);

        Monitor.PulseAll(mutationList);
        Monitor.Exit(mutationList);
        Console.WriteLine("[DATA_SERVER: recover]    Sucesso!");
        return true;
    }
}