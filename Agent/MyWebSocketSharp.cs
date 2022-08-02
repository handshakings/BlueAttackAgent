
using Agent.Models;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Agent
{
    public class MyWebSocketSharp
    {
        public void Connect()
        {
            WebSocket ws = new WebSocket("ws://localhost:333");
            ws.OnMessage += WS_OnMessage;
            ws.Connect();


            ClientConData clientConData = new ClientConData
            {
                user_id = 276,
                type = "socket",
            };
            string jsonData = JsonConvert.SerializeObject(clientConData);
            ws.Send(jsonData);
        }

        private void  WS_OnMessage(object sender, MessageEventArgs e)
        {
            
        }
    }
}
