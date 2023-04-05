using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WFMautostatus
{
    public class TrayApp : ApplicationContext
    {
        private NotifyIcon notifyIcon = new NotifyIcon();
        private FormApp formApp;
        private Thread t;

        public string manualStatus = "ingame", email = "", password = "";

        public TrayApp()
        {
            formApp = new FormApp(this);
            if (File.Exists("WFM.cfg"))
            {
                using (StreamReader sr = File.OpenText("WFM.cfg"))
                {
                    string s;
                    while ((s = sr.ReadLine()) != null)
                    {
                        switch (s.Split(' ')[0])
                        {
                            case "email:":
                                if (s.Split(' ')[1] != "-") email = s.Split(' ')[1];
                                break;
                            case "pass:":
                                if (s.Split(' ')[1] != "-") password = s.Split(' ')[1];
                                break;
                        }
                    }
                }
            }
            else
            {
                using (StreamWriter sw = File.CreateText("WFM.cfg"))
                {
                    sw.WriteLine("email: -\npass: -");
                }
            }

            if (email != "" && password != "")
            {
                AutoLogin(email, password);
            }
            else
            {
                Program.silentLaunch = false;
            }

            if (!Program.silentLaunch)
            {
                formApp.Show();
            }

            MenuItem inGame = new MenuItem("Online", new EventHandler(InGame));
            MenuItem invisible = new MenuItem("Invisible", new EventHandler(Invisible));
            MenuItem configMenuItem = new MenuItem("Show", new EventHandler(ShowApp));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(Exit));
            notifyIcon.Icon = Properties.Resources.AppIcon;
            notifyIcon.DoubleClick += new EventHandler(ShowApp);
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] { inGame, invisible, new MenuItem("-"), configMenuItem, exitMenuItem });
            notifyIcon.Visible = true;
            t = new Thread(new ThreadStart(WFChecker));
            t.Start();
        }

        /// <summary>
        /// Periodically checks if warframe is running then sets WFM status accordingly.
        /// </summary>
        private async void WFChecker()
        {
            while (true)
            {
                if (manualStatus == "invisible") { }
                else if (formApp.IsWarframeRunnning()) await formApp.SetWebsocketStatus("ingame");
                else await formApp.SetWebsocketStatus("invisible");
                Thread.Sleep(60000);
            }
        }

        /// <summary>
        /// Sets WFM status to "ONLINE IN GAME".
        /// </summary>
        async void InGame(object sender, EventArgs e)
        {
            await formApp.SetWebsocketStatus("ingame");
            manualStatus = "ingame";
        }

        /// <summary>
        /// Sets WFM status to "INVISIBLE".
        /// </summary>
        async void Invisible(object sender, EventArgs e)
        {
            await formApp.SetWebsocketStatus("invisible");
            manualStatus = "invisible";
        }

        /// <summary>
        /// Shows the applicaton.
        /// </summary>
        void ShowApp(object sender, EventArgs e)
        {
            if (formApp.Visible) formApp.Focus();
            else formApp.Show();
        }

        /// <summary>
        /// Call Exit from tray menu.
        /// </summary>
        void Exit(object sender, EventArgs e)
        {
            ExitApp();
        }

        /// <summary>
        /// Writes credentials to file for auto login.
        /// </summary>
        public void SaveConfig()
        {
            using (StreamWriter sw = File.CreateText("WFM.cfg"))
            {
                sw.WriteLine($"email: {(email == "" ? "-" : email)}\npass: {(password == "" ? "-" : password)}");
            }
        }

        /// <summary>
        /// Automatically tries to log in
        /// </summary>
        private async void AutoLogin(string email, string password)
        {
            try
            {
                await formApp.GetUserLogin(email, password);
            }
            catch (Exception ex)
            {
                formApp.WriteToFile("[Error] Autologin failed: " + ex.Message);
                Program.silentLaunch = false;
            }
        }

        public async void ExitApp()
        {
            t.Abort();
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            await formApp.SetWebsocketStatus("invisible");
            formApp.Disconnect();
            Application.Exit();
        }
    }
}
