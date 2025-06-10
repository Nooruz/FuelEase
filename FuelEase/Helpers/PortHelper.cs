using System.Collections.Generic;
using System.IO.Ports;
using System.Management;

namespace FuelEase.Helpers
{
    public static class PortHelper
    {
        public static List<string> GetPortNamesWithDescriptions()
        {
            List<string> portNamesWithDescriptions = new();
            string[] portNames = SerialPort.GetPortNames();

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Name LIKE '%(COM%)'"))
            {
                foreach (var obj in searcher.Get())
                {
                    string name = obj["Name"].ToString();
                    foreach (string portName in portNames)
                    {
                        if (name.Contains(portName))
                        {
                            string description = name.Replace($"({portName})", "").Trim();
                            portNamesWithDescriptions.Add($"{portName} <{description}>");
                            break;
                        }
                    }
                }
            }
            return portNamesWithDescriptions;
        }
    }
}
