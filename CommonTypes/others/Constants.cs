using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

class Constants
{
    public const int DEFAULT = 1;
    public const int MONOTONIC = 2;
    public const int MAX_FILES_OPENED = 10;
    public const int TIMEOUT = 1000; //Call timeout
    public const double LOADBALANCER_THRESHOLD = 1.2;   //20% of total average
    public const int LOADBALANCER_CICLE_LIMIT = 30;      //load balance cicle limited to X iterations

}

