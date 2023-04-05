using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            if (args.Length > 1 && args.Contains("--silent")) silentLaunch = true;
            // \HKEY_USERS\S-1-5-21-1133526535-3252321873-4106936650-1001\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new TrayApp());
        }
    }
}
