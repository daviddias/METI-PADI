using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using log4net;

namespace Puppet_Master
{
    static class Program
    {

        private static readonly ILog log = LogManager.GetLogger( System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {

            //log.Debug("This is a DEBUG level message. The most VERBOSE level.");
            //log.Info("Extended information, with higher importance than the Debug call");
            //log.Warn("An unexpected but recoverable situation occurred");
            //log.Error("An unexpected error occurred, an exception was thrown, or is about to be thrown", ex);
            //log.Fatal("Meltdown!", ex);


            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new puppetMasterGUI());
        }
    }
}
