using MD.Devices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace l2l_aggregator.Helpers
{
    public static class DeviceHelper
    {
        /// <summary>
        /// Ожидает, пока устройство перейдет в нужное состояние или истечет таймаут.
        /// </summary>
        public static void WaitForState(Device device, DeviceStatusCode desiredState, int timeoutSeconds)
        {
            var startTime = DateTime.UtcNow;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);

            while (device.Status != desiredState)
            {
                if (DateTime.UtcNow - startTime > timeout)
                    throw new TimeoutException($"Устройство '{device.Name}' не достигло состояния {desiredState} за {timeoutSeconds} секунд.");

                Thread.Sleep(100);
            }
        }
    }
}
