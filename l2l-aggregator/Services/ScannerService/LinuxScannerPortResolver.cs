using l2l_aggregator.Services.ScannerService.Interfaces;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace l2l_aggregator.Services.ScannerService
{
    public class LinuxScannerPortResolver : IScannerPortResolver
    {
        public IEnumerable<string> GetHoneywellScannerPorts()
        {
            var result = new List<string>();
            var ttyDevices = Directory.GetFiles("/dev", "ttyUSB*").Concat(Directory.GetFiles("/dev", "ttyACM*"));

            foreach (var dev in ttyDevices)
            {
                var devName = Path.GetFileName(dev);
                var vendorPath = $"/sys/class/tty/{devName}/device/../idVendor";

                if (File.Exists(vendorPath))
                {
                    var vid = File.ReadAllText(vendorPath).Trim().ToLower();
                    if (vid == "0c2e")
                    {
                        result.Add(dev);
                    }
                }
            }

            return result;
        }
    }
}
