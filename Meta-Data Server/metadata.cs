using System;
using System.Collections;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

class MetaDataServer{
    static void Main(string[] args){
        //Meta-Data Servers - Args <MetaDataPortLocal> <MetaDataPortOtherA> <MetaDataPortOtherB> [DataServerPort] [DataServer Port] [DataServer Port]...
        //Console.WriteLine("How many args: " + args.Length);
        //Console.WriteLine("What are the args: " + args);
        string thisMetaServerPort = args[0];
        string aMetaServerPort = args[1];
        string bMetaServerPort = args[2];
        

        string[] dataServersPorts = new string[args.Length-3];
        for (int i = 0; i < (args.Length-3) ; i++)
        {
            dataServersPorts[i] = args[i + 3];
        }
        


        BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
        provider.TypeFilterLevel = TypeFilterLevel.Full;
        IDictionary props = new Hashtable();
        //props["port"] = 8081;
        props["port"] = thisMetaServerPort;
        props["name"] = thisMetaServerPort;
        
        TcpChannel channel = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channel, false);

        props["port"] = (Convert.ToInt32(thisMetaServerPort) + 2000).ToString(); // backdoor puppet master
        props["name"] = (Convert.ToInt32(thisMetaServerPort) + 2000).ToString();
        TcpChannel channelBack = new TcpChannel(props, null, provider);
        ChannelServices.RegisterChannel(channelBack, false);

        MyRemoteMetaDataObject mdo = new MyRemoteMetaDataObject(thisMetaServerPort, aMetaServerPort,bMetaServerPort,dataServersPorts);
        RemotingServices.Marshal(mdo, "MyRemoteMetaDataObjectName", typeof(MyRemoteMetaDataObject));



        System.Console.WriteLine("<enter> para sair...");
        System.Console.ReadLine();
}
}
