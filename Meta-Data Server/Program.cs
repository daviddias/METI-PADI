using System;
using System.Collections;
using System.Text;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;
using System.Runtime.Serialization.Formatters;

class MetaDataServer{
static void Main(string[] args){
    BinaryServerFormatterSinkProvider provider = new BinaryServerFormatterSinkProvider();
    provider.TypeFilterLevel = TypeFilterLevel.Full;
    IDictionary props = new Hashtable();
    props["port"] = 8081;
    TcpChannel channel = new TcpChannel(props, null, provider);

    ChannelServices.RegisterChannel(channel, false);
    MyRemoteMetaDataObject mdo = new MyRemoteMetaDataObject();
    RemotingServices.Marshal(mdo, "MyRemoteMetaDataObjectName", typeof(MyRemoteMetaDataObject));

    System.Console.WriteLine("<enter> para sair...");
    System.Console.ReadLine();
}
}
