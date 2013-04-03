using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security.Cryptography;
using System.IO;

class Client : clientInterface
{
    public const string MS1_Address = "localhost:8081/MyRemoteMetaDataObjectName";
    public const string DS1_Address = "localhost:7081/MyRemoteDataObjectName";

    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;

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

    public static void metaServerConnectivityTest(MyRemoteMetaDataInterface obj)
    {
        if (obj == null)
        {
            System.Console.WriteLine("Could not locate meta server");
            Console.ReadLine();
        }

        else
            Console.WriteLine(obj.MetodoOla());
    }

    public static void dataServerConnectivityTest(MyRemoteDataInterface obj)
    {
        if (obj == null)
        {
            System.Console.WriteLine("Could not locate meta server");
            Console.ReadLine();
        }

        else
            Console.WriteLine(obj.MetodoOla());
    }

    public static ulong CalculateSHA1(string text, Encoding enc)
    {
        byte[] buffer = enc.GetBytes(text);
        SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
        ulong number = (ulong)BitConverter.ToInt64(cryptoTransformSHA1.ComputeHash(buffer), 0);
        return number % 6;
    }
    
    public static byte[] StrToByteArray(string str)
    {
        System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
        return encoding.GetBytes(str);
    }

    //usado pelo puppet-master
    public void open(string filename) { }
    public void create(string filename) { }
    public void delete(string filename) { }
    public void write(string filename, byte[] byte_array) { }

    static void Main()
    {
        TcpChannel channel = new TcpChannel();
        ChannelServices.RegisterChannel(channel, false);

        MyRemoteMetaDataInterface meta_obj = connectMetaServer(MS1_Address);
        MyRemoteDataInterface data_obj = connectDataServer(DS1_Address);

        //metaServerConnectivityTest(meta_obj);
        dataServerConnectivityTest(data_obj);

        //TESTING AREA

        string cliendID = "1";
        string filename = "ruicamacho.txt";
        byte[] content = StrToByteArray("Estou a tentar fazer um teste aqui pah!");

        data_obj.freeze();

        Console.WriteLine(data_obj.prepareCreate(cliendID, filename));
        Console.WriteLine(data_obj.commitCreate(cliendID, filename));

        Console.WriteLine(data_obj.prepareWrite(cliendID, filename, content));
        Console.WriteLine(data_obj.commitWrite(cliendID, filename));

        try
        {
            data_obj.read(filename, DEFAULT);
        }
        catch(Exception e) {
            Console.WriteLine(e.Message);
            Console.ReadLine();
            Console.WriteLine(data_obj.unfreeze());
        }
        
        Console.ReadLine();

        Console.WriteLine(data_obj.prepareDelete(cliendID, filename));
        Console.WriteLine(data_obj.commitDelete(cliendID, filename));

        Console.ReadLine();
    }
}
