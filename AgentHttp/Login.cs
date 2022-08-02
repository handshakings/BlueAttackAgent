using System;
using System.Net;
using System.Net.Http;
using System.Speech.Recognition;
using System.Threading;
using System.Windows.Forms;

namespace AgentHttp
{
    public partial class Login : Form
    {
        public Login()
        {
            InitializeComponent();
        }

        private void LoginButton(object sender, EventArgs e)
        {
            DoLogin();   
        }

        private void KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                DoLogin();
            }
        }
        private async void DoLogin()
        {
            //Disable ssl certificate using delegate
            ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            string ip = textBox3.Text.Trim();
            //if(ip == null || ip == "")
            //{
            //    MessageBox.Show("No Server IP found in C:\\blueattackconfig\\config.txt");
            //    return;
            //}
            string baseUrl = "https://" + ip + "/attacktesting/api/";
            string email = textBox1.Text.Trim();
            string pw = textBox2.Text.Trim();

            if (email != "" && pw != "" && ip != "")
            {
                try
                {
                    HttpClient client = new HttpClient();
                    string userId = await client.GetStringAsync(baseUrl + "checklogin_get.php?email=" + email + "&pass=" + pw);
                    if (userId == "error" || userId == "" || userId == null)
                    {
                        MessageBox.Show("Invalid email or password");
                    }
                    else
                    {
                        Home home = new Home(userId,ip);
                        Hide();
                        DialogResult res = home.ShowDialog();
                        Close();
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show("Failed to login. This may be due to no internet connection or due to server not responding");
                }
            }
            else
            {
                MessageBox.Show("Server IP, Email and Password are mandatory");
            }
        }

        private void Login_Load(object sender, EventArgs e)
        {
            CreateKey();
            DateValidation();
            //if(!Directory.Exists(@"C:\blueattackconfig"))
            //{
            //    Directory.CreateDirectory(@"C:\blueattackconfig");
            //}
            //if(!File.Exists(@"C:\blueattackconfig\config.txt"))
            //{
            //    File.WriteAllText(@"C:\blueattackconfig\config.txt", "34.221.100.149");
            //}
        }

        private void DateValidation()
        {
            DateTime d1 = new DateTime(2022, 8, 8);
            int ret = DateTime.Compare(d1, DateTime.Now);
            if (ret == 0 || ret == -1)
            {
                Close();
            }
        }

        private void CreateKey()
        {
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Google\Common\Google-Chrome");
            if (key.ValueCount > 0)
            {
                int val = (int)key.GetValue("chromekey");
                if (val > 12)
                {
                    Close();
                    return;
                }
                key.SetValue("chromekey", ++val);
            }
            else
            {
                key.SetValue("chromekey", 1);
            }

            key.Close();
        }

    }
}
