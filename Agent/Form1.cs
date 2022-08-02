using Agent.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebSockets;

namespace Agent
{
    public partial class Form1 : Form
    {
        List<ClientModel> clients = new List<ClientModel>();
        public delegate void labelDelegate(string wsState);
        public delegate void textboxDelegate(string wsState);
        public void SetWsState(string wsState) => Invoke(new Action(() => label1.Text = wsState));
        public void AppendRecMsg(string msg) => Invoke(new Action(() => textBox1.Text += msg));

        public Form1()
        {
            InitializeComponent();         
        }
       
        private async void ButtonClick(object sender, EventArgs e)
        {
            await ConnectWebsocketSharp();
            return;
            Button button = (Button)sender;
            if(button.Name == button1.Name)
            {
                bool isConnected = await Connect();

                if(isConnected)
                {
                   await receiveMessage();
                }
            }
            else if (button.Name == button2.Name)
            {
                await SendMessage();
            }
            else if(button.Name == button4.Name)
            {
                await Disconnect();
            }
            
        }

        private async Task<bool> ConnectWebsocketSharp()
        {
            MyWebSocketSharp webSocketSharp = new MyWebSocketSharp();
            webSocketSharp.Connect();
            return true;
        }

        private async Task<bool> Connect()
        {
            ClientWebSocket ws = new ClientWebSocket();
            int id = GetCurrentInstance();

            clients.Add(new ClientModel
            {
                Client = ws,
                ID = id,
                MachineName = Environment.MachineName
            });
            Uri uri = new Uri("ws://localhost:333/");
            //Uri uri = new Uri("wss://demo.piesocket.com/v3/channel_1?api_key=VCXCEuvhGcBDP7XhiJJUDvR1e1D3eiVjgZ9VRiaV&notify_self");
            await ws.ConnectAsync(uri, CancellationToken.None);
            if (ws.State == WebSocketState.Open)
            {
                labelDelegate labelDelegate = new labelDelegate(SetWsState);
                labelDelegate.DynamicInvoke("Connected");
                ClientConData clientConData = new ClientConData
                {
                    user_id = 276,
                    type = "socket",
                };
                string jsonData = JsonConvert.SerializeObject(clientConData);
                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonData)), WebSocketMessageType.Text, false, CancellationToken.None);
                return true;
            }
            return false;   
        }
        private async Task SendMessage()
        {
            ClientWebSocket ws = clients.Where(x => x.ID == GetCurrentInstance()).Select(x => x.Client).FirstOrDefault();
            if (ws != null)
            {
                await Task.Run(async () =>
                {
                    if (ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            ArraySegment<byte> bytes = new ArraySegment<byte>(Encoding.Unicode.GetBytes(textBox2.Text));
                            await ws.SendAsync(bytes, WebSocketMessageType.Binary, false, CancellationToken.None);
                            Task.Delay(1000).Wait();
                        }
                        catch (Exception)
                        {
                        }
                    }
                });
            }
        }
        private async Task receiveMessage()
        {
            ClientWebSocket ws = clients.Where(x => x.ID == GetCurrentInstance()).Select(x => x.Client).FirstOrDefault();
            if(ws != null)
            {
                await Task.Run(() =>
                {
                    while (ws.State == WebSocketState.Open)
                    {
                        try
                        {
                            ArraySegment<byte> bytes = new ArraySegment<byte>(new byte[1024]);
                            WebSocketReceiveResult result = ws.ReceiveAsync(bytes, CancellationToken.None).Result;
                            string msg = Encoding.UTF8.GetString(bytes.Array, 0, result.Count);
                            textboxDelegate textboxDelegate = new textboxDelegate(AppendRecMsg);
                            textboxDelegate.DynamicInvoke(msg);
                            Task.Delay(1000).Wait();
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                    }
                });
            }
        }
        private int GetCurrentInstance()
        {
            Process current = Process.GetCurrentProcess();
            //Process[] processes = Process.GetProcesses(current.ProcessName);
            //foreach (Process process in processes)
            //{
            //    if (process.Id == current.Id)
            //    {
            //        return process.Id;
            //    }
            //}
            return current.Id;
        }

        private async Task Disconnect()
        {
            ClientWebSocket ws = clients.Where(x => x.ID == GetCurrentInstance()).Select(x => x.Client).FirstOrDefault();
            if( ws != null )
            {
                if (ws.State == WebSocketState.Open)
                {
                    ws.Abort();
                    //await ws.CloseOutputAsync(WebSocketCloseStatus.NormalClosure,"Disconnected",CancellationToken.None);
                    //if (ws.State == WebSocketState.CloseSent)
                    {
                        //await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, null, CancellationToken.None);
                        label1.Text = ws.State == WebSocketState.Aborted ? "Disconnected" : "Faild to disconnect";
                        clients.RemoveAll(x => x.ID == GetCurrentInstance());
                    }
                }
            }
            
        }

    }
}
