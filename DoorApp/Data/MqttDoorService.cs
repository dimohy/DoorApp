using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;

using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DoorApp.Data
{
    public class MqttDoorService : IMqttApplicationMessageReceivedHandler
    {
        private const string Server = "broker.hivemq.com";
        private const string RequestTopic = "/shingu/door_control/door_app";


        private IMqttClient? _client;
        private ConcurrentDictionary<string, bool> _registDeviceIdMap = new ConcurrentDictionary<string, bool>();


        public MqttDoorService()
        {
            LoadRegistDeviceIds();
        }

        private void LoadRegistDeviceIds()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var filename = Path.Combine(path, "deviceIds.txt");
            if (File.Exists(filename) is false)
                return;
            
            var lines = File.ReadAllLines(filename);
            foreach (var id in lines)
                _registDeviceIdMap[id] = true;
        }
        
        private void SaveRegistDeviceIds()
        {
            var path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
            var filename = Path.Combine(path, "deviceIds.txt");
            File.WriteAllLines(filename, _registDeviceIdMap.Keys.ToArray());
        }

        public async Task StartServiceAsync()
        {
            var factory = new MqttFactory();

            _client = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(Server)
                .Build();

            _client.ApplicationMessageReceivedHandler = this;

            await _client.ConnectAsync(options, CancellationToken.None);

            var subOptions = factory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f => f.WithTopic(RequestTopic))
                .Build();

            await _client.SubscribeAsync(subOptions, CancellationToken.None);

            //await RegistAsync("1234");
        }

        private async Task RegistAsync(string deviceId)
        {
            var info = new Message("Regist", "door", deviceId);
            var payload = JsonSerializer.SerializeToUtf8Bytes(info);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(RequestTopic)
                .WithPayload(payload)
                .Build();

            await _client.PublishAsync(message);
        }

        Task IMqttApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs e)
        {
            var message = e.ApplicationMessage;

            try
            {
                if (message.Topic == RequestTopic)
                {
                    var info = JsonSerializer.Deserialize<Message>(message.Payload);
                    if (info is null)
                        return Task.CompletedTask;

                    if (info.DeviceType != "door")
                        return Task.CompletedTask;

                    _registDeviceIdMap[info.DeviceId] = true;

                    SaveRegistDeviceIds();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return Task.CompletedTask;
        }

        public bool IsRegistDeviceId(string deviceId)
        {
            if (deviceId is null)
                return false;

            var result = _registDeviceIdMap.ContainsKey(deviceId);
            return result;
        }
    }
}
