﻿using System;
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
using System.Windows.Forms.DataVisualization.Charting;
using System.IO;
using System.Reflection;

namespace PingTester
{
    public partial class Form1 : Form
    {
        // Declare a delegate used to communicate with the UI thread
        //private delegate Task UpdateStatusDelegate(double ping);
        private delegate Task UpdateStatusDelegate(double ping, Series series);
        private UpdateStatusDelegate updateStatusDelegate = null;

        // Declare our worker thread
        private Thread workerThread = null;
        private Thread workerThread1 = null;
        private Thread workerThread2 = null;

        //static variables
        private static int count = 0;
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
            //this.updateStatusDelegate = new UpdateStatusDelegate(this.UpdateStatus);
            this.updateStatusDelegate = new UpdateStatusDelegate(this.UpdateStatus);
            txtWeb.Text = "192.168.0.1";
            txtWeb1.Text = "192.168.1.20";
            txtWeb2.Text = "10.1.29.41";
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
            string web = txtWeb.Text;
            string web1 = txtWeb1.Text;
            string web2 = txtWeb2.Text;
            chart1.Series[0].Points.Clear();
            highPing = 0;
            lowPing = 0;
            lossCount = 0;
            pingCount = 0;
            pingTime.Clear();

            Series series = chart1.Series[0];
            Series series1 = chart1.Series[1];
            Series series2 = chart1.Series[2];
            string log = Application.StartupPath + @"\PingLogs\txtPing.txt";
            string log1 = Application.StartupPath + @"\PingLogs\txtPing1.txt";
            string log2 = Application.StartupPath + @"\PingLogs\txtPing2.txt";

            this.stopProcess = false;
            // Initialise and start worker thread
            //this.workerThread = new Thread(new ThreadStart(this.HeavyOperation));
            this.workerThread = new Thread(() => HeavyOperation(web, log, series));
            this.workerThread.Start();
            this.workerThread1 = new Thread(() => HeavyOperation(web1, log1, series1));
            this.workerThread1.Start();
            this.workerThread2 = new Thread(() => HeavyOperation(web2, log2, series2));
            this.workerThread2.Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.stopProcess = true;
        }

        private void HeavyOperation(string ip,string logTrgt,Series series)
        {
            using (StreamWriter w = File.AppendText(logTrgt))
            {
                w.WriteLine("Started Ping of " + ip);
            }

            int tmp = count;
            for (int i = 0; i < count; i++)
            {
                Thread.Sleep(250);
                pingCount = pingCount + 1;
                // Check if Stop button was clicked
                if (!this.stopProcess)
                {
                    double ping = pingServer(ip);
                    // Show progress
                    using (StreamWriter w = File.AppendText(logTrgt))
                    {
                        string time = Convert.ToString(DateTime.Now);
                        string pingT = Convert.ToString(ping);
                        if (ping == -1)
                        {
                            pingT = "Ping Failed";
                        }

                        w.WriteLine(time + " Ping(ms) " + pingT);
                    }
                    this.Invoke(this.updateStatusDelegate, ping, series);
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

        //private double UpdateStatus(double ping, Series series)
        private Task UpdateStatus(double ping, Series series)
        {
            var lst = series.Points.ToArray();
            chart1.ChartAreas[0].RecalculateAxesScale();
            if (series.Points.Count >= 10)
            {
                series.Points.RemoveAt(0);
            }
            series.Points.AddY(ping);
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

            return Task.FromResult(0);
            //return ping;
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
            if (string.IsNullOrEmpty(txtWeb.Text))
            {
                MessageBox.Show("Please select a ping count");
                return false;
            }
            return true;
        }
        //end
    }
}
