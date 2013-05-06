using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using log4net;

public static class Utils
{

    private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    private static Random random = new Random((int)DateTime.Now.Ticks);

    /************************************************************************
     *              Get Remote Reference to Meta Data Server 
     *              that is responsible(with failover)
     ************************************************************************/

    // O CLIENTE CHAMA SEMPRE ESTE =)
    public static MyRemoteMetaDataInterface getMetaDataRemoteInterface(string filename, string[] metaServerPorts)
    {
        int metaServerToContact = Utils.whichMetaServer(filename);
        MyRemoteMetaDataInterface mdi;
        while (true)
        {
            mdi = Utils.getRemoteMetaDataObj(metaServerPorts[metaServerToContact]);
            try 
            { 
                mdi.alive();
                break; //this one is alive
            }
            catch
            {
                metaServerToContact = metaServerToContact + 3;
                if (metaServerToContact > 5) { metaServerToContact = 0; }
                continue;
            }
        }
        log.Info("UTILS ::  Meta-Server to contact: " + Utils.whichMetaServer(filename) + " for filename: " + filename);
        return mdi;
    }


    /************************************************************************
     *              Which Meta Data Server is Responsible 
     ************************************************************************/
    public static int whichMetaServer(string filename)
    {
        Encoding enc =  Encoding.UTF8;
        byte[] buffer = enc.GetBytes(filename);
        SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
        ulong number = (ulong)BitConverter.ToInt64(cryptoTransformSHA1.ComputeHash(buffer), 0);
        return (int)(number % 6);
    }



    /************************************************************************
     *              Parse port number from given address 
     ************************************************************************/
    public static string getPortOfAddress(string address)
    {

        string[] words = address.Split(':');
        if (words.Count() != 3)
        {
            System.Console.WriteLine("[UTILS:  getPortOfAddress]    Malformed address");
            return null;
        }


        string[] port = words[2].Split('/');

        if (port.Count() != 2)
        {
            System.Console.WriteLine("[UTILS:  getPortOfAddress]    Malformed address");
            return null;
        }

        return port[0];

    }


    /************************************************************************
     *              Generate Unique Transactions ID 
     ************************************************************************/

    public static string generateTransactionID() {
        var bytes = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(bytes);
        }

        // and if you need it as a string...
        return BitConverter.ToString(bytes);

    }


    /************************************************************************
     *              Get Remote Object Reference Methods
     ************************************************************************/
    public static MyRemoteMetaDataInterface getRemoteMetaDataObj(string port)
    {
        string address =  "localhost:" + port + "/MyRemoteMetaDataObjectName";
        MyRemoteMetaDataInterface obj = (MyRemoteMetaDataInterface)Activator.GetObject(typeof(MyRemoteMetaDataInterface), "tcp://" + address);
        return obj;
    }

    public static MyRemoteDataInterface getRemoteDataServerObj(string port)
    {
        string address = "localhost:" + port + "/MyRemoteDataObjectName";
        MyRemoteDataInterface obj = (MyRemoteDataInterface)Activator.GetObject(typeof(MyRemoteDataInterface), "tcp://" + address);
        return obj;
    }

    public static remoteClientInterface getRemoteClientObj(string port)
    {
        string address = "localhost:" + port + "/RemoteClientName"; 
        remoteClientInterface obj = (remoteClientInterface)Activator.GetObject(typeof(remoteClientInterface), "tcp://" + address );
        return obj;
    }

    /************************************************************************
    *              Generate Local Name on Data Servers 
    ************************************************************************/
    public static string genLocalName(string metadataserverName)
    {
        char[] name = new char[16]; // Local names are 16 characters ASCII strings

        char[] invalidChar = System.IO.Path.GetInvalidFileNameChars();


        for (int i = 0; i < 16; i++)
        {
            if (i < 3)
                name[i] = metadataserverName[i];
            else
            {
                char c = (char)random.Next(32, 126); // the printable ASCII characters are numbered between 32 and 126

                while (Array.Exists(invalidChar, element => element == c))
                    c = (char)random.Next(32, 126);
                name[i] = c;

            }
        }

        return new string(name);
    }
}

