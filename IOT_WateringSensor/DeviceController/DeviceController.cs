using IOT_WateringSensor.Data;
using IOT_WateringSensor.Database;
using Microsoft.AspNetCore.Mvc;

namespace IOT_WateringSensor.DeviceController;

[ApiController]
[Route("api/device")]
public class DeviceController : Controller
{
    public DeviceController()
    {
    }
    
    public class RegisterDeviceRequest
    {
        public string? deviceId { get; set; }
        public string? bindingKey { get; set; }
    }

    [HttpPost]
    [Route("prepareBinding")]
    public async Task<ActionResult<string>> PrepareBinding(
        [FromServices] WaterSensorDbContext dbContext,
        [FromBody] string deviceId)
    {
        var binding = dbContext.UserToDeviceBindings.FirstOrDefault(x => x.DeviceId == deviceId);
        
        if(binding != null)
            return BadRequest("A binding is already pending for this device");
        
        var newBinding = new UserToDeviceBinding
        {
            DeviceId = deviceId,
            bindingKey = Guid.NewGuid().ToString(),
            isBound = false
        };
        
        dbContext.UserToDeviceBindings.Add(newBinding);

        await dbContext.SaveChangesAsync();

        return Ok(newBinding.bindingKey);
    }
}