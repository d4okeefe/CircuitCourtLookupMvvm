using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CircuitCourtLookupMvvm.Utilities;
using System.Xml.Linq;
using System.Globalization;
using System.Text.RegularExpressions;

namespace CircuitCourtLookupMvvm.Models
{
    class ConvertXmlsToXlsxFileWithExtendedAddresses
    {
        // properties
        public string Source { get; set; }
        public string Destination { get; set; }
        //fields
        private string[] possibleSubfolderNames = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "DC", "Fed" };
        // constructor

        public ConvertXmlsToXlsxFileWithExtendedAddresses(string src, string dest)
        {
            Source = src;
            Destination = dest;
            // get source files
            var _files = System.IO.Directory.EnumerateFiles(Source, "*.*", System.IO.SearchOption.AllDirectories);

            // Create DataTable
            DataTable dt = InitiateCircuitCourtDataTable();

            // Cycle through files
            int fileNumber = 0;
            int totalFiels = _files.Count();
            foreach (var _f in _files)
            {
                ++fileNumber;
                // skip if not xml
                if (!_f.EndsWith(".xml")) continue;

                XDocument xml = null;
                try
                {
                    // create XDocument from xml file
                    xml = XDocument.Load(_f);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                    continue;
                }

                // GET CIRCUIT COURT NAME & NUMBER
                string _circuit = string.Empty;
                var circuitNumberFromFileName = System.IO.Path.GetFileName(_f).Substring(0, 2);
                int out_num;
                if (int.TryParse(circuitNumberFromFileName, out out_num)) { _circuit = ConvertCircuitAbbrToString(out_num.ToString()); }
                else if (circuitNumberFromFileName.Equals("DC")) { _circuit = ConvertCircuitAbbrToString(circuitNumberFromFileName); }
                else { continue; }

                // capture items from stub parent
                var query_stub = from x in xml.Descendants("stub") select x;
                var _caseNumber = query_stub.ToList()[0].Attribute("caseNumber").Value;

                // capture item from caption parent
                var query_caption = from x in xml.Descendants("caption") select x;

                // capture shortTitle
                // capture parent: stub (shortTitle)
                var dr_shortTitle = (from x in xml.Descendants("stub")
                                   select x)
                                   .ToList().First().Attribute("shortTitle").Value;

                // capture attorney item and cycle through them
                var query_attorneys = from x in xml.Descendants("attorney")
                                      select x;
                foreach (var atty in query_attorneys)
                {
                    try
                    {
                        // create new datarow
                        DataRow dr = dt.NewRow();

                        // get party info for this atty
                        dr["partyInfo"] = atty.Parent.Attribute("info").Value;
                        dr["partyType"] = atty.Parent.Attribute("type").Value;

                        dr["shortTitle"] = dr_shortTitle;

                        // data for this case
                        dr["circuit"] = _circuit;
                        dr["caseNumber"] = _caseNumber;

                        // NAME
                        dr["firstName"] = atty.Attribute("firstName").Value;
                        dr["middleName"] = atty.Attribute("middleName").Value;
                        dr["lastName"] = atty.Attribute("lastName").Value;
                        dr["generation"] = atty.Attribute("generation").Value; // Jr. II III etc.
                        dr["fullName"] = string.Format("{0}{1} {2}{3}",
                            atty.Attribute("firstName").Value,
                            string.IsNullOrEmpty(atty.Attribute("middleName").Value) ? "" : " " + atty.Attribute("middleName").Value,
                            atty.Attribute("lastName").Value,
                            string.IsNullOrWhiteSpace(atty.Attribute("generation").Value) ? "" : string.Format(", {0}", atty.Attribute("generation").Value));

                        // UNUSED INFORMATION
                        dr["suffix"] = atty.Attribute("suffix").Value; // Esq. Esquire
                        dr["title"] = atty.Attribute("title").Value; // 'Attorney' 'Managing Counsel' 'Of Counsel'
                                                                     //dr["terminationDate"] = atty.Attribute("terminationDate").Value;
                        dr["noticeInfo"] = atty.Attribute("noticeInfo").Value;

                        // PARSE LAW FIRM NAME
                        dr["office"] = ParseLawFirmName(atty.Attribute("office").Value);

                        // PARSE ADDRESSES
                        dr["address1"] = ParseAddress(atty.Attribute("address1").Value);
                        dr["address2"] = ParseAddress(atty.Attribute("address2").Value);
                        dr["address3"] = ParseAddress(atty.Attribute("address3").Value);
                        dr["zip"] = atty.Attribute("zip").Value;
                        dr["unit"] = ParseAddress(atty.Attribute("unit").Value);
                        dr["room"] = ParseAddress(atty.Attribute("room").Value);

                        // OTHER INFORMATION
                        dr["email"] = atty.Attribute("email").Value;
                        dr["fax"] = atty.Attribute("fax").Value;
                        dr["businessPhone"] = atty.Attribute("businessPhone").Value;
                        dr["personalPhone"] = !string.IsNullOrEmpty(atty.Attribute("businessPhone").Value) ? "" : atty.Attribute("personalPhone").Value;
                        dr["city"] = ToTitleCase(atty.Attribute("city").Value);
                        dr["state"] = atty.Attribute("state").Value;

                        // Get date collected
                        dr["dateCollected"] = DateTime.Now.ToString("MM/dd/yyyy");

                        if (blockDuplicateEntries(dt, dr)) { continue; }

                        // ADD DATAROW TO DATATABLE
                        dt.Rows.Add(dr);
                    }
                    catch (Exception excpt)
                    {
                        Console.WriteLine(excpt);
                    }

                }
            }

            try
            {
                if (System.IO.File.Exists(Destination))
                {
                    System.IO.File.Delete(Destination);
                }
                //dt.WriteToCsvFile(Destination);
                dt.WriteToExcelFile(Destination);
            }
            catch (Exception excpt) { Console.WriteLine(excpt); return; }
        }
        private bool blockDuplicateEntries(DataTable dt, DataRow dr)
        {
            foreach (DataRow row in dt.Rows)
            {
                if (
                    row.Field<string>("firstName").Equals(dr["firstName"]) &&
                    row.Field<string>("middleName").Equals(dr["middleName"]) &&
                    row.Field<string>("lastName").Equals(dr["lastName"]) &&
                    row.Field<string>("address1").Equals(dr["address1"]) &&
                    row.Field<string>("address2").Equals(dr["address2"]) &&
                    row.Field<string>("address3").Equals(dr["address3"])
                    )
                {
                    return true;
                }
            }

            return false;
        }
        private DataTable InitiateCircuitCourtDataTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add(new DataColumn("circuit", typeof(string)));
            dt.Columns.Add(new DataColumn("caseNumber", typeof(string)));
            dt.Columns.Add(new DataColumn("partyInfo", typeof(string)));
            dt.Columns.Add(new DataColumn("partyType", typeof(string)));

            dt.Columns.Add(new DataColumn("shortTitle", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyInfoOpposing", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyTypeOpposing", typeof(string)));

            dt.Columns.Add(new DataColumn("firstName", typeof(string)));
            dt.Columns.Add(new DataColumn("middleName", typeof(string)));
            dt.Columns.Add(new DataColumn("lastName", typeof(string)));
            dt.Columns.Add(new DataColumn("fullName", typeof(string)));
            dt.Columns.Add(new DataColumn("generation", typeof(string)));
            dt.Columns.Add(new DataColumn("suffix", typeof(string)));
            dt.Columns.Add(new DataColumn("title", typeof(string)));
            dt.Columns.Add(new DataColumn("email", typeof(string)));
            dt.Columns.Add(new DataColumn("fax", typeof(string)));
            dt.Columns.Add(new DataColumn("address1", typeof(string)));
            dt.Columns.Add(new DataColumn("address2", typeof(string)));
            dt.Columns.Add(new DataColumn("address3", typeof(string)));
            dt.Columns.Add(new DataColumn("office", typeof(string)));
            dt.Columns.Add(new DataColumn("unit", typeof(string)));
            dt.Columns.Add(new DataColumn("room", typeof(string)));
            dt.Columns.Add(new DataColumn("businessPhone", typeof(string)));
            dt.Columns.Add(new DataColumn("personalPhone", typeof(string)));
            dt.Columns.Add(new DataColumn("city", typeof(string)));
            dt.Columns.Add(new DataColumn("state", typeof(string)));
            dt.Columns.Add(new DataColumn("zip", typeof(string)));
            dt.Columns.Add(new DataColumn("terminationDate", typeof(string)));
            dt.Columns.Add(new DataColumn("noticeInfo", typeof(string)));
            dt.Columns.Add(new DataColumn("dateCollected", typeof(string)));

            return dt;
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
        private string ToTitleCase(string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }
        private string ParseLawFirmName(string origFirmName)
        {
            string newFirmName = origFirmName;
            // check for all caps: if so, change to title case
            if (!Regex.Match(newFirmName, "[a-z]").Success) { newFirmName = ToTitleCase(newFirmName); }

            var dic = new Dictionary<string, string>();
            dic.Add(@"(^|\s)(Llp)(,|\s|$)", "$1LLP$3");
            dic.Add(@"(^|\s)(Pllp)(,|\s|$)", "$1PLLP$3");
            dic.Add(@"(^|\s)(Pc)(,|\s|$)", "$1PC$3");
            dic.Add(@"(^|\s)(Pllc)(,|\s|$)", "$1PLLC$3");
            dic.Add(@"(^|\s)(Pa)(,|\s|$)", "$1PA$3");
            dic.Add(@"(^|\s)(Plc)(,|\s|$)", "$1PLC$3");
            dic.Add(@"(^|\s)(Llc)(,|\s|$)", "$1LLC$3");
            dic.Add(@"(^|\s)(Lpa)(,|\s|$)", "$1LPA$3");

            dic.Add(@"(\s)(Of)(\s|$)", "$1of$3");
            dic.Add(@"(\s)(At)(\s|$)", "$1at$3");
            dic.Add(@"(\s)(For)(\s|$)", "$1for$3");
            dic.Add(@"(\s)(And)(\s|$)", "$1&$3");

            dic.Add(@"(^|\s)(Mcguirewoods)(\s|$)", "$1McGuireWoods$3");
            dic.Add(@"(^|\s)(Beharbehar)(\s|$)", "$1BeharBehar$3");
            dic.Add(@"(^|\s)(Kjc)(\s|$)", "$1KJC$3");
            dic.Add(@"(^|\s)(Aclu)(\s|$)", "$1ACLU$3");
            dic.Add(@"(\s)(The)(\s|$)", "$1the$3");
            dic.Add(@"(^|\s)(Afl-Cio)(\s|$)", "$1AFL-CIO$3");

            dic.Add(@"(^.*)(Esq.|Esquire)(.*$)", "");


            foreach (var d in dic)
            {
                // USEFUL FOR TESTING
                //var match = Regex.IsMatch(newFirmName, d.Key);
                //if(match){Console.WriteLine("!");}
                newFirmName = Regex.Replace(newFirmName, d.Key, d.Value);

                // check for apostrophe
                var m = Regex.Match(newFirmName, "\\w'[a-z]{2,}");
                if (m.Success)
                {
                    int indexOf = newFirmName.IndexOf('\'') + 1;
                    char letter = newFirmName[indexOf];
                    letter = Char.ToUpper(letter);
                    StringBuilder sb = new StringBuilder(newFirmName);
                    sb[indexOf] = letter;
                    newFirmName = sb.ToString();
                }
            }
            return newFirmName;
        }
        private string ParseZipCode(string origZip)
        {
            string newZip = origZip.Replace(" ", "");
            string pattern = @"(^)([0-9]{5}|[0-9]{5}-[0-9]{4})($)";
            var m = Regex.Match(newZip, pattern);
            if (m.Success) return newZip;
            else return "";
        }
        private string ParseAddress(string origAddr)
        {
            string newAddr = origAddr;
            newAddr = newAddr.Replace("&nbsp;", " ");
            while (newAddr.Contains("  ")) { newAddr = newAddr.Replace("  ", " "); }

            // check for all caps: if so, change to title case
            if (!Regex.Match(origAddr, "[a-z]").Success) { newAddr = ToTitleCase(newAddr); }

            var dic = new Dictionary<string, string>();

            // Additions ...
            dic.Add(@"(^|\s)([Ss]uite)(\s|$)", "$1Suite$3");

            // Suite, Floor, Room (acct for plural 'Stes')
            dic.Add(@"(^|\s)(Ste)(s?)(,?)(\s|$)", "$1$2$3.$4$5");
            dic.Add(@"(^|\s)(Fl|Flr|Rm)(,?)(\s|$)", "$1$2.$3$4");
            // add comma before suite
            dic.Add(@"([A-Za-z]|\.)(\s)(Ste.)", "$1,$2$3");
            // St, Rd, Ave, etc
            dic.Add(@"(^|\s)(St)(s?)(,?)(\s|$)", "$1$2$3.$4$5"); // acct for plural
            dic.Add(@"(^|\s)(Rd|Ave|Pkwy|Dr|Cir|Ln|Hwy|Sq|Blvd|Twr|Bldg|Ctr|Pl|Plz)(,?)(\s|$)", "$1$2.$3$4");
            // directions
            dic.Add(@"(^|\s)(W|E|N|S)(,?)(\s|$)", "$1$2.$3$4");
            // 1st, 2nd, 3rd
            dic.Add(@"(^|\s)([0-9]{1,})(Th|TH)(\s|$)", "$1$2th$4");
            dic.Add(@"(^|\s)([0-9]{1,})*(1{1,})(St|ST)(\s|$)", "$1$2$3st$4");
            dic.Add(@"(^|\s)([0-9]{1,})*(2{1,})(Nd|ND)(\s|$)", "$1$2$3nd$4");
            // PO Box
            dic.Add(@"(Po)(\s)(Box|Drawer)", "PO$2$3");

            foreach (var d in dic)
            {
                // USEFUL FOR TESTING
                //var match = Regex.IsMatch(newFirmName, d.Key);
                //if(match){Console.WriteLine("!");}
                newAddr = Regex.Replace(newAddr, d.Key, d.Value);

                // check that first letter is capital ???

                // check for apostrophe
                var m = Regex.Match(newAddr, "\\w'[a-z]{2,}");
                if (m.Success)
                {
                    int indexOf = newAddr.IndexOf('\'') + 1;
                    char letter = newAddr[indexOf];
                    letter = Char.ToUpper(letter);
                    StringBuilder sb = new StringBuilder(newAddr);
                    sb[indexOf] = letter;
                    newAddr = sb.ToString();
                }
            }
            return newAddr;
        }
    }
}
