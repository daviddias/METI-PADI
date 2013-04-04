using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security.Cryptography;



public interface clientInterface { 

    //usado pelo puppet-master
    void open(string filename);                         //TODO
    void create(string filename);                       //TODO
    void delete(string filename);                       //TODO
    void write(string filename, byte[] byte_array);     //TODO
}



public class remoteClient : MarshalByRefObject, clientInterface
{
    FileHandler[] openFiles;

    public const string MS1_Address = "localhost:8081/MyRemoteMetaDataObjectName";
    public const string MS2_Address = "localhost:8082/MyRemoteMetaDataObjectName";
    public const string MS3_Address = "localhost:8083/MyRemoteMetaDataObjectName";

    public const string DS1_Address = "localhost:7081/MyRemoteDataObjectName";

    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;


    //Atributos
    public string clientID;
    
    //Construtor
    public remoteClient(string ID)
    {
        openFiles = new FileHandler[10];
        clientID = ID;
        System.Console.WriteLine("Client is up!");
    }

    //Metodos auxiliares
    public static MyRemoteMetaDataInterface connectMetaServer(string address)
    {
        MyRemoteMetaDataInterface obj = (MyRemoteMetaDataInterface)Activator.GetObject(typeof(MyRemoteMetaDataInterface), "tcp://" + address);
        return obj;
    }

    public static MyRemoteDataInterface connectDataServer(string address)
    {
        MyRemoteDataInterface obj = (MyRemoteDataInterface)Activator.GetObject(typeof(MyRemoteDataInterface), "tcp://" + address);
        return obj;
    }

    public static ulong CalculateSHA1(string text, Encoding enc)
    {
        byte[] buffer = enc.GetBytes(text);
        SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
        ulong number = (ulong)BitConverter.ToInt64(cryptoTransformSHA1.ComputeHash(buffer), 0);
        return number % 6;
    }

    public string MetodoOla()
    {
        openFiles = new FileHandler[10];
        return "[CLIENT]   Ola eu sou o Client!";
    }

    public void open(string filename) {

        MyRemoteMetaDataInterface meta_obj = null;

        switch (CalculateSHA1(filename, Encoding.UTF8))
        {
            case (0):
                meta_obj = connectMetaServer(MS1_Address);
                break;
            case (1):
                meta_obj = connectMetaServer(MS1_Address);
                break;
            case (2):
                meta_obj = connectMetaServer(MS2_Address);
                break;
            case (3):
                meta_obj = connectMetaServer(MS2_Address);
                break;
            case (4):
                meta_obj = connectMetaServer(MS3_Address);
                break;
            case (5):
                meta_obj = connectMetaServer(MS3_Address);
                break;
            default:
                Console.WriteLine("[Client  open]:  Erro ao calcular o SHA-1!");
                break;

        }

        meta_obj.open(clientID, filename);

        return; 
    }
    public void create(string filename) { return; } 
    public void delete(string filename) { return; }
    public void write(string filename, byte[] byte_array) {return ;}
}
