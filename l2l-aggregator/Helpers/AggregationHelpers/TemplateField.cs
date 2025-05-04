using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace l2l_aggregator.Helpers.AggregationHelpers
{
    public class TemplateField : ObservableObject
    {
        private string _name = string.Empty;
        private string _type = string.Empty;
        private bool _isSelected = true;
        private XElement _element;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }

        public XElement Element
        {
            get => _element;
            set => SetProperty(ref _element, value);
        }
    }
}
