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

// Staattiset asetukset
ResidenceSettings CurrentSettings = new();
double CurrentApplianceConsumption = 0;
ElectricityPriceService _priceService = new();
WeatherService _weatherService = new();

app.MapPost("/settings", (ResidenceSettings settings) =>
{
    CurrentSettings = settings;
    Console.WriteLine($"[EnergyServer] ASETUKSET PÄIVITETTY → {settings.People} asukasta, {settings.HeatingMethod}");
    return Results.Ok(new { message = "Asetukset päivitetty" });
});

app.MapGet("/energy/flow", async () =>
{
    // 1. Hae simuloitu aika TimeServerilta
    var timeClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };
    var timeData = await timeClient.GetFromJsonAsync<TimeResponseDto>("/time");

    var simulatedTime = timeData?.SimulatedTime ?? DateTime.Now;
    var simulatedHour = simulatedTime.Hour;

    // 2. Hae sää
    var weather = await _weatherService.GetWeather("Jyväskylä") ?? new Weather
    {
        Temperature = 12,
        Sunlight = 55,
        WindSpeed = 6
    };

    // 3. Hae spot-hinta simulaation ajan mukaan
    double currentPricePerKwh = await _priceService.GetCurrentPriceAsync(simulatedTime);

    // 4. Laske kulutus
    double baseConsumption = CurrentSettings.People * 1.8;

    double heating = CurrentSettings.HeatingMethod switch
    {
        "wood" => 5.5,
        "oil" => 8.5,
        "heat pump" => 3.8,
        "direct electricity" => 11.5,
        _ => 7
    };

    if (weather.Temperature < 12)
        heating += (15 - weather.Temperature) * 0.85;

    heating += weather.WindSpeed * 0.35;
    heating *= (1 - CurrentSettings.Insulation);

    double carCharging = CurrentSettings.CarType switch
    {
        "electric" => 6.5,
        "hybrid" => 2.5,
        _ => 0
    };

    var flow = new EnergyFlowDto
    {
        Timestamp = simulatedTime,
        SolarProductionKw = (simulatedHour >= 5 && simulatedHour <= 21)
            ? Math.Round(weather.Sunlight * 0.068, 2) : 0,

        ConsumptionKw = Math.Round(baseConsumption + heating + CurrentApplianceConsumption + carCharging, 2),

        BatteryLevelKwh = Math.Round(45 + Math.Sin(simulatedHour / 6.0) * 42, 1),
        BatteryLevelPercent = Math.Round(Math.Clamp(25 + Math.Sin(simulatedHour / 6.0) * 55, 10, 95), 1),

        GridImportKw = 0,
        GridExportKw = 0,
        StatusMessage = "Normaali toiminta"
    };

    // Energiatase
    double net = flow.SolarProductionKw - flow.ConsumptionKw;
    if (net > 0)
    {
        flow.GridExportKw = Math.Round(net * 0.65, 2);
        flow.BatteryChargeKw = Math.Round(net * 0.35, 2);
    }
    else
    {
        flow.GridImportKw = Math.Round(Math.Abs(net), 2);
    }

    flow.CurrentPricePerKwh = currentPricePerKwh;   // ← Uusi kenttä

    return Results.Ok(flow);
});

app.MapPost("/energy/appliances", (ApplianceStatusDto status) =>
{
    CurrentApplianceConsumption = status.TotalConsumptionFromAppliancesKw;
    return Results.Ok();
});

app.Run();

Console.WriteLine("✅ EnergyServer käynnissä → http://localhost:5002");