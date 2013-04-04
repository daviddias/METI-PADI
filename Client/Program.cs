using System;
using System.Collections;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Security.Cryptography;
using System.IO;
using System.Runtime.Serialization.Formatters;

class Client
{

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

        BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
        provider.TypeFilterLevel = TypeFilterLevel.Full;
        IDictionary props = new Hashtable();
        props["port"] = 6081;

        TcpChannel channel = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channel, false);

        remoteClient rc = new remoteClient();
        RemotingServices.Marshal(rc, "RemoteClientName", typeof(remoteClient));

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
        data_obj.freeze();
        Console.WriteLine(data_obj.commitCreate(cliendID, filename));

        
        Console.WriteLine(data_obj.prepareWrite(cliendID, filename,content));
        data_obj.freeze();
        Console.WriteLine(data_obj.commitWrite(cliendID, filename));

        data_obj.freeze();
        Console.WriteLine(System.Text.Encoding.Default.GetString(data_obj.read(@"C:\"+filename, DEFAULT)));

        Console.WriteLine(data_obj.prepareDelete(cliendID, filename));
        data_obj.freeze();
        Console.WriteLine(data_obj.commitDelete(cliendID, filename));

        Console.WriteLine("[CLIENT  Main]: Enter para sair do cliente!");
        Console.ReadLine();
    }
}
