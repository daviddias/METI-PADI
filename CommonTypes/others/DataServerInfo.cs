using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public class DataServerInfo
{
    public double MachineHeat;
    public List<FileHandler> fileHandlers = new List<FileHandler>();
    public string dataServer; //The data server port which acts as an ID

    public DataServerInfo(double MachineHeat, List<FileHandler> fileHandlers, string dataServer)
    {
        this.MachineHeat = MachineHeat;
        this.fileHandlers = fileHandlers;
        this.dataServer = dataServer;
    }

    public DataServerInfo() { }
    
}

