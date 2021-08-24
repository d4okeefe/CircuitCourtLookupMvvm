using System;
using System.Collections.Generic;

namespace CircuitCourtLookupMvvm.Viewmodels
{
    public class DataFromXmlFiles
    {
        public string Circuit { get; set; }
        public string FileName { get; set; }
        public string DocketNumber { get; set; }
        public List<DocketTexts> DocketTexts { get; set; }
    }

    public class DocketTexts
    {
        public string DocketLink { get; set;}
        public DateTime DateOfText { get; set; }
        public string TextInfo { get; set; }
    }
}