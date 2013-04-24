using System;
using System.Collections;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

class DataServer
{
    static void Main(string[] args)
    {

        string dataServerPort = args[0];
        string serverNumber = args[1];
        
        BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
        provider.TypeFilterLevel = TypeFilterLevel.Full;
        IDictionary props = new Hashtable();
        
        props["port"] = dataServerPort;
        props["name"] = dataServerPort;
        TcpChannel channel = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channel, false);


        props["port"] = (Convert.ToInt32(dataServerPort) + 100).ToString(); // backdoor puppet master
        props["name"] = (Convert.ToInt32(dataServerPort) + 100).ToString();
        TcpChannel channelBack = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channelBack, false);

        MyRemoteDataObject dao = new MyRemoteDataObject(Int32.Parse(serverNumber));
        RemotingServices.Marshal(dao, "MyRemoteDataObjectName", typeof(MyRemoteDataObject));

        System.Console.WriteLine("<enter> para sair...");
        System.Console.ReadLine();
    }
}
