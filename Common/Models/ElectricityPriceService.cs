namespace Common.Models;

public class ElectricityPriceService
{
    private double _currentPrice = 0.185;
    private DateTime _lastSimulatedHour = DateTime.MinValue;

    public async Task<double> GetCurrentPriceAsync(DateTime simulatedTime)
    {
        // Vaihdetaan hintaa aina kun simuloitu tunti vaihtuu
        if (simulatedTime.Hour != _lastSimulatedHour.Hour ||
            simulatedTime.Date != _lastSimulatedHour.Date)
        {
            // Simuloidaan realistista spot-hinnan vaihtelua kellonajan mukaan
            double basePrice = simulatedTime.Hour switch
            {
                >= 0 and < 6 => 0.09,   // Yö - halpa
                >= 6 and < 10 => 0.22,   // Aamu
                >= 10 and < 14 => 0.28,  // Päivä
                >= 14 and < 18 => 0.35,  // Iltapäivä
                >= 18 and < 22 => 0.42,  // Ilta - kallis
                _ => 0.18                // Muut ajat
            };

            // Lisätään pieni satunnainen vaihtelu
            var random = new Random();
            _currentPrice = Math.Round(basePrice + (random.NextDouble() * 0.18 - 0.09), 3);

            _lastSimulatedHour = simulatedTime;

            Console.WriteLine($"[Sähkön hinta] Simuloitu tunti {simulatedTime:HH:mm} → Hinta: {_currentPrice:F3} €/kWh");
        }

        return _currentPrice;
    }

    // Ylikuormitus vanhaa kutsua varten (jos ei anna aikaa)
    public async Task<double> GetCurrentPriceAsync()
    {
        return await GetCurrentPriceAsync(DateTime.Now);
    }
}