using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.NetworkInformation;

namespace PingTester
{
    public partial class Form1 : Form
    {
        // Declare a delegate used to communicate with the UI thread
        private delegate double UpdateStatusDelegate(double ping);
        private UpdateStatusDelegate updateStatusDelegate = null;

        // Declare our worker thread
        private Thread workerThread = null;

        //static variables
        private static int count = 0;
        private static string web = null;
        private static double highPing = 0;
        private static double lowPing = 0;
        private static double lossCount = 0;
        private static double pingCount = 0;
        private static List<double> pingTime = new List<double>();

        // Boolean flag used to stop the thread
        private bool stopProcess = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Initialise the delegate
            this.updateStatusDelegate = new UpdateStatusDelegate(this.UpdateStatus);
            txtProgress.Text = "www.google.co.za";
            stopProcess = true;
            listBox1.SelectedIndex = 0;
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (errorCatch() == false)
            {
                return;
            }
            count = Convert.ToInt32(listBox1.GetItemText(listBox1.SelectedItem));
            web = txtProgress.Text;
            chart1.Series[0].Points.Clear();
            highPing = 0;
            lowPing = 0;
            lossCount = 0;
            pingCount = 0;
            pingTime.Clear();

            this.stopProcess = false;
            // Initialise and start worker thread
            this.workerThread = new Thread(new ThreadStart(this.HeavyOperation));
            this.workerThread.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.stopProcess = true;
        }

        private void HeavyOperation()
        {
            int tmp = count;
            for (int i = 0; i < count; i++)
            {
                Thread.Sleep(250);
                pingCount = pingCount + 1;
                // Check if Stop button was clicked
                if (!this.stopProcess)
                {
                    double ping = pingServer(web);
                    // Show progress
                    this.Invoke(this.updateStatusDelegate, ping);
                }
                else
                {
                    // Stop heavy operation
                    this.workerThread.Abort();
                }
            }
            stopProcess = true;
            this.workerThread.Abort();
        }

        private double pingServer(string webAddress)
        {
            var ping = new Ping();

            var result = ping.Send(webAddress);

            if (result.Status == IPStatus.Success)
            {
                return result.RoundtripTime;
            }
            return -1;
                
        }

        private double UpdateStatus(double ping)
        {
            var lst = chart1.Series[0].Points.ToArray();
            if (chart1.Series[0].Points.Count >= 10)
            {
                chart1.Series[0].Points.RemoveAt(0);
            }
            chart1.Series[0].Points.AddY(ping);
            if (pingCount == 1)
            {
                lblPingLow.Text = Convert.ToString(ping);
                lowPing = ping;
                lblPingHigh.Text = Convert.ToString(ping);
                highPing = ping;
            }
            if (ping == -1)
            {
                lossCount = lossCount + 1;
            }
            if (ping < lowPing && ping > -1)
            {
                lblPingLow.Text = Convert.ToString(ping);
                lowPing = ping;
            }
            if (ping > highPing)
            {
                lblPingHigh.Text = Convert.ToString(ping);
                highPing = ping;
            }

            if (pingTime.Count >= 10)
            {
                pingTime.RemoveAt(0);
            }
            pingTime.Add(ping);
            double pingSum = pingTime.Sum();
            lblAvrgPing.Text = Convert.ToString(pingSum / pingTime.Count);
            lblPackLoss.Text = Convert.ToString(lossCount / pingCount * 100);

            return ping;
        }

        private bool errorCatch()
        {
            if (stopProcess == false)
            {
                MessageBox.Show("A Ping test is currently running. Please wait for it to complete or stop it and try again.");
                return false;
            }
            if (string.IsNullOrEmpty(listBox1.GetItemText(listBox1.SelectedIndex)))
            {
                MessageBox.Show("Please select a ping count");
                return false;
            }
            if (string.IsNullOrEmpty(txtProgress.Text))
            {
                MessageBox.Show("Please select a ping count");
                return false;
            }
            return true;
        }
        //end
    }
}
