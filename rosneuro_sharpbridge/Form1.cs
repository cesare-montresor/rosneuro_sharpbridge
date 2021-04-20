
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
        private rosneuro_sharpbridge rosnode = new rosneuro_sharpbridge();

        public Form1()
        {
            InitializeComponent();
        }
        
        private void button1_Click(object sender, EventArgs e)
        {
            var addressPort = addressTxt.Text.ToString(); 
            var connected = rosnode.Connect(addressPort);
            if (connected) {
                rosnode.StartDataStreamer();
            }

            outputLbl.Text = connected ? "Connected!" : "Connection failed";
        }

    }
}
