using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


[Serializable]
public class TransactionDTO
{
    public string transctionID;
    public string clientID;
    public string filenameGlobal;
    public string filenameGenerated;
    public long filesize;
    public byte[] filecontent;
    public long version;

    public Boolean success;

    public TransactionDTO(string tid, string cid, string fn, Boolean scc)
    {
        transctionID = tid;
        clientID = cid;
        filenameGlobal = fn;
        success = scc;
    }
}

