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

        Console.WriteLine("[ c " + clientPort + " ] Running, click enter to exit");
        Console.ReadLine();
    }
}
