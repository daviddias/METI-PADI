using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class HeatTableItem{

    //TODO

    public HeatTableItem(){ }

}
public class FileHandler{

    //TODO

    public FileHandler() { }

}

public interface MyRemoteMetaDataInterface{

    string MetodoOla();

    //usados pelos client
    FileHandler open(string clientID, string Filename);
    void close(string ClientID, FileHandler filehandler);
    FileHandler create(string clientID, string Filename, int NBServers, int Read_Quorun, int Write_Quorun);
    void confirmCreate(string clientID, FileHandler filehandler, Boolean created);
    FileHandler delete(string clientID, FileHandler filehandler);
    void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted);
    FileHandler write(string clientID, FileHandler filehandler);
    void confirmWrite(string clientID, FileHandler filehander, Boolean wrote);

    //usado pelo Puppet-Master
    Boolean fail();
    Boolean recover();

    //usado por outros Meta-Servers
    Boolean lockFile(string Filename);
    Boolean unlockFile(string Filename);
    void updateHeatTable(List<HeatTableItem> table);
}

public class MyRemoteMetaDataObject : MarshalByRefObject, MyRemoteMetaDataInterface{

    public MyRemoteMetaDataObject(){
        System.Console.WriteLine("MetaData Server is up!");
    }

    public override object InitializeLifetimeService()
    {
        return null;
    }

    public string MetodoOla(){
        return "[META_SERVER]   Ola eu sou o MetaData Server!";
    }

    //usados pelos client
    public FileHandler open(string clientID, string Filename)
    {
        FileHandler fh = new FileHandler();
        return fh;
    }

    public void close(string ClientID, FileHandler filehandler) { }

    public FileHandler create(string clientID, string Filename, int NBServers, int Read_Quorun, int Write_Quorun)
    {
        FileHandler fh = new FileHandler();
        return fh;
    }

    public void confirmCreate(string clientID, FileHandler filehandler, Boolean created) { }

    public FileHandler delete(string clientID, FileHandler filehandler)
    {
        FileHandler fh = new FileHandler();
        return fh;
    }

    public void confirmDelete(string clientID, FileHandler filehandler, Boolean deleted) { }

    public FileHandler write(string clientID, FileHandler filehandler)
    {
        FileHandler fh = new FileHandler();
        return fh;
    }
    public void confirmWrite(string clientID, FileHandler filehander, Boolean wrote) { }

    //usado pelo Puppet-Master
    public Boolean fail() { return true; }

    public Boolean recover() { return true; }

    //usado por outros Meta-Servers
    public Boolean lockFile(string Filename) { return true; }

    public Boolean unlockFile(string Filename) { return true; }

    public void updateHeatTable(List<HeatTableItem> table) { }
}
