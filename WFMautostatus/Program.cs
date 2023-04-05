using System;
using System.Linq;
using System.Windows.Forms;

namespace WFMautostatus
{
    static class Program
    {
        public static bool silentLaunch = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0 && Array.IndexOf(args, "--silent") >= 0)
            {
                silentLaunch = true;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
