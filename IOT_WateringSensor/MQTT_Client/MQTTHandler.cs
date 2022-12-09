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

    public MqttHandler(WaterSensorDbContext dbContext)
    {
        _context = dbContext;
        _context.Database.EnsureCreated();
    }
    
    public async Task Run_Server_With_Logging()
    {
        var mqttFactory = new MqttFactory();

        using var mqttClient = mqttFactory.CreateMqttClient();
        
        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(Environment.GetEnvironmentVariable("MQTT_Broker") ?? "[::1]")
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += 
            async e => await ConsumePublishedMessage(e);

        await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
        
        var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(
                f =>
                {
                    f.WithTopic("water");
                })
            .Build();

        await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);

        // Trick to keep the program running without busy waiting the thread
        var manualResetEvent = new ManualResetEvent(false);
        manualResetEvent.WaitOne();
    }

    private async Task ConsumePublishedMessage(MqttApplicationMessageReceivedEventArgs e)
    {
        if(!e.ApplicationMessage.Topic.Contains("water"))
            return;
            
        var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

        var payload = JsonConvert
            .DeserializeObject<SensorDataDto>(message);
            
        Console.WriteLine("Intercepted publish: " + payload.ClientId + " " + payload.TimeStamp + " " + payload.Moisture);

        await _context.SensorData.AddAsync(new SensorData
        {
            DeviceId = payload.ClientId,
            TimeStamp = payload.TimeStamp,
            Moisture = payload.Moisture
        });
        
        await _context.SaveChangesAsync();
    }
}