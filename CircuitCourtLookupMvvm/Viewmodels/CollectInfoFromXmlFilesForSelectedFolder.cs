using System.Collections.Generic;
using System.Collections.ObjectModel;
using CircuitCourtLookupMvvm.Models;
using System.Xml.Linq;
using System.Linq;

namespace CircuitCourtLookupMvvm.Viewmodels
{
    internal class CollectInfoFromXmlFilesForSelectedFolder
    {
        public List<DataFromXmlFiles> CollectedXmlData { get; internal set; }

        public CollectInfoFromXmlFilesForSelectedFolder(string dir_name)
        {
            var pacer_directory = string.Empty;
            foreach (var d in System.IO.Directory.EnumerateDirectories(dir_name))
            {
                if (d.ToLower().Contains("pacer"))
                {
                    pacer_directory = d;
                }
            }
            if(string.IsNullOrEmpty(pacer_directory))
            {
                return;
            }

            var files = System.IO.Directory.EnumerateFiles(pacer_directory, "*.xml");

            CollectedXmlData = new List<DataFromXmlFiles>();

            foreach (var file in files)
            {


                var xml = XDocument.Load(file);

                // GET CIRCUIT COURT NAME & NUMBER
                string _circuit = string.Empty;
                var circuitNumberFromFileName = System.IO.Path.GetFileName(file).Substring(0, 2);
                int out_num;
                if (int.TryParse(circuitNumberFromFileName, out out_num)) { _circuit = ConvertCircuitAbbrToString(out_num.ToString()); }
                else if (circuitNumberFromFileName.Equals("DC")) { _circuit = ConvertCircuitAbbrToString(circuitNumberFromFileName); }
                else { continue; }


                // capture items from stub parent
                var query_stub = from x in xml.Descendants("stub") select x;
                var _caseNumber = query_stub.ToList()[0].Attribute("caseNumber").Value;

                var dataFromXmlFiles = new DataFromXmlFiles
                {
                    FileName = file,
                    Circuit = _circuit,
                    DocketNumber = _caseNumber
                };

                var list_of_texts = new List<DocketTexts>();

                // capture text
                var query_docketTexts = from x in xml.Descendants("docketText") select x;
                foreach (var q in query_docketTexts)
                {
                    var date = q.Attribute("dateFiled").Value;
                    var date_parsed = date.Split('/');
                    var text = q.Attribute("text").Value;
                    var docLink = q.Attribute("docLink").Value;

                    var docketText = new DocketTexts
                    {
                        DateOfText = new System.DateTime(int.Parse(date_parsed[2]), int.Parse(date_parsed[0]), int.Parse(date_parsed[1])),
                        TextInfo = text,
                        DocketLink = docLink
                    };

                    list_of_texts.Add(docketText);
                }

                if (list_of_texts.Count > 0) { dataFromXmlFiles.DocketTexts = list_of_texts; }

                CollectedXmlData.Add(dataFromXmlFiles);
            }
        }

        private string ConvertCircuitAbbrToString(string circuit)
        {
            if (circuit.Equals("1")) return "First Circuit";
            if (circuit.Equals("2")) return "Second Circuit";
            if (circuit.Equals("3")) return "Third Circuit";
            if (circuit.Equals("4")) return "Fourth Circuit";
            if (circuit.Equals("5")) return "Fifth Circuit";
            if (circuit.Equals("6")) return "Sixth Circuit";
            if (circuit.Equals("7")) return "Seventh Circuit";
            if (circuit.Equals("8")) return "Eighth Circuit";
            if (circuit.Equals("9")) return "Ninth Circuit";
            if (circuit.Equals("10")) return "Tenth Circuit";
            if (circuit.Equals("11")) return "Eleventh Circuit";
            if (circuit.Equals("DC")) return "District of Columbia Circuit";
            if (circuit.Equals("13")) return "Federal Circuit";
            if (circuit.Equals("Fed")) return "Federal Circuit";
            return "";
        }
    }
}