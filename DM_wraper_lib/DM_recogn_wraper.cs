﻿using SixLabors.ImageSharp;
using System.Collections.Concurrent;
//using DM_process;
//using DM_recogn_lib; 
namespace DM_wraper_NS
{
    public class DM_recogn_wraper
    {
        //event of new result
        public delegate void NewResultContainer(int countResult);
        //event with error message 
        public delegate void AlarmEventForUser(string textEvent, string typeEvent);

        //__________ For Library _________________
        public event Action libUpdateParams = delegate { };
        public event Action libUpdatePrintPattern = delegate { };
        public event Action libTakeShot = delegate { };
        public event Action libStartShot = delegate { };
        public event Action libStopShot = delegate { };
        public event Action libGetCameras = delegate { };

        //--------

        //__________ For Software _________________
        public event NewResultContainer swNewDMResult = delegate { };
        public event AlarmEventForUser alarmEvent = delegate { };

        //-------------------------------------------
        public delegate void FindedCameras(string[] cameras);

        public event FindedCameras swFindedCamerasList = delegate { };
        public event Action swPrintPaternOk = delegate { };
        public event Action swParamsOk = delegate { };
        public event Action swStartOk = delegate { };
        public event Action swShotOk = delegate { };
        public event Action swStopOk = delegate { };
        //-------------------------------------------

        private ConcurrentQueue<result_data> _result = new ConcurrentQueue<result_data>();
        private recogn_params _params;
        private string pathToPrintPattern;
        private string XMLoPrintPattern;
        private string[] _camerasList;
        public void Init()
        {
            Console.WriteLine("DM_recogn_wraper - initialisation");
        }

        //
        //__________ For Software _________________
        //

        public recogn_params GetParams()
        {
            return _params;
        }
        public result_data GetDMResult()
        {
            result_data newResult;
            if (_result.TryDequeue(out newResult))
            {
                Console.WriteLine($"GetDMResult: {newResult.BOXs.Count()}");
                return newResult;
            }

            throw new Exception("DM results list is empty");
        }
        public bool SetParams(recogn_params newParams)
        {
            _params = newParams;
            libUpdateParams();
            Console.WriteLine("SetParams - ok");
            return true;
        }

        /// <summary>
        /// Send FastReport (fr3) template
        /// for OCR configuration and BOX detection
        /// </summary>
        /// <param name="patternPath">path to fr3 file</param>
        public bool SendPrintPattern(string patternPath)
        {
            pathToPrintPattern = patternPath;
            XMLoPrintPattern = "";
            libUpdatePrintPattern();
            return true;
        }

        /// <summary>
        /// Send FastReport (fr3) template
        /// for OCR configuration and BOX detection
        /// </summary>
        /// <param name="patternPath">XML fr3 data</param>
        public bool SendPrintPatternXML(string XMLPattern)
        {
            Console.WriteLine(XMLPattern);
            XMLoPrintPattern = XMLPattern;
            pathToPrintPattern = "";
            libUpdatePrintPattern();
            return true;
        }

        /// <summary>
        /// send software signal for captue image
        /// DONT USE when use hardware trigger
        /// </summary>
        public bool SendShotFrameComand()
        {
            Console.WriteLine("DM_recogn_wraper - SendShotFrameComand");
            libTakeShot();
            return true;
        }

        /// <summary>
        /// start shot process
        /// use then hardware trigger
        /// </summary>
        public bool SendStartShotComand()
        {
            Console.WriteLine("DM_recogn_wraper - SendStartShotComand");
            libStartShot();
            return true;
        }

        /// <summary>
        /// abort capture image. Turn off camera framegrabber
        /// reset all settings
        /// </summary>
        public bool SendStopShotComand()
        {
            libStopShot();
            return true;
        }

        public string[] GetCameras()
        {
            _camerasList = [];
            libGetCameras();
            Thread.Sleep(1000);
            return _camerasList;
        }

        //
        //__________ For Library _________________
        //

        public void SetCamerasList(string[] _camList)
        {
            _camerasList = _camList;
        }

        public string GetPathToPrintPattern()
        {
            return pathToPrintPattern;
        }

        public string GetXMLPrintPattern()
        {
            return XMLoPrintPattern;
        }
        public void initForLib(recogn_params defParams)
        {
            _params = defParams;
            Console.WriteLine();
        }

        public bool Update_result_data(result_data newResult)
        {
            _result.Enqueue(newResult);
            swNewDMResult(_result.Count);
            Console.WriteLine($"Update_result_data: {newResult.BOXs.Count()}");
            return true;
        }

        public bool Show_user_event(string text, string typeEvent)
        {
            alarmEvent(text, typeEvent);
            return true;
        }
    }

    public struct recogn_params
    {
        public int countOfDM = 0;//заполнить сколько на слое упаковок
        public int pixInMM = 10;//
        public string CamInterfaces = "GigEVision2";
        public string cameraName = "/img";
        public string CamPathToAddConf = "";
        public camera_preset _Preset = new camera_preset("Basler");
        public bool softwareTrigger = true;
        public bool hardwareTrigger = false;
        public bool OCRRecogn = false; //ocr распознавание текста
        public bool packRecogn = false; //если нужна коробка(если нужа ocr)
        public bool DMRecogn = false; //true если нужен датаматрикс

        public recogn_params()
        {
        }
    }

    public struct camera_preset
    {
        public string name;
        public string pathCSV;
        public string swTriggerVal;
        public camera_preset(string _name)
        {
            switch (_name)
            {
                case "Basler":
                    {
                        name = "Basler";
                        pathCSV = "basler.csv";
                        swTriggerVal = "trig"; //TODO
                        return;
                    }
            }
            name = string.Empty;
            pathCSV = string.Empty;
            swTriggerVal = string.Empty;

        }
    }

    public struct result_data
    {
        public Image rawImage;
        public Image processedImage;
        public List<BOX_data> BOXs;

        public void setDMDataList(List<BOX_data> _DMdataArr)
        {
            BOXs = _DMdataArr;
        }
    }

    public struct BOX_data
    {
        public int poseX;
        public int poseY;
        public int height;
        public int width;
        public int alpha;
        public string packType;
        public List<OCR_data> OCR;
        public DM_data DM;
        public bool isError;

        public BOX_data()
        {
            OCR = new List<OCR_data>();
            DM = new DM_data();
            poseX = 0;
            poseY = 0;
            height = 0;
            width = 0;
            alpha = 0;
        }
    }
    //распознавание текста
    public struct OCR_data
    {
        public string Text;
        public string Name;
        public int poseX;
        public int poseY;
        public int height;
        public int width;
        public int alpha;
        public bool isError;
    }

    public struct DM_data
    {
        public string data;
        public int poseX;
        public int poseY;
        public int height;
        public int width;
        public int alpha;
        public bool isError;
    }
}