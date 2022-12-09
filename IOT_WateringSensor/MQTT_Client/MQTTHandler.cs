using System.Text;
using IOT_WateringSensor.Data;
using IOT_WateringSensor.Database;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;

namespace IOT_WateringSensor.MQTT_Client;

public class MqttHandler
{
    private static WaterSensorDbContext _context;
    private static IMqttClient _mqttClient;

    public MqttHandler(WaterSensorDbContext dbContext)
    {
        _context = dbContext;
        _context.Database.EnsureCreated();
    }
    
    public async Task Run_Server_With_Logging()
    {
        var mqttFactory = new MqttFactory();

        _mqttClient = mqttFactory.CreateMqttClient();
        
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_Broker") ?? "[::1]")
            .Build();

        _mqttClient.ApplicationMessageReceivedAsync += 
            async e => await ConsumePublishedMessage(e);

        await _mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(
                f =>
                {
                    f.WithTopic("water");
                    f.WithTopic("bind");
                })
            .Build();

        await _mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        // Trick to keep the program running without busy waiting the thread
        var manualResetEvent = new ManualResetEvent(false);
        manualResetEvent.WaitOne();
    }

    private async Task ConsumePublishedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
        switch (e.ApplicationMessage.Topic)
        {
            case "water":
                var payload = JsonConvert.DeserializeObject<SensorDataDto>(message);
                Console.WriteLine("Intercepted publish: " + payload.ClientId + " " + payload.TimeStamp + " " + payload.Moisture);
                
                await _context.SensorData.AddAsync(new SensorData
                {
                    DeviceId = payload.ClientId,
                    TimeStamp = payload.TimeStamp,
                    Moisture = payload.Moisture
                });

                break;
            case "bind":
                var deviceId = message;
                Console.WriteLine("Device tries to bind: " + deviceId);

                var binding = _context.UserToDeviceBindings.FirstOrDefault(x => x.DeviceId == deviceId);

                if (binding != null)
                    return;
        
                var newBinding = new UserToDeviceBinding
                {
                    DeviceId = deviceId,
                    bindingKey = Guid.NewGuid().ToString(),
                    isBound = false
                };
        
                _context.UserToDeviceBindings.Add(newBinding);

                var responseMessage = new MqttApplicationMessageBuilder()
                    .WithTopic(deviceId)
                    .WithPayload(newBinding.bindingKey)
                    .Build();
                    
                await _mqttClient.PublishAsync(responseMessage, CancellationToken.None);

                break;
            default:
                return;
            
        }

        await _context.SaveChangesAsync();
    }
}