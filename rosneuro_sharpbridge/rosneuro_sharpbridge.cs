using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


//RosSharp includes

using RosSharp.RosBridgeClient;
using std_msgs = RosSharp.RosBridgeClient.MessageTypes.Std;
using std_srvs = RosSharp.RosBridgeClient.MessageTypes.Std;
using rosapi = RosSharp.RosBridgeClient.MessageTypes.Rosapi;
using System.Threading;

namespace rosneuro_sharpbridge
{

    public delegate void StringDelegate(String t);

    public class rosneuro_sharpbridge
    {
        public static RosSocket socket;


        public string TOPIC_DATA = "data"; // read to get stream of data from the headset

        public string TOPIC_SETTINGS_SET = "settings"; // write to congiute the headset
        public string TOPIC_SETTINGS_GET = "info"; // read to know the current settings

        public string OPTION_SAMPLE_RATE  = "sample_rate";
        public string OPTION_NUM_CHANNELS = "num_channels";

        public int sample_rate;
        public int num_channels;
        public string node_name;
        
        Thread data_thread = null;
        Thread control_thread = null;

        int control_hz = 5;
        

        //advertising: write
        string get_data_topic_id = null;
        string get_nchan_topic_id = null;
        string get_srate_topic_id = null;


        //subscribing: read
        string set_nchan_topic_id = null;
        string set_srate_topic_id = null;


        // sensible defaults ?
        public rosneuro_sharpbridge(string node_name = "sharpbridge", int sample_rate = 512, int num_channels = 16)
        {
            this.node_name = node_name;
            this.sample_rate = sample_rate;
            this.num_channels = num_channels;
        }
        
        public void Connect(string uri)
        {
            if (socket == null)
            {
                socket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketSharpProtocol(uri));

                //Send data
                get_data_topic_id = socket.Advertise<std_msgs.String>(DataTopic());
                
                //Info
                get_nchan_topic_id = socket.Advertise<std_msgs.Int32>(GetNumChannelsTopic());
                get_srate_topic_id = socket.Advertise<std_msgs.Int32>(GetSampleRateTopic());

                //config
                set_nchan_topic_id = socket.Subscribe<std_msgs.Int32>(SetNumChannelsTopic(), OnNumChannelUpdate);
                set_nchan_topic_id = socket.Subscribe<std_msgs.Int32>(SetSampleRateTopic(), OnSampleRateUpdate);

                StartControl();
            }
        }

        public void OnNumChannelUpdate(std_msgs.Int32 num_channels)
        {
            this.num_channels = num_channels.data;
            OnSettingsChanged();
        }

        public void OnSampleRateUpdate(std_msgs.Int32 sample_rate)
        {
            this.sample_rate = sample_rate.data;
            OnSettingsChanged();
        }

        public void OnSettingsChanged() { 
            // adjust headset settings via sdk.
        }
        
        public void ControlLoop()
        {
            var msec_pause = 1000 / control_hz;
            var msg_int = new std_msgs.Int32();
            while (true)
            {
                // Publication:
                msg_int.data = num_channels;
                socket.Publish(get_nchan_topic_id, msg_int);

                msg_int.data = sample_rate;
                socket.Publish(get_srate_topic_id, msg_int);

                Thread.Sleep(msec_pause);
            }
        }
        
        public void DataLoop()
        {
            var msg_str = new std_msgs.String();
            var start_time = DateTime.Now;
            while (true)
            {
                msg_str.data = DateTime.Now.Subtract(start_time).TotalMilliseconds.ToString();
                socket.Publish(get_data_topic_id, msg_str);

                Thread.Sleep(1);
            }
        }

        public void StartControl()
        {
            if (control_thread == null)
            {
                control_thread = new Thread(new ThreadStart(ControlLoop));
                // control_thread.Start();
            }
        }

        public void StopControl()
        {
            if (control_thread != null)
            {
                control_thread.Abort();
            }
        }

        public void StartData()
        {
            if (data_thread == null) {
                data_thread = new Thread(new ThreadStart(DataLoop)); 
            }
        }

        public void StopData()
        {
            if (data_thread != null)
            {
                data_thread.Abort();
            }
        }
        

        public void Diconnect()
        {
            if (socket != null){
                socket.Unadvertise(get_data_topic_id);
                socket.Unadvertise(get_nchan_topic_id);
                socket.Unadvertise(get_srate_topic_id);

                socket.Unsubscribe(set_nchan_topic_id);
                socket.Unsubscribe(set_srate_topic_id);

                socket.Close();
                socket = null;
            }
        }

        public void PublishString(string topic, String text)
        {
            string id = socket.Advertise<std_msgs.String>(topic);
            std_msgs.String message = new std_msgs.String{ data = text };
            socket.Publish(id, message);
        }

        public void SubscribeString(string topic, StringDelegate handler)
        {

            string id = socket.Subscribe(topic, (std_msgs.String msg) => { handler(msg.data); });
            //OnMessageReceived.WaitOne();
            //OnMessageReceived.Reset();
            socket.Unsubscribe(id);
            Thread.Sleep(100);
        }







        #region Topic string assembly
        public string SetNumChannelsTopic()
        {
            return BuildTopic(TOPIC_SETTINGS_SET, OPTION_NUM_CHANNELS);
        }

        public string GetNumChannelsTopic()
        {
            return BuildTopic(TOPIC_SETTINGS_GET, OPTION_NUM_CHANNELS);
        }


        public string SetSampleRateTopic()
        {
            return BuildTopic(TOPIC_SETTINGS_SET, OPTION_SAMPLE_RATE);
        }

        public string GetSampleRateTopic()
        {
            return BuildTopic(TOPIC_SETTINGS_GET, OPTION_SAMPLE_RATE);
        }


        public string DataTopic()
        {
            return BuildTopic(TOPIC_DATA);
        }

        public string BuildTopic(params string[] parts)
        {
            string path = String.Join("/", parts);
            return String.Format("/{0}/{1}", node_name, path);
        }

        #endregion
    }
}
