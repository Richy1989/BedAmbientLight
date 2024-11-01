using System;
using System.Collections;
using System.Diagnostics;
using System.Text;
using System.Threading;
using BedLightESP.Settings;
using nanoFramework.M2Mqtt;
using nanoFramework.M2Mqtt.Messages;

namespace BedLightESP.Mqtt
{
    internal class MqttManager
    {
        //Make sure only one thread at the time can work with the mqtt service
        private readonly ManualResetEvent mreMQTT = new(true);
        private readonly ISettingsManager _settingsManager;
        private MqttClient mqtt;
        private readonly CancellationToken token;
        private bool resubscribeAll = false;
        private bool stopService = false;
        private bool enableAutoRestart = true;
        public IDictionary SubscribeTopics { get; private set; }

        /// <summary>Initializes a new MQTT Manager.</summary>
        /// <param name="modicusStartupManager"></param>
        /// <param name="token"></param>
        public MqttManager(ISettingsManager settingsManager)
        {
            _settingsManager = settingsManager;
            SubscribeTopics = new Hashtable();
        }

        /// <summary>Initialize the MQTT Service, repeat this function periodically to make sure we always reconnect.</summary>
        public void InitializeMQTT()
        {
            if (mqtt != null && mqtt.IsConnected)
            {
                Debug.WriteLine("++++ MQTT is already running. No need to start. ++++");
                return;
            }

            stopService = false;
            mqtt?.Close();
            mqtt?.Dispose();

            bool autoRestartNeeded = false;
            do
            {
                autoRestartNeeded = false;
                try
                {
                    if (!EstablishConnection())
                    {
                        if (enableAutoRestart)
                            autoRestartNeeded = true;
                        else
                            return;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ERROR connecting MQTT: {ex.Message}");

                    if (enableAutoRestart)
                        autoRestartNeeded = true;
                }
                if (autoRestartNeeded)
                    Thread.Sleep(1000);
            }
            while (autoRestartNeeded && !token.IsCancellationRequested && !stopService);

            //if (resubscribeAll)
            //    ResubscribeToNewClientID();

            string[] topics = new string[SubscribeTopics.Keys.Count];
            MqttQoSLevel[] level = new MqttQoSLevel[SubscribeTopics.Keys.Count];

            int i = 0;
            foreach (var item in SubscribeTopics.Keys)
            {
                topics[i] = (string)item;
                level[i++] = MqttQoSLevel.ExactlyOnce;
            }

            if (topics.Length > 0)
            {
                mqtt.Subscribe(topics, level);
                mqtt.MqttMsgPublishReceived += Mqtt_MqttMsgPublishReceived;
            }
            resubscribeAll = false;
        }

        public void StopMqtt(bool preventAutoRestart = true)
        {
            //make sure we do not restart the service automatically on stopping
            stopService = preventAutoRestart;

            //Set this variable to ensure a gracefull startup when we resart the service
            resubscribeAll = true;

            enableAutoRestart = false;

            mreMQTT.WaitOne();

            //mqtt?.Disconnect();
            mqtt?.Close();
            mqtt?.Dispose();

            mreMQTT.Set();
        }

        /// <summary>.Create new MQTT Client and start a connectrion with necessary subscriptions.</summary>
        private bool EstablishConnection()
        {
            mqtt = new MqttClient(_settingsManager.Settings.MqttServer, _settingsManager.Settings.MqttPort, secure: false, null, null, MqttSslProtocols.None);
            var ret = mqtt.Connect(_settingsManager.Settings.MqttClientID, _settingsManager.Settings.MqttUsername, _settingsManager.Settings.MqttPassword);

            if (ret != MqttReasonCode.Success)
            {
                Debug.WriteLine($"++++ ERROR connecting: {ret} ++++");
                mqtt.Disconnect();
                return false;
            }

            mqtt.ConnectionClosed += (s, e) =>
            {
                if (!stopService)
                    InitializeMQTT();
            };

            enableAutoRestart = true;

            Debug.WriteLine($"++++ MQTT connecting successful: {ret} ++++");
            return true;
        }

        /// <summary>Event callback when a subscribed message is received.</summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">MqttMsgPublishEventArgs</param>
        private void Mqtt_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Debug.WriteLine($"++++ MQTT Command Received:\nTopic:\n{e.Topic}\nContent:\n{Encoding.UTF8.GetString(e.Message, 0, e.Message.Length)} ++++");

            //var subscriber = (ICommand)SubscribeTopics[e.Topic];
            //subscriber?.Execute(Encoding.UTF8.GetString(e.Message, 0, e.Message.Length));
        }

        /// <summary>Publish a message to the MQTT broker.</summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        public void Publish(string topic, string message) => SendMessage(topic, Encoding.UTF8.GetBytes(message));

        ///// <summary>Add new subscribing service to a specified command topic.</summary>
        ///// <param name="subscriber"></param>
        //private void AddSubcriber(ICommand subscriber)
        //{
        //    var topic = $"{globalSettings.MqttSettings.MqttClientID}/cmd/{subscriber.Topic}";
        //    SubscribeTopics.Add(topic, subscriber);
        //}

        /// <summary>Send a the message to publish.</summary>
        /// <param name="topic"></param>
        /// <param name="message"></param>
        private void SendMessage(string topic, byte[] message)
        {
            if (mqtt == null || !mqtt.IsConnected) return;

            string to = $"{_settingsManager.Settings.MqttClientID}/{topic}";
            mqtt.Publish(to, message);
        }
    }
}