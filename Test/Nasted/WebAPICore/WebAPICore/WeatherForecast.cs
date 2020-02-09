using CommonStandard;
using System;

namespace WebAPICore
{
    public class WeatherForecast
    {
        public DateTime Date { get; set; }

        public int TemperatureC { get; set; }

        public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);

        public string Summary { get; set; }
    }

    public class TestUseCase
    {
        public void Use()
        {
            var xx = new TestClass();
        }

        public void Execute()
        {
            var xx = new TestClass();
            xx.Add();
        }

    }

    public class XxxZZZZ
    {
        public void Work()
        {
            var x = new TestClass();
            x.Add();
        }
    }
}
