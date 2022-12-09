using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace DataSource;

class Program
{
    
    private static Dictionary<Guid, int> _guids = new Dictionary<Guid, int> {
        {Guid.Parse("913c70fb-365b-48f8-b6e8-42c5ca71fbbe"), 70},
        {Guid.Parse("a90b5f11-a675-4523-89c2-84b7ff9f0935"), 50},
        {Guid.Parse("2a7f264d-b158-4d10-b6a9-cc9b5dd8b726"), 30}};
    
    public static async Task Main(string[] args)
    {
        var mqttFactory = new MqttFactory();
        var random = new Random();

        for (;;)
        {
            using var mqttClient = mqttFactory.CreateMqttClient();
            
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_Broker") ?? "[::1]")
                .Build();

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            foreach (var guid in _guids)
            { 
                var newMoisture = Math.Clamp(guid.Value + random.Next(-1, 2), 0, 100);
                
                _guids[guid.Key] = newMoisture;
                
                var json = JsonConvert.SerializeObject(new { clientid = guid.Key, timestamp = DateTime.Now, Moisture = newMoisture});

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic("water")
                    .WithPayload(json)
                    .Build();
                    
                await mqttClient.PublishAsync(message, CancellationToken.None);
            }
            await mqttClient.DisconnectAsync();

            Console.WriteLine("MQTT application message is published.");
            Thread.Sleep(1000);
        }
    }
}