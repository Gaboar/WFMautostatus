using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSocketSharp;

namespace WFMautostatus
{
    public partial class FormApp : Form
    {
        public TrayApp trayApp;
        public string JWT;
        private readonly WebSocket marketSocket = new WebSocket("wss://warframe.market/socket?platform=pc");
        readonly HttpClient client;

        public bool IsWarframeRunnning()
        {
            return Process.GetProcessesByName("Warframe.x64").Length == 1;
        }

        public bool IsJwtLoggedIn()
        {
            return JWT != null && JWT.Length > 300; //check if the token is of the right length
        }

        public FormApp(TrayApp trayApp)
        {
            InitializeComponent();
            this.trayApp = trayApp;
            using (StreamWriter sw = new StreamWriter(new FileStream("Log.txt", FileMode.Create, FileAccess.Write)))
            {
                sw.WriteLine("[Initialize]");
            }
            label3.Text = "";
            WebProxy proxy = null;
            String proxy_string = Environment.GetEnvironmentVariable("http_proxy");
            if (proxy_string != null)
            {
                proxy = new WebProxy(new Uri(proxy_string));
            }
            HttpClientHandler handler = new HttpClientHandler
            {
                Proxy = proxy
            };
            handler.UseCookies = false;
            client = new HttpClient(handler);

            marketSocket.SslConfiguration.EnabledSslProtocols = SslProtocols.None;
        }

        private async void button1_Click(object sender, EventArgs e) //login
        {
            try
            {
                await GetUserLogin(textBox1.Text, textBox2.Text);
            }
            catch (Exception ex)
            {
                label3.Text += $"Couldn't login: {ex.Message}";
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e) //logout
        {
            trayApp.email = "";
            trayApp.password = "";
            trayApp.SaveConfig();
            Disconnect();
        }

        private async void button3_Click(object sender, EventArgs e) //online
        {
            await SetWebsocketStatus("ingame");
            trayApp.manualStatus = "ingame";
        }

        private async void button4_Click(object sender, EventArgs e) //invisible
        {
            await SetWebsocketStatus("invisible");
            trayApp.manualStatus = "invisible";
        }

        private void FormApp_FormClosing(object sender, FormClosingEventArgs e)
        {
            trayApp.ExitApp();
        }

        private void FormApp_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
            this.Hide();
        }

        /// <summary>
        /// Get's the user's login JWT to authenticate future API calls.
        /// </summary>
        /// <param name="email">Users email</param>
        /// <param name="password">Users password</param>
        public async Task GetUserLogin(string email, string password)
        {
            textBox1.Text = "";
            textBox2.Text = "";
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri("https://api.warframe.market/v1/auth/signin"),
                Method = HttpMethod.Post,
            };
            var content = $"{{ \"email\":\"{email}\",\"password\":\"{password.Replace(@"\", @"\\")}\", \"auth_type\": \"header\"}}";
            request.Content = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            request.Headers.Add("Authorization", "JWT");
            request.Headers.Add("language", "en");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("platform", "pc");
            request.Headers.Add("auth_type", "header");
            var response = await client.SendAsync(request);
            var responseBody = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode)
            {
                SetJWT(response.Headers);
                await OpenWebSocket();
                if (checkBox1.Checked)
                {
                    trayApp.email = email;
                    trayApp.password = password;
                    trayApp.SaveConfig();
                }
                SwitchView();
            }
            else
            {
                Regex rgxEmail = new Regex("[a-zA-Z0-9]");
                string censoredEmail = rgxEmail.Replace(email, "*");
                throw new Exception("GetUserLogin, " + responseBody + $"Email: {censoredEmail}, Pw length: {password.Length}");
            }
            request.Dispose();
        }

        /// <summary>
        /// Sets the JWT to be used for future calls
        /// </summary>
        /// <param name="headers">Response headers from the original Login call</param>
        public void SetJWT(HttpResponseHeaders headers)
        {
            foreach (var item in headers)
            {
                if (!item.Key.ToLower(new System.Globalization.CultureInfo("en", false)).Contains("authorization")) continue;
                var temp = item.Value.First();
                JWT = temp.Substring(4);
                return;
            }
        }

        /// <summary>
        /// Attempts to connect the user's account to the websocket
        /// </summary>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> OpenWebSocket()
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            WriteToFile("[Debug] Connecting to websocket");

            if (marketSocket.IsAlive)
            {
                return false;
            }

            marketSocket.OnMessage += (sender, e) =>
            {
                if (e.Data.Contains("@WS/ERROR"))
                {
                    WriteToFile("[Error] Disconnected: " + e);
                    Disconnect();
                }
            };

            marketSocket.OnMessage += (sender, e) =>
            {
                if (!e.Data.Contains("SET_STATUS")) return;
                var message = JsonConvert.DeserializeObject<JObject>(e.Data);

            };

            marketSocket.OnOpen += (sender, e) =>
            {
                marketSocket.Send("{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"" + (IsWarframeRunnning() ? "ingame" : "invisible") + "\"}");
            };

            try
            {
                marketSocket.SetCookie(new WebSocketSharp.Net.Cookie("JWT", JWT));
                marketSocket.ConnectAsync();
            }
            catch (Exception e)
            {
                WriteToFile("[Error] Unable to connect: " + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets the status of WFM websocket. Will try to reconnect if it is not already connected.
        /// Accepts the following values:
        /// offline, set's player status to be hidden on the site.  
        /// online, set's player status to be online on the site.   
        /// in game, set's player status to be online and ingame on the site
        /// </summary>
        /// <param name="status"></param>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        public async Task<bool> SetWebsocketStatus(string status)
        {
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
            if (!IsJwtLoggedIn()) return false;

            var message = "{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"";
            switch (status)
            {
                case "ingame":
                case "in game":
                    message += "ingame\"}";
                    break;
                case "online":
                    message += "online\"}";
                    break;
                default:
                    message += "invisible\"}";
                    break;
            }
            try
            {
                SendMessage(message);
            }
            catch (Exception e)
            {
                WriteToFile("[Error] Unable to connect: " + e.Message);
                throw;
            }
            return true;
        }

        /// <summary>
        /// Dummy method to make it so that you log send messages
        /// </summary>
        /// <param name="data">The JSON string of data being sent over websocket</param>
        private void SendMessage(string data)
        {
            try
            {
                marketSocket.Send(data);
            }
            catch (InvalidOperationException e)
            {
                WriteToFile("[Error] Unable to send message: " + e);
            }
        }

        /// <summary>
        /// Disconnects the user from websocket and sets JWT to null
        /// </summary>
        public void Disconnect()
        {
            if (marketSocket.ReadyState == WebSocketState.Open)
            { //only send disconnect message if the socket is connected
                SendMessage("{\"type\":\"@WS/USER/SET_STATUS\",\"payload\":\"invisible\"}");
                JWT = null;
                marketSocket.Close(1006);
                SwitchView();
            }
        }

        public void SwitchView()
        {
            label1.Visible = !label1.Visible;
            label2.Visible = !label2.Visible;
            label3.Visible = !label3.Visible;
            textBox1.Visible = !textBox1.Visible;
            textBox2.Visible = !textBox2.Visible;
            button1.Visible = !button1.Visible;
            button2.Visible = !button2.Visible;
            button3.Visible = !button3.Visible;
            button4.Visible = !button4.Visible;
            checkBox1.Visible = !checkBox1.Visible;
        }

        // debug
        public void WriteToFile(string s)
        {
            using (StreamWriter sw = new StreamWriter(new FileStream("Log.txt", FileMode.Append, FileAccess.Write)))
            {
                sw.WriteLine(s);
            }
        }
    }
}
