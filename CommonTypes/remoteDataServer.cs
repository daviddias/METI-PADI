using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;



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
            return false;
        }

        if (isfrozen == true){
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);
            args.Add(byte_array);

            Request request = new Request(PREPAREWRITE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: PrepareWrite]    The server is frozen!");
            return false;
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
            return false;
        }

        if (isfrozen == true){
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);

            Request request = new Request(COMMITWRITE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: commitWrite]    The server is frozen!");
            return false;
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
            return bytes;
        }

        if (isfrozen == true)
        {
            List<Object> args = new List<Object>();
            args.Add(local_file_name);
            args.Add(semantic);

            Request request = new Request(READ, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: read]    The server is frozen!");
            throw new Exception("[DATA_SERVER: read]    The server is frozen!");
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
            return false;
        }

        if (isfrozen == true)
        {
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);

            Request request = new Request(PREPARECREATE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: PrepareCreate]    The server is frozen!");
            return false;
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
            return false;
        }

        if (isfrozen == true){
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);

            Request request = new Request(COMMITCREATE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: commitCreate]    The server is frozen!");
            return false;
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
            return false;
        }

        if (isfrozen == true)
        {
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);

            Request request = new Request(PREPAREDELETE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: prepareDelete]    The server is frozen!");
            return false;
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
            return false;
        }

        if (isfrozen == true)
        {
            List<Object> args = new List<Object>();
            args.Add(clientID);
            args.Add(local_file_name);

            Request request = new Request(COMMITDELETE, args);
            pendingRequests.Add(request);

            Console.WriteLine("[DATA_SERVER: commitDelete]    The server is frozen!");
            return false;
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

        Boolean success = true;

        if (isfrozen == false)
        {
            Console.WriteLine("[DATA_SERVER: unfreeze]    The server was not frozen!");
            return false;
        }

        isfrozen = false;
        Object[] arguments;

        foreach (Request request in pendingRequests)
        {
            arguments = new Object[] {null, null, null};

            int i = 0;
            foreach (Object arg in request.arguments){
                arguments[i] = arg;
                i++;
            }

            switch (request.function)
            {
                case PREPARECREATE:
                    if (!prepareCreate((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case PREPAREWRITE:
                    if (!prepareWrite((string)request.arguments[0], (string)request.arguments[1], (byte[])request.arguments[2]))
                        success = false;
                    break;
                case PREPAREDELETE:
                    if (!prepareDelete((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case COMMITCREATE:
                    if (!commitCreate((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case COMMITWRITE:
                    if (!commitWrite((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case COMMITDELETE:
                    if (!commitDelete((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case READ:
                    if(read((string)arguments[0], (int)arguments[1]) == null)
                        success = false;
                    break;
                case TRANSFERFILE:
                    if (!transferFile((string)arguments[0], (string)arguments[1]))
                        success = false;
                    break;
                case RECEIVEFILE:
                    if (!receiveFile((string)arguments[0], (byte[])arguments[1]))
                        success = false;
                    break;
                case FAIL:
                    if (!fail())
                        success = false;
                    break;
                default:
                    Console.WriteLine("[DATA_SERVER: unfreeze]    Invalid function in pendingRequests list");
                    break;
            }

        }

        return success; 
    }

    public Boolean fail() {

        if (isfrozen == true){
            List<Object> args = new List<Object>();

            Request request = new Request(FAIL, args);
            pendingRequests.Add(request);
            Console.WriteLine("[DATA_SERVER: fail]    Cannot fail while server is frozen!");
            return false;
        }

        isfailed = true;
        return true; 
    }

    public Boolean recover() {
        if (isfailed == true)
        {
            Console.WriteLine("[DATA_SERVER: recover]    The server was not failed!");
            return false;
        }
        isfailed = false;
        return true; 
    }
}