using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security.Cryptography;
using System.IO;

class Client
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

    static void Main()
    {
        TcpChannel channel = new TcpChannel();
        ChannelServices.RegisterChannel(channel, false);

        MyRemoteMetaDataInterface meta_obj = connectMetaServer(MS1_Address);
        MyRemoteDataInterface data_obj = connectDataServer(DS1_Address);

        //metaServerConnectivityTest(meta_obj);
        dataServerConnectivityTest(data_obj);

        //TESTING AREA

        while (true)
        {
            Console.WriteLine("[AUXILIAR    Main]:   Enter para fazer unfreeze");
            Console.ReadLine();
            data_obj.unfreeze();
        }
    }
}
