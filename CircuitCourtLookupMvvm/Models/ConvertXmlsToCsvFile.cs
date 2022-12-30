using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CircuitCourtLookupMvvm.Utilities;

namespace CircuitCourtLookupMvvm.Models
{
    class ConvertXmlsToCsvFile
    {
        #region PROPERTIES
        public string Source { get; set; }
        public string Destination { get; set; }
        #endregion
        #region CONSTRUCTOR
        public ConvertXmlsToCsvFile(string src, string dest)
        {
            // assign properties
            Source = src;
            Destination = dest;

            // collect source files
            var _files = System.IO.Directory.EnumerateFiles(Source, "*.xml", System.IO.SearchOption.AllDirectories);

            // setup datatable (esp column headers)
            DataTable dt = InitiateQuadientDataTable();

            // track to eliminate duplicate Names & Addresses
            var attyNameHashSet = new HashSet<string>();
            var attyAddressHashSet = new HashSet<string>();

            // get data from previous week's file
            var archiveFileData = new CaptureDataFromArchiveXlxsFiles(Source).PreviousWeekFileData;

            var reasonElim = new ReasonsAttorneyRemovedFromCollection();

            // loop through xml files (each file -> datatable row)
            foreach (var _f in _files)
            {
                XDocument xml = null;
                try
                {
                    // create XDocument from xml file
                    xml = XDocument.Load(_f);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write(ex);
                    continue;
                }

                // get circuit court name & number
                string _circuit = string.Empty;
                var circuitNumberFromFileName = System.IO.Path.GetFileName(_f).Substring(0, 2);
                int out_num;
                if (int.TryParse(circuitNumberFromFileName, out out_num)) { _circuit = ConvertCircuitAbbrToString(out_num.ToString()); }
                else if (circuitNumberFromFileName.Equals("DC")) { _circuit = ConvertCircuitAbbrToString(circuitNumberFromFileName); }
                else { reasonElim.BadCircuitNumber++; continue; } // skip to next xml if filename is nonstandard

                // capture parent: stub (shortTitle)
                var _shortTitle = (from x in xml.Descendants("stub")
                                   select x)
                                   .ToList().First().Attribute("shortTitle").Value;

                // capture parent: caption
                var _caption = (from x in xml.Descendants("caption")
                                select x)
                                .ToList().First().Value;

                // added 4/6/2018: Excel cells cannot have more than 32,XXX characters
                var _cap_count = _caption.Count();
                if (_cap_count > 32000)
                {
                    _caption = _caption.Substring(0, 32000);
                }


                if (_caption.ToLower().Contains("full caption") || _caption.ToLower().Contains("for the caption"))
                { reasonElim.BadCaption++; continue; } // skip captions that refer to other cases

                // CAPTURE: attorney (loop through each attorney)
                var query_attorneys = from x in xml.Descendants("attorney")
                                      select x;

                // KEEP TRACK OF PARTYINFO/PARTYTYPE TO ELIMINATE DUPS (refreshes for every file)
                var party_info_included = new HashSet<string>();
                var party_type_included = new HashSet<string>();

                foreach (var atty in query_attorneys)
                {
                    // COLLECT ONLY 'Retained' ATTORNEYS, FILTER Pro Se and Government
                    if (!atty.Attribute("noticeInfo").Value.Contains("Retained")) { reasonElim.BadNoticeInfo++; continue; }

                    // FILTER EMPTY ZIP CODES
                    var zipCode = ParseZipCode(atty.Attribute("zip").Value);
                    if (string.IsNullOrEmpty(zipCode)) { reasonElim.BadZipCode++; continue; }

                    var party_info = atty.Parent.Attribute("info").Value;
                    var party_type = atty.Parent.Attribute("type").Value;

                    // skip parties that are not part of the appeal
                    if (!(party_type.ToLower().Contains("petitioner".ToLower())
                        || party_type.ToLower().Contains("respondent".ToLower())
                        || party_type.ToLower().Contains("appellant".ToLower())
                        || party_type.ToLower().Contains("appellee".ToLower())))
                    { reasonElim.BadPartyType++; continue; }

                    // skip parties whose side (or party info) is already represented
                    if (party_info_included.Contains(party_info)) { reasonElim.RepeatPartyInfo++; continue; }
                    party_info_included.Add(party_info);
                    //do the same for party type
                    if (party_type_included.Contains(party_type)) { reasonElim.RepeatPartyType++; continue; }
                    party_type_included.Add(party_type);

                    // collect datarow information
                    var dr_circuit = _circuit;

                    var dr_shortTitle = _shortTitle;

                    // remove et al
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "(, et al.?)(\\s)", "$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "(, et al.?)($)", "$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( et al.?)(\\s)", "$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( et al.?)($)", "$2");

                    // Fix up some common problems with Short Titles
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Ltd)(\\s)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Ltd)($)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Inc)(\\s)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Inc)($)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Corp)(\\s)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( Corp)($)", "$1.$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( LLC)(.)($)", "$1$3");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "( LLC)(.)(,)", "$1$3");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "(, [A-Z])($)", "$2");
                    dr_shortTitle = Regex.Replace(dr_shortTitle, "(,)(\\s)(v.)(\\s)", "$2$3$4");

                    // Add \r\n to Short Titles
                    dr_shortTitle = dr_shortTitle.Replace(" v. ", "\r\nv.\r\n");

                    // Keep full caption for comparison's sake !!!
                    var dr_caption = _caption;

                    // atty name
                    var dr_fullName = string.Format("{0}{1} {2}{3}",
                        atty.Attribute("firstName").Value,
                        string.IsNullOrEmpty(atty.Attribute("middleName").Value) ? "" : " " + atty.Attribute("middleName").Value,
                        atty.Attribute("lastName").Value,
                         string.IsNullOrWhiteSpace(atty.Attribute("generation").Value) ? "" : string.Format(", {0}", atty.Attribute("generation").Value));

                    // atty combined address
                    var atty_office = ParseLawFirmName(atty.Attribute("office").Value);
                    // added 6/2/17 (atty general names marked as [NTC Retained] were slipping through
                    if (atty_office.ToLower().Contains("attorney general")) { reasonElim.GovernmentAttorney++; continue; }
                    var atty_address1 = ParseAddress(atty.Attribute("address1").Value);
                    var atty_address2 = ParseAddress(atty.Attribute("address2").Value);
                    var atty_address3 = ParseAddress(atty.Attribute("address3").Value);
                    var atty_unit = ParseAddress(atty.Attribute("unit").Value);
                    var atty_room = ParseAddress(atty.Attribute("room").Value);
                    var atty_city = ToTitleCase(atty.Attribute("city").Value);
                    var atty_state = atty.Attribute("state").Value;
                    var atty_zip = zipCode; // already parsed

                    var dr_combinedAddress = parseCombinedAddress(
                        atty_office, atty_address1, atty_address2, atty_address3,
                        atty_unit, atty_room, atty_city, atty_state, atty_zip);

                    // skip duplicate Names and Addresses
                    if (attyNameHashSet.Contains(dr_fullName))
                    { reasonElim.RepeatFullName++; continue; }
                    if (attyAddressHashSet.Contains(dr_combinedAddress))
                    { reasonElim.RepeatFullAddress++; continue; }

                    // skip addresses within returned list
                    if (archiveFileData.Where(x => x.CombinedAddress.Replace("\n", "\r\n").Equals(dr_combinedAddress)).Count() > 0)
                    {
                        reasonElim.AddressInReturnedList++;
                        continue;
                    }

                    // keep track of hashsets
                    attyNameHashSet.Add(dr_fullName);
                    attyAddressHashSet.Add(dr_combinedAddress);

                    // create new datarow (after finished with filters)
                    var dr = dt.NewRow();


                    dr["Name"] = "";
                    dr["Company"] = "";
                    dr["Country"] = "US";
                    dr["SmsNumber"] = !string.IsNullOrEmpty(atty.Attribute("businessPhone").Value)
                        ? atty.Attribute("businessPhone").Value
                        : (!string.IsNullOrEmpty(atty.Attribute("personalPhone").Value)
                        ? atty.Attribute("personalPhone").Value
                        : "");
                    dr["FaxNumber"] = dr_shortTitle;
                    dr["email"] = atty.Attribute("email").Value;
                    dr["Language"] = "EN";
                    dr["Preferences"] = dr_shortTitle;
                    dr["AddressLine1"] = dr_fullName; // use name here
                    dr["AddressLine2"] = atty_address1;
                    dr["AddressLine3"] = atty_address2;
                    dr["AddressLine4"] = atty_address3;
                    dr["AddressLine5"] = atty_unit;
                    dr["AddressLine6"] = atty_unit;
                    dr["AddressLine7"] = dr_shortTitle;
                    dr["AddressLine8"] = dr_shortTitle;
                    dr["AddressLine9"] = dr_shortTitle;



                    dr["Name"] = dr_circuit;





                    dr["shortTitle"] = dr_shortTitle;
                    dr["caption"] = dr_caption;
                    dr["fullName"] = dr_fullName;
                    dr["combinedAddress"] = dr_combinedAddress;
                    dr["email"] = atty.Attribute("email").Value;


                    // ADD DATAROW TO DATATABLE
                    dt.Rows.Add(dr);
                    if (dt.Rows.Count == 200)
                    { }
                }
            }
            // REMOVE DUPS FROM PREVIOUS 10 WEEKS
            //var captureArchiveFiles = new CaptureDataFromArchiveXlxsFiles(Source).PreviousWeekFileData;
            //var countDataPoints = dt.Rows.Count;
            //dt = RemoveDuplicateRowsFromDataTableByName(dt, archiveFileData.Select(x => x.FullName));
            //countDataPoints = dt.Rows.Count;
            //dt = RemoveDuplicateRowsFromDataTableByCaseName(dt, archiveFileData.Select(x => x.Caption));
            //countDataPoints = dt.Rows.Count;
            //dt = RemoveDuplicateRowsFromDataTableByCombinedAddress(dt, archiveFileData.Select(x => x.CombinedAddress));
            //countDataPoints = dt.Rows.Count;

            // 2022: returnedLetters is throwing an error so commented out the process
            //var returnedLetters = new LettersReturned().ListOfAttyInfo;
            //dt = RemoveAddressesWithReturnedLetterMatch(dt, returnedLetters.Select(x => x.CombinedAddress));


            try
            {
                if (System.IO.File.Exists(Destination))
                {
                    System.IO.File.Delete(Destination);
                }
                dt.WriteToExcelFile(Destination);
            }
            catch (Exception excpt) { Console.WriteLine(excpt); }
            finally
            {
                var conversionInfoFilename = System.IO.Path.Combine(Source, "conversion_info.txt");
                using (var sw = new System.IO.StreamWriter(conversionInfoFilename))
                {
                    sw.WriteLine(reasonElim.ToString());
                }
            }
        }
        #endregion
        #region PRIVATE METHODS
        private string parseCombinedAddress(
            string office, string address1, string address2, string address3,
            string unit, string room, string city, string state, string zip)
        {
            var combinedAddress = string.Empty;
            combinedAddress = (string.IsNullOrEmpty(office)
                || Regex.IsMatch(office, "^Law Offices?$")) ? "" : office + "\r\n";

            combinedAddress += string.IsNullOrEmpty(address1) ? "" : address1;

            // loop through secondary address lines
            string[] address_array = { address2, address3, unit, room };
            var lenAddressMax = 45;
            var lenAddressLine = address1.Length;
            foreach (var itm in address_array)
            {
                if (!string.IsNullOrEmpty(itm))
                {
                    if (lenAddressLine + itm.Length <= lenAddressMax)
                    {
                        combinedAddress += ", " + itm;
                    }
                    else
                    {
                        combinedAddress += "\r\n" + itm;
                    }
                    lenAddressLine += itm.Length;
                }
            }

            // add city, state zip
            combinedAddress += "\r\n" + city + ", " + state + " " + zip;

            return combinedAddress;
        }

        /**
         * CsvAddressBookTemplate.csv:
         * ClientId;Name;Company;Country;SmsNumber;FaxNumber;Email;Language;Preferences;AddressLine1;AddressLine2;AddressLine3;AddressLine4;AddressLine5;AddressLine6;AddressLine7;AddressLine8;AddressLine9
         * 
         * CsvMailMergeTemplate.csv
         * Address line 1;Address line 2;Address line 3;Address line 4;Address line 5;Address line 6;Recipient country ISO code;Name
         * 
         * EXAMPLE DATA CsvMailMergeTemplate.csv
         * Address line 1;Address line 2;Address line 3;Address line 4;Address line 5;Address line 6;Recipient country ISO code;Name
         * Neopost Denver; 10065 E Harvard Ave #200; Denver CO 80231;;;;US;Service Tech
         * Jon Kamrath;875 Wheelers Farms Rd;Milford CT 06461;;;;US;Quadient
         * Adam Davenport;123 Main St;Apt 14;Milford CT 06461;;;US;Quadient
         * 
         */
        private DataTable InitiateQuadientDataTable()
        {
            DataTable dt = new DataTable();

            //dt.Columns.Add(new DataColumn("ClientId", typeof(string)));
            dt.Columns.Add(new DataColumn("Name", typeof(string)));
            dt.Columns.Add(new DataColumn("Company", typeof(string)));
            dt.Columns.Add(new DataColumn("Country", typeof(string)));
            dt.Columns.Add(new DataColumn("SmsNumber", typeof(string)));
            dt.Columns.Add(new DataColumn("FaxNumber", typeof(string)));
            dt.Columns.Add(new DataColumn("Email", typeof(string)));
            dt.Columns.Add(new DataColumn("Language", typeof(string)));
            dt.Columns.Add(new DataColumn("Preferences", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine1", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine2", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine3", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine4", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine5", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine6", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine7", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine8", typeof(string)));
            dt.Columns.Add(new DataColumn("AddressLine9", typeof(string)));

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

            dic.Add(@"(^|\s)(Mcguirewoods)(,|\s|$)", "$1McGuireWoods$3");
            dic.Add(@"(^|\s)(Beharbehar)(\s|$)", "$1BeharBehar$3");
            dic.Add(@"(^|\s)(Kjc)(\s|$)", "$1KJC$3");
            dic.Add(@"(^|\s)(Aclu)(\s|$)", "$1ACLU$3");
            dic.Add(@"(\s)(The)(\s|$)", "$1the$3");
            dic.Add(@"(^|\s)(Afl-Cio)(\s|$)", "$1AFL-CIO$3");

            dic.Add(@"(^.*)(Esq.|Esquire)(.*$)", "");


            foreach (var d in dic)
            {
                // USEFUL FOR TESTING
                //if(Regex.IsMatch(newFirmName, d.Key)){Console.WriteLine("!");}

                // REPLACE EACH DICTIONARY ITEM
                newFirmName = Regex.Replace(newFirmName, d.Key, d.Value);

                // check for apostrophe (O'Keefe, etc.)
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
            // check for good zip codes
            bool goodZipCode;

            string newZip = origZip.Replace(" ", "");
            string pattern = @"(^)([0-9]{5}|[0-9]{5}-[0-9]{4})($)";
            var m = Regex.Match(newZip, pattern);
            goodZipCode = m.Success;

            if (goodZipCode)
            {
                // remove 00000
                if (newZip.Equals("00000"))
                    newZip = "";
                // remove 00000-0000
                else if (newZip.Equals("00000-0000"))
                    newZip = "";
                // or just -0000
                else if (newZip.Contains("-0000"))
                    newZip = newZip.Remove(5);
            }

            return goodZipCode ? newZip : "";
        }
        private string ParseAddress(string origAddr)
        {
            string newAddr = origAddr;
            newAddr = newAddr.Replace("&nbsp;", " ");
            while (newAddr.Contains("  ")) { newAddr = newAddr.Replace("  ", " "); }

            // IF ALL CAPS, CONVERT TO TITLE CASE
            if (!Regex.Match(origAddr, "[a-z]").Success) { newAddr = ToTitleCase(newAddr); }

            var dic = new Dictionary<string, string>();

            // Suite, Floor, Room (acct for plural 'Stes')
            dic.Add(@"(^|\s)([Ss]uite)(\s|$)", "$1Suite$3");
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
                //if(Regex.IsMatch(newAddr, d.Key)){Console.WriteLine("!");}

                // REPLACE EACH DICTIONARY ITEM
                newAddr = Regex.Replace(newAddr, d.Key, d.Value);

                // check for apostrophe (O'Keefe, etc.)
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
        #endregion
    }
}
