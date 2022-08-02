using System.Net.WebSockets;

namespace Agent.Models
{
    public class ClientModel
    {
        public int ID { get; set; }
        public string MachineName { get; set; }      
        public ClientWebSocket Client { get; set; }
    }

    public class ClientConData
    {
        public string type { get; set; }
        public int user_id { get; set; }
    }
}
