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
    public String filenameGlobal;
    public long fileSize;
    public int nbServers;
    public String[] dataServersPorts;
    public int readQuorum;
    public int writeQuorum;
    public long nFileAccess;
    public long version;
    public double heat;
    public Dictionary<string, string> dataServersFiles = new Dictionary<string, string>(); // store the local filename for each dataserver (dataserver, localFileName)





    /* Who has opened this File */
    public bool isOpen;
    public List<string> byWhom = new List<string>();

    /* Mutual exclusion */
    public bool isLocked = false;
    public string byWho;

    /* Constructor */
    public FileHandler(String fileName, long fileSize, int nbServers,
        String[] dataServersPorts, String[] localNames, int readQuorum, int writeQuorum, long nFileAccess)
    {
        this.filenameGlobal = fileName;
        this.fileSize = fileSize;
        this.nbServers = nbServers;
        this.dataServersPorts = dataServersPorts;
        this.readQuorum = readQuorum;
        this.writeQuorum = writeQuorum;
        this.nFileAccess = nFileAccess;

        for (int i = 0; i < nbServers; i++)
            dataServersFiles.Add(dataServersPorts[i], localNames[i]);

        isOpen = true;
    }

    /* ToString */
    public override string ToString()
    {
        string s = "Filename: " + this.filenameGlobal + "\n";
        s += "\t" + "Size - " + this.fileSize + " (bytes)\n";
        s += "\t" + "Number of Data Severs - " + this.nbServers + "\n";
        s += "\t" + "Read Quorum - " + this.readQuorum + "\n";
        s += "\t" + "Write Quorum - " + this.writeQuorum + "\n";
        s += "\t" + "Number of accesses - " + this.nFileAccess+ "\n";
        s += "\t" + "Version - " + this.version + "\n";


        s += "\t" + "DataServers where the file is open:\n";
        s += "\t\t\tDataServer\tLocal Filename\n";
        foreach (string dataServer in dataServersFiles.Keys)
            s += "\t\t\t " + dataServer + " \t " + dataServersFiles[dataServer] + "\n";


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