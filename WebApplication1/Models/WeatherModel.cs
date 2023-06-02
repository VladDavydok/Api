namespace WebApplication1.Models
{
    public class WeatherModel
    {
        public Forecast Forecast { get; set; }
    }

    public class Forecast
    {
        public List<ForecastDay> ForecastDay { get; set; }
    }

    public class ForecastDay
    {
        public DateTime Date { get; set; }
        public DayTemperature Day { get; set; }
    }

    public class DayTemperature
    {
        public double Avgtemp_c { get; set; }
        public double Daily_chance_of_rain { get; set; }
    }
}
