
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using RosSharp;
using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using std_srvs = RosSharp.RosBridgeClient.MessageTypes.Std;
using rosapi = RosSharp.RosBridgeClient.MessageTypes.Rosapi;
using System.Threading;

namespace rosneuro_sharpbridge
{
    public partial class Form1 : Form
    {
        private rosneuro_sharpbridge connector = new rosneuro_sharpbridge();

        public Form1()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            var addressPort = addressTxt.Text.ToString(); //"ws://192.168.1.37:8080";
            connector.Connect(addressPort);
            connector.StartData();

        }

    }
}
