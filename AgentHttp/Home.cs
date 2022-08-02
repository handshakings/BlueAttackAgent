using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgentHttp
{
    public partial class Home : Form
    {
        string userId = null;
        string baseUrl = null;
        HttpClient client = new HttpClient();
        bool isLoggedIn = true;

        public delegate void labelDelegate(string wsState);
        public void SetWsState(string wsState)
        {
            Invoke(new Action(() =>
            {
                label1.Text = wsState;
                label1.ForeColor = wsState == "Connected" ? Color.Green : Color.Red;
            }));
        }

        public Home(string userId, string ip)
        {
            InitializeComponent();
            this.userId = userId;
            baseUrl = "https://" + ip + "/attacktesting/api/";
            
        }
        private void Home_Load(object sender, EventArgs e)
        {
            string dirUrl = @"c:\blueattacksfolder";
            if (!Directory.Exists(dirUrl))
            {
                Directory.CreateDirectory(dirUrl); 
            }  
        }
        private async void ButtonClick(object sender, EventArgs e)
        {
            Button button = (Button)sender;
            if (button.Name == button1.Name)
            {
                button.Enabled = false;
                button2.Enabled = true;
                await Connect();
            }
            else if (button.Name == button2.Name)
            {
                button.Enabled = false;
                button1.Enabled = true;
                await Disconnect();
            }
            //else if (button.Name == button3.Name)
            //{
            //    button.Enabled = false;
            //    button1.Enabled = true;
            //    await Disconnect();
            //    this.Close();               
            //}
        }

        

        private async Task Connect()
        {
            DisableSSLCertificate();
            new labelDelegate(SetWsState).DynamicInvoke("Connected");
            isLoggedIn = true;
            while (userId != "" && userId != null && userId != "error")
            {
                if(!isLoggedIn)
                {
                    return;
                }
                try
                {
                    string result = await client.GetStringAsync(baseUrl + "getcommand.php?hostname=" + Environment.MachineName + "&userid=" + userId);
                    if (result != null && result != "")
                    {
                        string[] data = result.Split(new string[] { " _X_ " }, StringSplitOptions.None);
                        int length = data.Length;
                        string networkAttackId = data[length - 1];
                        //res1 is 245 for successfule and "" for failure

                        for (int i = 0; i < length - 1; i++)
                        {
                            string path = @"C:\blueattacksfolder\" + networkAttackId + "_" + i.ToString() + "c";
                            string batPath = path + ".bat";
                            string txtPath = path + ".txt";
                            string command = data[i] + "< NUL > " + txtPath + " & type " + txtPath + "" + Environment.NewLine + "exit";
                            File.WriteAllText(batPath, command);

                            ExecuteCommands(batPath);
                            string[] lines = File.ReadAllLines(txtPath);
                            //lines = RemoveEmptyLines(lines);
                            if(lines != null)
                            {
                                foreach (string line in lines)
                                {
                                    try
                                    {
                                        if (line.Contains("is not recognized as an internal or external command") && line.Contains("to write to a nonexistent pipe"))
                                        {
                                            string res = await client.GetStringAsync(baseUrl + "prevented.php?networkattackid=" + networkAttackId + "&prevented=yes");
                                        }
                                        else
                                        {
                                            //"https://" + Serverip + "/attacktesting/api/print_cmd.php?networkattackid=" + allstring(ln - 1) + "&hostname=" + strComputerName + "&cmd=" + sOutput
                                            string res = await client.GetStringAsync(baseUrl + "print_cmd.php?networkattackid=" + networkAttackId + "&hostname=" + Environment.MachineName + "&cmd=" + line);
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        continue;
                                    }
                                }
                                new WebClient().UploadFileAsync(new Uri(baseUrl + "upload.php"), txtPath);
                                string newCommand = data[i].Replace("&&", " %26%26 ");
                                string newRes = await client.GetStringAsync(baseUrl + "sendback_cmd.php?networkattackid=" + networkAttackId + "&hostname=" + Environment.MachineName + "&cmd=" + newCommand);
                            }
                            
                        }
                    }
                }
                catch (Exception)
                {
                    continue;
                }
                Thread.Sleep(1000);
            }
        }
        private async Task Disconnect()
        {  
            //disconnect
            isLoggedIn = false;
            string res = await client.GetStringAsync(baseUrl + "update_agent.php?hostname=" + Environment.MachineName + "&userid=" + userId);
            new labelDelegate(SetWsState).DynamicInvoke("Disconnected");
        }


        private void ExecuteCommands(string batFilePath)
        {
            try
            {
                Process cmd = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo(batFilePath);
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;
                cmd.StartInfo = startInfo;
                cmd.Start();
                cmd.WaitForExit();
            }
            catch (Exception)
            {

            }
        }
        private string[] RemoveEmptyLines(string result)
        {
            string[] lines = result.Split(Environment.NewLine.ToCharArray()).Where(x => !string.IsNullOrEmpty(x)).ToArray();
            return lines.Take(lines.Length - 1).Skip(3).ToArray();
        }

        private void ReadStndardError()
        {

        }


        private void DisableSSLCertificate()
        {
            //Disable ssl certificate using delegate
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            }
            catch (Exception)
            {

            }
        }

        private async void Home_FormClosing(object sender, FormClosingEventArgs e)
        {
            //disconnect
            isLoggedIn = false;
            string res = await client.GetStringAsync(baseUrl + "update_agent.php?hostname=" + Environment.MachineName + "&userid=" + userId);
            Application.Exit();
        }
    }
}
