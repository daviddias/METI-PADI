using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;


public static class Utils
{

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


}

