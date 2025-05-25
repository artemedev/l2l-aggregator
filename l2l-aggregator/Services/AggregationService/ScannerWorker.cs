using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO.Ports;

namespace l2l_aggregator.Services.AggregationService
{
    internal class ScannerWorker(string portName) : BackgroundWorker
    {
        public event Action<string> BarcodeScanned;

        private System.IO.Ports.SerialPort scannerPort = new System.IO.Ports.SerialPort(portName, 9600, System.IO.Ports.Parity.None, 8, System.IO.Ports.StopBits.One);
        protected override void OnDoWork(DoWorkEventArgs e)
        {
            scannerPort.DataReceived += ScannerPort_DataReceived;
            scannerPort.Open();
        }

        private void ScannerPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = scannerPort.ReadExisting();
                if (!string.IsNullOrWhiteSpace(data))
                {
                    Debug.WriteLine($"[ScannerWorker] Считан ШК: {data}");
                    BarcodeScanned?.Invoke(data.Trim());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[ScannerWorker] Ошибка чтения: {ex.Message}");
            }

        }

        protected override void Dispose(bool disposing)
        {
            Debug.WriteLine("Прихолпываем воркер");
            if (scannerPort != null)
            {
                if (!scannerPort.IsOpen)
                    scannerPort.Close();
                scannerPort.Dispose();
            }
            base.Dispose(disposing);
        }

    }
}
