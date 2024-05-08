namespace GreeControlAPI
{
    public class WeatherForecast
    {
        public DateOnly Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string? Summary { get; set; }
    }

    public class SubInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Mac { get; set; } = string.Empty;
        public SubInfo(string name, string mac)
        {
            Name = name;
            Mac = mac;
        }
    }
    public class ResponseDevInfo
    {
        public string Devmac { get; set; } = string.Empty;
        public string Devip { get; set; } = string.Empty;
        public List<SubInfo> Devsubinfolist { get; set; }        

        public string PrivateKey { get; set; } = string.Empty;
    }


    public class DeviceStatus
    {
        public int Pow { get; set; }
        public int SetTem { get; set; }
        public int WdSpd { get; set; }
    }


}
