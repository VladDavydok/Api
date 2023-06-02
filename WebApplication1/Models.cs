public class Park
{
    public string Name { get; set; }
    public double Lat { get; set; }
    public double Lng { get; set; }
    public Park(string name, double lat, double lng)
    {
        Name = name;
        Lat = lat;
        Lng = lng;
    }
}
public class Forecast
{
    public string Date { get; set; }
    public double Avgtemp_c { get; set; }
    public double Daily_chance_of_rain { get; set; }
    public Forecast(string date, double avgtemp_c, double daily_chance_of_rain)
    {
        Date = date;
        Avgtemp_c = avgtemp_c;
        Daily_chance_of_rain = daily_chance_of_rain;
    }
}
public class Water
{
    public string Time { get; set; }
    public double Seasurfacetemperature_mean { get; set; }
    public Water(string time, double seasurfacetemperature_mean)
    {
        Time = time;
        Seasurfacetemperature_mean = seasurfacetemperature_mean;
    }
}

