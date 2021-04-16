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

    public class SharedConfig {

        public rosneuro_sharpbridge node;

        public string node_name;
        public int controller_hz;
        public int data_streamer_hz;

        // ####################
        // MUTEX ON: SocketLock
        // ####################
        public readonly object SocketLock = new object();
        public RosSocket socket;
        // TOPICS
        public string get_data_topic_id; // data
        public string get_nchan_topic_id; // num_channels
        public string set_nchan_topic_id;
        public string get_srate_topic_id; // sample_rate
        public string set_srate_topic_id;





        // ####################
        // MUTEX ON: ConfigLock
        // ####################
        public readonly object ConfigLock = new object();
        public bool controller_initialized = false;
        public bool headset_initialized = false;
        public int sample_rate;
        public int num_channels;
        public bool stream_data;
        
        // Controller
        public WorkerController controller_worker;
        public Thread controller_thread;
    
        // Data Streamer
        public WorkerDataStreamer data_worker;
        public Thread data_thread;


      
    }

    public class rosneuro_sharpbridge
    {
        public string TOPIC_DATA = "data"; // read to get stream of data from the headset

        public string TOPIC_SETTINGS_SET = "settings"; // write to congiute the headset
        public string TOPIC_SETTINGS_GET = "info"; // read to know the current settings

        public string OPTION_SAMPLE_RATE = "sample_rate";
        public string OPTION_NUM_CHANNELS = "num_channels";
        public string OPTION_STREAMING_DATA = "steaming_data";


        public SharedConfig config = new SharedConfig();


        // sensible defaults ?
        public rosneuro_sharpbridge(string node_name = "sharpbridge", int sample_rate = 512, int num_channels = 16)
        {
            config.node = this;
            config.node_name = node_name;
            config.sample_rate = sample_rate;
            config.num_channels = num_channels;

            config.controller_hz = 10;
            config.data_streamer_hz = 0;

            config.controller_worker = new WorkerController(config);
            config.data_worker = new WorkerDataStreamer(config);
        }

        public void Connect(string uri)
        {           
            if (config.socket == null)
            {
                config.socket = new RosSocket(new RosSharp.RosBridgeClient.Protocols.WebSocketSharpProtocol(uri));
                StartController();
            }
        }
        
        public void StartController()
        {
            if (config.controller_thread == null)
            {
                config.controller_thread = new Thread(new ThreadStart(config.controller_worker.Loop));
                config.controller_thread.Start();
            }
        }

        public void StopController()
        {
            if (config.controller_thread != null)
            {
                config.controller_thread.Abort();
                config.controller_thread = null;
            }
        }

        public void StartDataStreamer()
        {
            if (config.data_thread == null) {
                
                config.data_thread = new Thread(new ThreadStart(config.data_worker.Loop));
                config.data_thread.Start();
            }
        }

        public void StopDataStreamer()
        {
            if (config.data_thread != null)
            {
                config.data_thread.Abort();
                config.data_thread = null;
            }
        }
        

        public void Diconnect()
        {
            if (config.socket != null){
                StopController();
                StopDataStreamer();

                

                config.socket.Close();
                config.socket = null;
            }
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
            return String.Format("/{0}/{1}", config.node_name, path);
        }

        #endregion
    }


    public class Worker {
        public SharedConfig config;

        public Worker(SharedConfig config)
        {
            this.config = config;
        }
    }

    public class WorkerController: Worker
    {
        public WorkerController(SharedConfig config) : base(config) { }


        public void OnNumChannelUpdate(std_msgs.Int32 num_channels)
        {
            var changed = false;
            lock (config.ConfigLock)
            {
                if (config.num_channels != num_channels.data)
                {
                    config.num_channels = num_channels.data;
                    changed = true;
                }
            }


            if (changed) { OnSettingsChanged();}
        }

        public void OnSampleRateUpdate(std_msgs.Int32 sample_rate)
        {
            var changed = false;
            lock (config.ConfigLock)
            {
                if (config.sample_rate != sample_rate.data)
                {
                    config.sample_rate = sample_rate.data;
                    changed = true;
                }
            }
            if (changed) { OnSettingsChanged(); }
        }

        public void OnSettingsChanged()
        {
            lock (config.ConfigLock)
            {
                // adjust headset settings via sdk.
                config.headset_initialized = false;
            }
        }


        public void Init() {

            lock (config.SocketLock) { 
                //Data
                config.get_data_topic_id = config.socket.Advertise<std_msgs.String>(config.node.DataTopic());

                //Info
                config.get_srate_topic_id = config.socket.Advertise<std_msgs.Int32>(config.node.GetSampleRateTopic());
                config.set_srate_topic_id = config.socket.Subscribe<std_msgs.Int32>(config.node.SetSampleRateTopic(), config.controller_worker.OnSampleRateUpdate);

                //config
                config.get_nchan_topic_id = config.socket.Advertise<std_msgs.Int32>(config.node.GetNumChannelsTopic());
                config.set_nchan_topic_id = config.socket.Subscribe<std_msgs.Int32>(config.node.SetNumChannelsTopic(), config.controller_worker.OnNumChannelUpdate);
            }
        }
        
        public void Loop()
        {
            var msec_pause = (config.controller_hz < 1) ? 0 : 1000 / config.controller_hz;
            var msg_nchan = new std_msgs.Int32();
            var msg_srate = new std_msgs.Int32();
            while (true)
            {
                lock (config.ConfigLock)
                {
                    msg_nchan.data = config.num_channels;
                    msg_srate.data = config.sample_rate;
                }
                lock (config.SocketLock) { 
                    config.socket.Publish(config.get_nchan_topic_id, msg_nchan);
                    config.socket.Publish(config.get_srate_topic_id, msg_srate);
                }
                Thread.Sleep(msec_pause);
            }
        }

    }


    public class WorkerDataStreamer: Worker
    {
        public WorkerDataStreamer(SharedConfig config) : base(config) { }

        public bool ReinitHeadset() {
            bool result = false;

            lock (config.ConfigLock){
                if (!config.headset_initialized)
                {
                    //TODO: stop/restart the headset with new settings 

                    config.headset_initialized = true;
                    result = true;
                }
            }

            return result;
        }

        public void Loop()
        {
            var msec_pause = ( config.data_streamer_hz < 1 ) ? 0 : 1000 / config.data_streamer_hz;
            var msg_data = new std_msgs.String();
            var start_time = DateTime.Now;
            while (true)
            {
                if ( ReinitHeadset() ) {
                    start_time = DateTime.Now;
                }

                lock (config.ConfigLock){
                    msg_data.data = DateTime.Now.Subtract(start_time).TotalMilliseconds.ToString();
                }

                lock (config.SocketLock) { 
                    config.socket.Publish(config.get_data_topic_id, msg_data);
                }
                
                
                Thread.Sleep(msec_pause);
            }
        }
    }


}
