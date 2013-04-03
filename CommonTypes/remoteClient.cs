using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;


public interface clientInterface { 

    //usado pelo puppet-master
    void open(string filename);
    void create(string filename);
    void delete(string filename);
    void write(string filename, byte[] byte_array);
}

public class remoteClient
{

}
