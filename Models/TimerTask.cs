using System;

namespace GreeControlAPI.Models
{
    public class TimerTask
    {
        public string MacAddress { get; set; }
        public string Action { get; set; } // "TurnOn" or "TurnOff"
        public DateTimeOffset Time { get; set; }
    }
}
