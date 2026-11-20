using System.Text.Json;

namespace Common.Models;

    public class WeatherService
    {
        private readonly HttpClient _client = new HttpClient();

        private const string ApiKey = "438aafe410bc1f6773de939f6193d656";

        public async Task<GeoLocation?> GetCoordinates(string city)
        {
            string geoUrl = $"http://api.openweathermap.org/geo/1.0/direct?q={city}&limit=1&appid={ApiKey}";
            var geoJson = await _client.GetStringAsync(geoUrl);
            var locations = JsonSerializer.Deserialize<List<GeoLocation>>(geoJson);
            return locations?.FirstOrDefault();
        }

        public async Task<Weather?> GetWeather(string city = "Jyväskylä")
        {
            var location = await GetCoordinates(city);
            if (location == null) return null;

            string weatherUrl =
                $"https://api.openweathermap.org/data/2.5/weather?lat={location.Lat}&lon={location.Lon}&appid={ApiKey}&units=metric";

            var weatherJson = await _client.GetStringAsync(weatherUrl);
            var data = JsonSerializer.Deserialize<OpenWeatherResponse>(weatherJson);

            if (data == null) return null;

            return new Weather
            {
                Temperature = Math.Round(data.Main.Temp, 1),
                Sunlight = 100 - data.Clouds.All,
                WindSpeed = Math.Round(data.Wind.Speed, 1)
            };
        }
    }
