using Common.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/weather", async () =>
{
    var weatherService = new WeatherService();
    var weather = await weatherService.GetWeather("Jyväskylä");

    if (weather == null)
    {
        // Fallback jos API ei toimi
        weather = new Weather
        {
            Temperature = 14.5,
            Sunlight = 65,
            WindSpeed = 6.2
        };
    }

    Console.WriteLine($"[WeatherServer] Jyväskylä: {weather.Temperature}°C, Sunlight {weather.Sunlight}%, Wind {weather.WindSpeed} m/s");

    return Results.Ok(weather);
});

app.Run();

Console.WriteLine("WeatherServer käynnissä → http://localhost:5001");