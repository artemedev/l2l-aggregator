using SixLabors.ImageSharp;
using System.Collections.Concurrent;
//using DM_process;
//using DM_recogn_lib; 
namespace DM_wraper_NS
{
    public class DM_recogn_wraper
    {
        //DM_process dM_Process = null;
        public delegate void NewDMResultContainer(int countResult);
        //__________ For Library _________________
        public event Action libUpdateParams = delegate { };
        public event Action libUpdatePrintPattern = delegate { };
        public event Action libTakeShot = delegate { };
        //__________ For Software _________________
        public event NewDMResultContainer swNewDMResult = delegate { };

        private ConcurrentQueue<DM_result_data> _result = new ConcurrentQueue<DM_result_data>();
        private DM_recogn_params _params;
        private string pathToPrintPattern;
        public void Init()
        {
            Console.WriteLine("DM_recogn_wraper - initialisation");
        }

        //
        //__________ For Software _________________
        //

        public DM_recogn_params GetParams()
        {
            return _params;
        }
        public DM_result_data GetDMResult()
        {
            DM_result_data newResult;
            if (_result.TryDequeue(out newResult))
                return newResult;
            throw new Exception("DM results list is empty");
        }
        public bool SetParams(DM_recogn_params newParams)
        {
            _params = newParams;
            libUpdateParams();
            Console.WriteLine("");
            return true;
        }

        public bool SendPrintPattern(string patternPath)
        {
            Console.WriteLine("");
            pathToPrintPattern = patternPath;
            libUpdatePrintPattern();
            return true;
        }

        public bool SendShotFrameComand()
        {
            libTakeShot();
            return true;
        }

        //
        //__________ For Library _________________
        //

        public string GetPathToPrintPattern()
        {
            return pathToPrintPattern;
        }
        public void initForLib(DM_recogn_params defParams)
        {
            _params = defParams;
            Console.WriteLine();
        }

        public bool Update_result_data(DM_result_data newResult)
        {
            _result.Enqueue(newResult);
            swNewDMResult(_result.Count);
            return true;
        }
    }

    public struct DM_recogn_params
    {
        public int countOfDM = 0;
        public int pixInMM = 10;
        public string cameraName = "Debug";
        public string cameraAddress = "";
        public bool softwareTrigger = true;
        public bool hrdwareTrigger = false;
        public bool OCRRecogn = false;
        public bool packRecogn = false;

        public DM_recogn_params()
        {
        }
    }

    public struct DM_result_data
    {
        public Image rawImage;
        public Image processedImage;
        public List<DM_dataCell> DMdataArr;
        public void setDMDataList(List<DM_dataCell> _DMdataArr)
        {
            DMdataArr = _DMdataArr;
        }
    }

    public struct DM_dataCell
    {
        public List<DM_dataOcr> OCR;
        public int poseX;
        public int poseY;
        public int width;
        public int height;
        public string packType;
        public bool isError;
        public int angle;
    }
    public struct DM_dataOcr
    {
        public string data;
        public string name;
        public int poseX;
        public int poseY;
        public int width;
        public int height;
        public string packType;
        public bool isError;
        public int angle;

    }
}