using System.Text.Json.Serialization;
using IOT_WateringSensor;
using IOT_WateringSensor.Areas.Identity;
using IOT_WateringSensor.Database;
using IOT_WateringSensor.MQTT_Client;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MudBlazor.Services;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.ConfigureServices();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
    .AddEntityFrameworkStores<WaterSensorDbContext>();
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();
builder.Services.AddScoped<AuthenticationStateProvider, RevalidatingIdentityAuthenticationStateProvider<IdentityUser>>();
new Thread(async _ =>
{
    var serviceCollection = builder.Services.BuildServiceProvider();
    var handler = new MqttHandler(serviceCollection.GetRequiredService<WaterSensorDbContext>());
    await handler.Run_Server_With_Logging();
}).Start();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.MapBlazorHub();

app.MapWhen(ctx => !ctx.Request.Path.StartsWithSegments("/api"), blazor =>
{
    blazor.UseEndpoints(endpoints =>
    {
        endpoints.MapFallbackToPage("/_Host");
    });
});

//explicitly map api endpoints only when path starts with api
app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/api"), api =>
{
    api.UseEndpoints(endpoints =>
    {
        endpoints.MapControllers();
    });
});
/*
await using (var scope = builder.Services.BuildServiceProvider().GetRequiredService<WaterSensorDbContext>())
{
    await scope.Database.EnsureCreatedAsync();
}
*/
app.Run();