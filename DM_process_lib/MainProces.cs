using DM_wraper_NS;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
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
                OCR = new List<OCR_data>()
            };


            foreach (Cell_OCR entry in cell.cell_ocr)
            {
                var nameLower = entry.name?.ToLowerInvariant();
                var dataLower = entry.data?.ToLowerInvariant();

                bool shouldAdd = dataModel.Any(kvp =>
                    (kvp.Value && kvp.Key.ToLowerInvariant() == nameLower) ||
                    (!kvp.Value && kvp.Key.ToLowerInvariant() == dataLower)
                );

                if (shouldAdd)
                {
                    dataCell.OCR.Add(new OCR_data
                    {
                        Text = entry.data,
                        Name = entry.name,
                        poseX = entry.poseX,
                        poseY = entry.poseY,
                        width = entry.width,
                        height = entry.height,
                        alpha = entry.alpha,

                    });
                }

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
                byte[] frxBytes = Convert.FromBase64String(base64Frx);


                // Загружаем FastReport из памяти
                using (var memoryStream = new MemoryStream(frxBytes))
                {
                    OriginalDocument = XDocument.Load(memoryStream);
                }

                dataModel = ExtractFieldsFromReport(OriginalDocument, "LabelQry");



            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки FastReport: {ex.Message}");
            }

            return dataModel;
        }


        private static Dictionary<string, bool> ExtractFieldsFromReport(XDocument reportDocument, string prefix)
        {
            var resultFields = new Dictionary<string, bool>();

            // 1. Все элементы с атрибутом DataField
            var elementsWithDataField = reportDocument
                .Descendants()
                .Where(el => el.Attribute("DataField") != null);

            foreach (var element in elementsWithDataField)
            {
                var dataField = element.Attribute("DataField")?.Value;
                if (!string.IsNullOrEmpty(dataField) && !resultFields.ContainsKey(dataField))
                {
                    resultFields[dataField] = true;
                }
            }

            // 2. Элементы без DataField, но с Text — пробуем найти в тексте шаблоны
            var elementsWithText = reportDocument
                .Descendants()
                .Where(el => el.Attribute("DataField") == null && el.Attribute("Text") != null);

            var regex = new Regex(@"\[(.*?)\]|""(.*?)""");

            foreach (var element in elementsWithText)
            {
                var text = element.Attribute("Text")?.Value;
                if (!string.IsNullOrEmpty(text))
                {
                    var matches = regex.Matches(text);
                    foreach (Match match in matches)
                    {
                        var value = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
                        if (!string.IsNullOrEmpty(value) && !resultFields.ContainsKey(value))
                        {
                            resultFields[value] = false;
                        }
                    }
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

            public List<Cell_OCR> cell_ocr { get; set; }
        }

        private class Cell_OCR
        {
            public string data { get; set; }
            public string name { get; set; }
            public int poseX { get; set; }
            public int poseY { get; set; }
            public int width { get; set; }
            public int height { get; set; }
            public int alpha { get; set; }

        }
    }
}
