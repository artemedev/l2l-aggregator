using DM_process_lib;
using DM_wraper_NS;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace l2l_aggregator.Services.DmProcessing
{
    public class DmScanService
    {
        private readonly DM_recogn_wraper _recognWrapper;
        private readonly DM_process _dmProcess;
        private static TaskCompletionSource<bool> _dmrDataReady;

        private TaskCompletionSource<bool>? _startOkSignal;
        public DmScanService()
        {
            _recognWrapper = new DM_recogn_wraper();
            _recognWrapper.Init();
            _recognWrapper.swNewDMResult += OnNewResult;
            _recognWrapper.alarmEvent += alarmEvent;
            _recognWrapper.swStartOk += OnStartOk;
            _recognWrapper.swFindedCamerasList += showListOfCam;
            _dmProcess = new DM_process();
            _dmProcess.Init(_recognWrapper);
        }

        private result_data _dmrData;
        public void StartScan(string base64Template)
        {
            _dmrDataReady = new TaskCompletionSource<bool>();
            _recognWrapper.SendPrintPatternXML(base64Template);
            _recognWrapper.SendStartShotComand();
            
        }
        public void SaveXmlToFile(string xmlContent, string filePath)
        {
            try
            {
                // Убедимся, что директория существует
                string? directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                // Записываем в UTF-8 без BOM — для Linux это стандарт
                File.WriteAllText(filePath, xmlContent, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка при сохранении XML-файла: {ex.Message}");
                throw;
            }
        }
        public bool ConfigureParams(recogn_params parameters)
        {
            return _recognWrapper.SetParams(parameters);
        }
        public void StopScan()
        {
            _recognWrapper.SendStopShotComand();
        }
        public async Task<result_data> WaitForResultAsync()
        {
            await _dmrDataReady.Task;
            return _dmrData;
        }
        private void OnNewResult(int countResult)
        {
            _dmrData = _recognWrapper.GetDMResult();
            _dmrDataReady.TrySetResult(true);
        }
        public void startShot()
        {
            _dmrDataReady = new TaskCompletionSource<bool>();
            _recognWrapper.SendShotFrameComand();
        }
        public Task WaitForStartOkAsync()
        {
            _startOkSignal = new TaskCompletionSource<bool>();
            return _startOkSignal.Task;
        }
        private void OnStartOk()
        {
            _startOkSignal?.TrySetResult(true);
        }
        private static void showListOfCam(string[] res)
        {
            if (res == null || res.Length < 1)
            {
                Console.WriteLine("CAMERA NOT AVALIBLE!");
                return;
            }
            foreach (string s in res)
            {
                Console.WriteLine(s);
            }
            Console.WriteLine("All cameras finded");
        }
        public static void alarmEvent(string textEvent, string typeEvent)
        {
            Console.WriteLine($"ALARM EVENT {typeEvent} {textEvent}");
        }
    }
}
