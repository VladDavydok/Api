using Newtonsoft.Json;

namespace WebApplication1.Models
{
    public class WaterModel
    {
        public Data_Day Data_Day { get; set; }
    }
    //public class Data_Day
    //{
    //    public string[] Time { get; set; }
    //    public double[] SeaSurfaceTemperature_Mean { get; set; }
    //}
    public class Data_Day
    {
        public List<Time> time { get; set; }
        public List<SeaSurfaceTemperature_Mean> seaSurfaceTemperature_Mean { get; set; }
    }
    public class Time
    {
        public string time1 { get; set; }
        public string time2 { get; set; }
        public string time3 { get; set; }
        public string time4 { get; set; }
        public string time5 { get; set; }
        public string time6 { get; set; }
        public string time7 { get; set; }
    }
    public class SeaSurfaceTemperature_Mean
    {
        public double seaSurfaceTemperature_Mean1 { get; set; }
        public double seaSurfaceTemperature_Mean2 { get; set; }
        public double seaSurfaceTemperature_Mean3 { get; set; }
        public double seaSurfaceTemperature_Mean4 { get; set; }
        public double seaSurfaceTemperature_Mean5 { get; set; }
        public double seaSurfaceTemperature_Mean6 { get; set; }
        public double seaSurfaceTemperature_Mean7 { get; set; }
    }
}
