using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Threading;

namespace Arduino
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Connecting");
            SerialPort serial = new SerialPort("COM3",9600);
            serial.Open();
            Console.WriteLine("Connection Succesfull!");
            Console.WriteLine("Set LED ON HIGH");
            serial.Write("H");
            Thread.Sleep(4000);
            Console.WriteLine("SET LED ON LOW");
            serial.Write("L");
            Thread.Sleep(2000);
            serial.Close();
        }
    }
}
