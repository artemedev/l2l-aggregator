using DM_wraper_NS;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace DM_process_lib
{
    internal class MainProces : IDM_process
    {
        public MainProces()
        {
        }
        public void MP_TakeShot()
        {
            Thread th_pShot = new Thread(_shot_proces);
            th_pShot.IsBackground = true;
            th_pShot.Start();
        }

        private void _shot_proces()
        {
            Console.WriteLine("MainProces : Take a shot");

            Dictionary<string, bool> dataModel = ExtractDataModelFromFRX();
            List<BOX_data> lDMD = new List<BOX_data>();
            string baseDir = Path.GetDirectoryName(typeof(MainProces).Assembly.Location)!;
            string filePath = Path.Combine(baseDir, "result.json");
            string jsonData = File.ReadAllText(filePath);
            ResultData resultData = JsonConvert.DeserializeObject<ResultData>(jsonData);
            foreach (Cell cell in resultData.cells)
            {
                BOX_data dataCell = CreateDataCellFromModel(dataModel, cell);
                lDMD.Add(dataCell);
            }


            result_data dmrd = new result_data();
            dmrd.BOXs = lDMD;
            string imagePath = Path.Combine(baseDir, "image_raw.jpg");
            byte[] imageBytes = File.ReadAllBytes(imagePath);
            using (MemoryStream ms = new MemoryStream(imageBytes))
            {
                dmrd.rawImage = Image.Load<Rgba32>(ms);
            }
            Console.WriteLine("Add new DM codes");
            _DMP._dM_recogn_wraper.Update_result_data(dmrd);

        }
        /// <summary>
        /// Отдельная функция, которая формирует BOX_data, заполняет его списком OCR 
        /// на основе dataModel и массива predefinedData
        /// </summary>
        private BOX_data CreateDataCellFromModel(Dictionary<string, bool> dataModel, Cell cell)
        {
            BOX_data dataCell = new BOX_data
            {
                poseX = cell.poseX,
                poseY = cell.poseY,
                width = cell.width,
                height = cell.height,
                alpha = cell.angle,
                packType = "PackageType",
                isError = false,
                OCR = new List<OCR_data>(),
                DM = new DM_wraper_NS.DM_data
                {
                    data = cell.cell_dm?.data,
                    poseX = cell.cell_dm?.poseX ?? 0,
                    poseY = cell.cell_dm?.poseY ?? 0,
                    width = cell.cell_dm?.width ?? 0,
                    height = cell.cell_dm?.height ?? 0,
                    alpha = cell.cell_dm?.angle ?? 0,
                    isError = cell.cell_dm?.isError ?? false
                }
            };


            foreach (Cell_OCR entry in cell.cell_ocr)
            {
                //var nameLower = entry.name?.ToLowerInvariant();
                //var dataLower = entry.data?.ToLowerInvariant();

                //bool shouldAdd = dataModel.Any(kvp =>
                //    (kvp.Value && kvp.Key.ToLowerInvariant() == nameLower) ||
                //    (!kvp.Value && kvp.Key.ToLowerInvariant() == dataLower)
                //);

                //if (shouldAdd)
                //{
                    dataCell.OCR.Add(new OCR_data
                    {
                        Text = entry.data,
                        Name = entry.name,
                        poseX = entry.poseX,
                        poseY = entry.poseY,
                        width = entry.width,
                        height = entry.height,
                        alpha = entry.angle,

                    });
                //}

            }
            return dataCell;
        }
        public XDocument OriginalDocument { get; private set; }
        private Dictionary<string, bool> ExtractDataModelFromFRX()
        {
            Dictionary<string, bool> dataModel = new Dictionary<string, bool>();
            string base64Frx = _DMP._pathToPrintPattern;

            if (string.IsNullOrEmpty(base64Frx))
            {
                Console.WriteLine("Base64-строка для FRX не задана, создаём пустую модель.");
                dataModel["Default"] = false;
                return dataModel;
            }

            try
            {
                string xmlString;

                if (IsBase64String(base64Frx))
                {
                    byte[] frxBytes = Convert.FromBase64String(base64Frx);
                    xmlString = Encoding.UTF8.GetString(frxBytes);
                }
                else
                {
                    xmlString = base64Frx;
                }

                using (var reader = new StringReader(xmlString))
                {
                    OriginalDocument = XDocument.Load(reader);
                }

                dataModel = ExtractFieldsFromReport(OriginalDocument, "LabelQry");



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки FastReport: {ex.Message}");
            }

            return dataModel;
        }

        private bool IsBase64String(string s)
        {
            s = s.Trim();
            return (s.Length % 4 == 0) &&
                   System.Text.RegularExpressions.Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,2}$");
        }
        private static Dictionary<string, bool> ExtractFieldsFromReport(XDocument reportDocument, string prefix)
        {
            var resultFields = new Dictionary<string, bool>();

            // Найдём все элементы, у которых Type = "variable" и задан Name
            var variableElements = reportDocument
                .Descendants()
                .Where(el =>
                    el.Attribute("Type")?.Value == "variable" &&
                    !string.IsNullOrWhiteSpace(el.Attribute("Name")?.Value)
                );

            foreach (var element in variableElements)
            {
                var name = element.Attribute("Name")?.Value?.Trim();
                if (!string.IsNullOrEmpty(name) && !resultFields.ContainsKey(name))
                {
                    resultFields[name] = true;
                }
            }

            return resultFields;
        }
        private class ResultData
        {
            public List<Cell> cells { get; set; }
        }

        private class Cell
        {
            public int cell_id { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int angle { get; set; }
            public DM_data cell_dm { get; set; }
            public List<Cell_OCR> cell_ocr { get; set; }
        }
        public class DM_data
        {
            public string data { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int height { get; set; }
            public int width { get; set; }
            public int angle { get; set; }
            public bool isError { get; set; }
        }
        private class Cell_OCR
        {
            public string data { get; set; }
            public string name { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int angle { get; set; }

        }
    }
}
