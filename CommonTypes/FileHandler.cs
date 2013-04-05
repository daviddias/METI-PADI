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
    bool isOpen;
    List<string> byWhom = new List<string>();

    /* Mutual exclusion */
    bool isLocked = false;
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