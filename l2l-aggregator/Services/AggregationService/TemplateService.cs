using l2l_aggregator.Helpers.AggregationHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace l2l_aggregator.Services.AggregationService
{
    public class TemplateService
    {
        public XDocument OriginalDocument { get; private set; }

        public List<TemplateField> LoadTemplateFromBase64(string base64Template)
        {
            var fields = new List<TemplateField>();

            if (string.IsNullOrEmpty(base64Template))
                return fields;

            try
            {
                byte[] templateBytes = Convert.FromBase64String(base64Template);
                using (var memoryStream = new MemoryStream(templateBytes))
                {
                    OriginalDocument = XDocument.Load(memoryStream);
                }

                var templatePage = OriginalDocument.Descendants()
                    .FirstOrDefault(e => e.Name.LocalName == "TfrxTemplatePage");

                if (templatePage == null)
                    return fields;

                foreach (var element in templatePage.Elements())
                {
                    string elementType = element.Name.LocalName;
                    if (elementType.StartsWith("Tfrx") || elementType.StartsWith("Template"))
                    {
                        var nameAttr = element.Attribute("Name");
                        var textAttr = element.Attribute("Text");
                        var dataFieldAttr = element.Attribute("DataField");

                        if (nameAttr != null)
                        {
                            if (dataFieldAttr != null)
                            {
                                fields.Add(new TemplateField
                                {
                                    Name = dataFieldAttr.Value,
                                    Type = "переменная",
                                    Element = element,
                                    IsSelected = true
                                });
                            }
                            else if (textAttr != null)
                            {
                                fields.Add(new TemplateField
                                {
                                    Name = textAttr.Value,
                                    Type = "текст",
                                    Element = element,
                                    IsSelected = true
                                });
                            }
                        }
                    }
                }

                return fields;
            }
            catch
            {
                return new List<TemplateField>();
            }
        }

        public string GenerateTemplateBase64(List<TemplateField> fields)
        {
            if (OriginalDocument == null || fields.Count == 0)
                return string.Empty;

            var newDocument = new XDocument(OriginalDocument);

            // Очистка ненужного
            newDocument.Descendants("Datasets").Remove();
            newDocument.Descendants("Variables").Remove();

            var dataPage = newDocument.Descendants().FirstOrDefault(e => e.Name.LocalName == "TfrxDataPage");
            if (dataPage != null)
            {
                var allowedAttrs = new HashSet<string> { "Name", "Height", "Left", "Top", "Width" };
                foreach (var attr in dataPage.Attributes().Where(a => !allowedAttrs.Contains(a.Name.LocalName)).ToList())
                {
                    attr.Remove();
                }
            }

            var templatePage = newDocument.Descendants().FirstOrDefault(e => e.Name.LocalName == "TfrxTemplatePage");
            if (templatePage == null)
                return string.Empty;

            // Очистка атрибутов страницы
            foreach (var attr in templatePage.Attributes().Where(a => string.IsNullOrWhiteSpace(a.Value) || a.Value == "0").ToList())
            {
                attr.Remove();
            }

            // Удаление невыбранных полей
            var fieldsToRemove = new List<XElement>();
            foreach (var element in templatePage.Elements())
            {
                var field = fields.FirstOrDefault(f => f.Element.Attribute("Name")?.Value == element.Attribute("Name")?.Value);
                if (field != null && !field.IsSelected)
                {
                    fieldsToRemove.Add(element);
                }
                else if (field != null)
                {
                    var allowedAttrs = new HashSet<string>
                    {
                        "Name", "Left", "Top", "Width", "Height", "Font.Name", "Text", "DataField"
                    };

                    foreach (var attr in element.Attributes().Where(a => !allowedAttrs.Contains(a.Name.LocalName)).ToList())
                    {
                        attr.Remove();
                    }
                }
            }

            foreach (var el in fieldsToRemove)
            {
                el.Remove();
            }

            return EncodeToBase64(newDocument);
        }

        private string EncodeToBase64(XDocument doc)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                doc.Save(ms);
                return Convert.ToBase64String(ms.ToArray());
            }
        }
    }
}
