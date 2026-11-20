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

// Simulaation tila (tallennetaan staattiseen luokkaan)
var state = new TimeSimulationState();

app.MapGet("/time", () =>
{
    var realNow = DateTime.UtcNow;
    var elapsedRealSeconds = (realNow - state.RealStartTime).TotalSeconds;

    var simulatedTime = state.SimulatedStartTime.AddSeconds(elapsedRealSeconds * state.SimulationSpeed);

    var response = new TimeResponseDto
    {
        RealTime = realNow,
        SimulatedTime = simulatedTime,
        SimulationSpeed = state.SimulationSpeed,
        SimulatedHoursPerRealSecond = state.SimulationSpeed / 3600.0
    };

    return Results.Ok(response);
});

// Aseta simulaation parametrit
app.MapPost("/time/config", (SimulationConfig config) =>
{
    state.SimulatedStartTime = config.StartTime;
    state.RealStartTime = DateTime.UtcNow;
    state.SimulationSpeed = config.Speed;
    return Results.Ok(new { message = "Simulaation asetukset päivitetty" });
});

app.Run();

Console.WriteLine("✅ TimeServer käynnissä → http://localhost:5000");

// ====================== APU LUOKAT ======================
public class TimeSimulationState
{
    public DateTime SimulatedStartTime { get; set; } = DateTime.Now;
    public DateTime RealStartTime { get; set; } = DateTime.Now;
    public long SimulationSpeed { get; set; } = 60; // 60x nopeus
}

public class SimulationConfig
{
    public DateTime StartTime { get; set; } = DateTime.Now;
    public long Speed { get; set; } = 60;
}