using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AgileServer
{
    public partial class Form1 : Form
    {
        public int DHCP_PID { get; set; }
        public int TFTP_PID { get; set; }
        private static string directoryPath = Application.StartupPath+@"\diagnostic";
        private static string roampath = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+@"\Agileconfig"; 
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //MessageBox.Show(roampath);
            if (!Directory.Exists(roampath))
            {
                try
                {
                    Directory.CreateDirectory(roampath);
                }
                catch (IOException err)
                {
                    MessageBox.Show(DateTime.Now.ToString()+" ERROR 01:"+err.Message);
                }
            }
            else
            {
                try
                {
                    Directory.Delete(roampath, true);
                    Directory.CreateDirectory(roampath);
                }
                catch (IOException err)
                {
                    MessageBox.Show(DateTime.Now.ToString()+" ERROR 02:"+err.Message);
                }
            }

            string logPath = Application.StartupPath+@"\log";
            DirectoryInfo dyInfo = new DirectoryInfo(logPath);
            foreach (FileInfo feInfo in dyInfo.GetFiles())
            {
                if (feInfo.CreationTime < DateTime.Today)
                    feInfo.Delete();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("你确定要关闭吗！", "提示信息", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);    
            if (result == DialogResult.OK)    
            {    
                e.Cancel = false;  //点击OK   
            }    
            else  
            {    
                e.Cancel = true;    
            }
        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;
            fileDialog.Title = "Choose global.bin";
            fileDialog.Filter = "Global config (*.bin)|*.bin|All files (*.*)|*.*";
            if (fileDialog.ShowDialog() == DialogResult.OK)
            {
                string file = fileDialog.FileName;
                //MessageBox.Show("已选择文件:" + file, "选择文件提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                System.IO.File.Copy(file, roampath+@"\"+Path.GetFileName(file), true);
                this.textBox1.Text = file;
            }
            this.richTextBox1.AppendText("Step1: Choose global config file.\n");
        }

        private void button6_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("notepad.exe", "OpenDHCPServer.ini");
            }
            catch (IOException err)
            {
                MessageBox.Show("ERROR 03:" + err.Message);
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", roampath);
            }
            catch (IOException err)
            {
                MessageBox.Show("ERROR 05:" + err.Message);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.Description = "Choose mac.bin path";
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                string foldPath = dialog.SelectedPath;
                //MessageBox.Show("已选择文件夹:" + foldPath, "选择文件夹提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                if (System.IO.Directory.Exists(foldPath))
                {
                    string[] files = System.IO.Directory.GetFiles(foldPath);
                    string fileName, destFile;

                    foreach (string s in files)
                    {
                        //仅返回路径字符串的文件名及后缀
                        fileName = System.IO.Path.GetFileName(s);
                        destFile = System.IO.Path.Combine(roampath, fileName);
                        System.IO.File.Copy(s, destFile, true);
                    }
                }
                else
                {
                    MessageBox.Show("ERROR 04: Invalid path of macbin");
                }

                this.textBox2.Text = foldPath;
                this.richTextBox1.AppendText("Step2: Choose Mac bin ready.\n");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            bool isLoad = false;
            if (string.IsNullOrWhiteSpace(this.textBox1.Text))
            {
                MessageBox.Show(DateTime.Now.ToString() + " ERROR 11: Invalid path of global.bin!.");
            }
            else if (string.IsNullOrWhiteSpace(this.textBox2.Text))
            {
                MessageBox.Show(DateTime.Now.ToString() + " ERROR 11: Invalid path of MACBIN dir!.");
            }

            if (CheckPort("67"))
            {
                MessageBox.Show(DateTime.Now.ToString() + " ERROR 09: Essential port 67 was occupied now!.");
            }
            else if(CheckPort("69"))
            {
                MessageBox.Show(DateTime.Now.ToString() + " ERROR 10: Essential port 69 was occupied now!.");
            }
            else
            {
                Process dhcpproc = null;
                Process tftpproc = null;
                try
                {
                    dhcpproc = new Process();
                    dhcpproc.StartInfo.FileName = Application.StartupPath + @"\OpenDHCPServer.exe";
                    dhcpproc.StartInfo.Arguments = "-v";//this is argument
                    dhcpproc.EnableRaisingEvents = true;
                    dhcpproc.Exited += new EventHandler(Dhcp_Exited);
                    dhcpproc.StartInfo.CreateNoWindow = true;
                    dhcpproc.StartInfo.RedirectStandardOutput = true;
                    dhcpproc.StartInfo.UseShellExecute = false;
                    dhcpproc.Start();
                    dhcpproc.WaitForExit(100);
                    dhcpproc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    DHCP_PID = dhcpproc.Id;
                    tftpproc = new Process();
                    tftpproc.StartInfo.FileName = Application.StartupPath + @"\OpenTFTPServerMT.exe";
                    tftpproc.StartInfo.Arguments = "-v";//this is argument
                    tftpproc.EnableRaisingEvents = true;
                    tftpproc.Exited += new EventHandler(Dhcp_Exited);
                    tftpproc.StartInfo.CreateNoWindow = true;
                    tftpproc.StartInfo.RedirectStandardOutput = true;
                    tftpproc.StartInfo.UseShellExecute = false;
                    tftpproc.Start();
                    tftpproc.WaitForExit(100);
                    tftpproc.OutputDataReceived += new DataReceivedEventHandler(OutputHandler);
                    TFTP_PID = tftpproc.Id;

                    this.richTextBox1.AppendText("INFO: Locate service process is " + DHCP_PID + "\nINFO: File service process is" + TFTP_PID + "\n");
                    this.button5.BackColor = System.Drawing.Color.Green;
                }
                catch (Exception err)
                {
                    MessageBox.Show(DateTime.Now.ToString() + " ERROR 07：" + err.Message);
                }  
            }

            //while (true)
            //{
            //    if (Ping("192.168.0.1") && isLoad == true)
            //    {
            //        MessageBox.Show("Device config finished.");
            //        isLoad = false;
            //        this.richTextBox1.AppendText("INFO:Dut config finished!");
            //        break;
            //    }
            //    else if (!Ping("192.168.0.1"))
            //    {
            //        isLoad = true;
            //    }
            //}
            
        }

        private void button4_Click(object sender, EventArgs e)
        {         
            Process[] dhcpProcs = Process.GetProcessesByName("OpenDHCPServer");
            foreach (Process proc in dhcpProcs)
            {
                MessageBox.Show("INFO: Locate service process closed!\n");
                proc.Kill();
            }

            Process[] tftpProcs = Process.GetProcessesByName("OpenTFTPServerMT");
            foreach (Process proc in tftpProcs)
            {
                MessageBox.Show("INFO: File service process closed!\n");
                proc.Kill();
            }

            this.button5.BackColor = System.Drawing.Color.Red;
        }

        private void button5_Click(object sender, EventArgs e)
        {
        
        }


        private void Dhcp_Exited(object sender, EventArgs e)
        {
            //MessageBox.Show(DateTime.Now.ToString()+" ERROR 06: DHCP shutdown accidentally.");
        }

        private void OutputHandler(object sendingProcess, DataReceivedEventArgs outLine)
        {
            if (!String.IsNullOrEmpty(outLine.Data))
            {
                this.richTextBox1.AppendText(outLine.Data);
            }
        }

        public static bool PortInUse(int port)
        {
            bool inUse = false;
            IPGlobalProperties ipProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipEndPoints = ipProperties.GetActiveTcpListeners();

            foreach (IPEndPoint endPoint in ipEndPoints)
            {
                if (endPoint.Port == port)
                {
                    inUse = true;
                    break;                    
                }
            }

            return inUse;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            DirectoryInfo directorySelected = new DirectoryInfo(Application.StartupPath+@"\log");
            Compress(directorySelected);

            System.IO.File.Copy("Agile-config-diagnostic.gz", roampath + @"\" + Path.GetFileName("Agile-config-diagnostic.gz"), true);
        }

        public static void Compress(DirectoryInfo directorySelected)
        {
            foreach (FileInfo fileToCompress in directorySelected.GetFiles())
            {
                using (FileStream originalFileStream = fileToCompress.OpenRead())
                {
                    if ((File.GetAttributes(fileToCompress.FullName) &
                       FileAttributes.Hidden) != FileAttributes.Hidden & fileToCompress.Extension != ".gz")
                    {
                        using (FileStream compressedFileStream = File.Create("Agile-config-diagnostic.gz"))
                        {
                            using (GZipStream compressionStream = new GZipStream(compressedFileStream,
                               CompressionMode.Compress))
                            {
                                originalFileStream.CopyTo(compressionStream);

                            }
                        }
                    }

                }
            }
        }

        #region 检测端口号
        public bool CheckPort(string tempPort)
        {
            Process p = new Process();
            p.StartInfo = new ProcessStartInfo("netstat", "-an");
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string result = p.StandardOutput.ReadToEnd().ToLower();//最后都转换成小写字母
            System.Net.IPAddress[] addressList = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            List<string> ipList = new List<string>();
            ipList.Add("127.0.0.1");
            ipList.Add("0.0.0.0");
            for (int i = 0; i < addressList.Length; i++)
            {
                ipList.Add(addressList[i].ToString());
            }
            bool use = false;
            for (int i = 0; i < ipList.Count; i++)
            {
                if (result.IndexOf("tcp    " + ipList[i] + ":" + tempPort) >= 0 || result.IndexOf("udp    " + ipList[i] + ":" + tempPort) >= 0)
                {
                    use = true;
                    break;
                }
            }
            p.Close();
            return use;
        }
        #endregion

        #region 测试样机是否重启完毕

        public bool Ping(string ip)
        {
            System.Net.NetworkInformation.Ping p = new System.Net.NetworkInformation.Ping();
            System.Net.NetworkInformation.PingOptions options = new System.Net.NetworkInformation.PingOptions();
            options.DontFragment = true;
            string data = "Test Data!";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 1000; // Timeout 时间，单位：毫秒
            System.Net.NetworkInformation.PingReply reply = p.Send(ip, timeout, buffer, options);
            if (reply.Status == System.Net.NetworkInformation.IPStatus.Success)
                return true;
            else
                return false;
        }

        #endregion

        private void Form1_FormClosing_1(object sender, FormClosingEventArgs e)
        {
            DialogResult result = MessageBox.Show("Shutdown the server process now?", "Confirm information", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                e.Cancel = false;
                this.button4.PerformClick();
            }
            else
            {
                e.Cancel = true;
            }
        }
    }
}
