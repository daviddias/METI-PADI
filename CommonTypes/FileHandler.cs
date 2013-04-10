using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;

[Serializable]
public class FileHandler
{
    /* Atributes */
    public String fileName;
    public long fileSize;
    public int nbServers;
    public String[] dataServersPorts;
    public int readQuorum;
    public int writeQuorum;
    public long nFileAccess;

    /* Who has opened this File */
    public bool isOpen;
    public List<string> byWhom = new List<string>();

    /* Mutual exclusion */
    public bool isLocked = false;
    string byWho;

    /* Constructor */
    public FileHandler(String fileName, long fileSize, int nbServers,
        String[] dataServersPorts, int readQuorum, int writeQuorum, long nFileAccess)
    {
        this.fileName = fileName;
        this.fileSize = fileSize;
        this.nbServers = nbServers;
        this.dataServersPorts = dataServersPorts;
        this.readQuorum = readQuorum;
        this.writeQuorum = writeQuorum;
        this.nFileAccess = nFileAccess;
        isOpen = true;
    }

    /* ToString */
    public override string ToString()
    {
        string s = "Filename: " + this.fileName + "\n";
        s += "\t" + "Size - " + this.fileSize + "\n";
        s += "\t" + "Number of Data Severs - " + this.nbServers + "\n";
        s += "\t" + "Read Quorum - " + this.readQuorum + "\n";
        s += "\t" + "Write Quorum - " + this.writeQuorum + "\n";

        /*
         * Falta por o pair (dataserver, localname)
         * 
        s += "\t" + "Data where the file is open:\n";
        foreach(string data in par_dataserver_local_name)
        */

        return s;
    }
 
    /* Serializable stuff */



}










/* Example from exceptions
public class MyException : ApplicationException {
	public int campo;
	public MyRemoteInterface mo;
    
	public MyException(int c, MyRemoteInterface mo) {
		campo = c;
		this.mo = mo;
	}

	public MyException(System.Runtime.Serialization.SerializationInfo info,
		System.Runtime.Serialization.StreamingContext context)
		: base(info, context) {
		campo = info.GetInt32("campo");
		mo = (MyRemoteInterface)info.GetValue("mo", typeof(MyRemoteInterface));
	}

	public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) {
		base.GetObjectData(info, context);
		info.AddValue("campo", campo);
		info.AddValue("mo", mo);
	}
*/