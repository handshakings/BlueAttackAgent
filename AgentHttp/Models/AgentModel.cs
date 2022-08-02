
namespace AgentHttp.Models
{
    public class AgentModel
    { 
        public int CurrentProcessId { get; set; }
        public string UserId { get; set; }
        
    }

    public class AgentConModel
    {
        public string ComType { get; set; }
        public string UserId { get; set; }
        public string HostName { get; set; }  
    }
    public class AgentChatModel
    {
        public string ComType { get; set; }
        public string UserId { get; set; }
        public string Message { get; set; }       
    }

    public class ServerCommands
    {
        public string UserId { get; set; }
        public string Commands { get; set; }
    }

    public class CommandsResult
    {
        public string Command { get; set; }
        public string Result { get; set; }
    }

}
