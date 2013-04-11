using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[Serializable]
public class TransactionDTO
{
    public string transactionID;
    public string clientID;
    public string filenameGlobal;
    public string filenameForDataServer;
    public long filesize;
    public byte[] filecontent;
    public long version;

    public Boolean success;

    public TransactionDTO(string transactionID, string clientID, string filenameForDataServer)
    {
        this.transactionID = transactionID;
        this.clientID = clientID;
        this.filenameForDataServer = filenameForDataServer;
    }
}

