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
   
    static void Main(string[] args)
    {
        //Clients - Args <clientPort> <clientID> <meta0Port> <meta1Port> <meta2Port> 
        string clientPort = args[0];
        string clientID = args[1];
        string[] metaServerPorts = {args[2],args[3],args[4]};
            
        BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
        provider.TypeFilterLevel = TypeFilterLevel.Full;
        IDictionary props = new Hashtable();
        props["port"] = clientPort;

        TcpChannel channel = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channel, false);

        remoteClient rc = new remoteClient(clientID, metaServerPorts);
        RemotingServices.Marshal(rc, "RemoteClientName", typeof(remoteClient));

        //MyRemoteMetaDataInterface meta_obj = rc.connectMetaServer(MS1_Address);
        //MyRemoteDataInterface data_obj = rc.connectDataServer(DS1_Address);

        //metaServerConnectivityTest(meta_obj);
        //rc.dataServerConnectivityTest(data_obj);

        //TESTING AREA
        /*
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
        */

        Console.WriteLine("[CLIENT  Main]: Enter para sair do cliente!");
        Console.ReadLine();
    }
}
