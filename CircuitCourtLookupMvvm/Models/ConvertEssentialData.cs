using CircuitCourtLookupMvvm.Utilities;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CircuitCourtLookupMvvm.Models
{
    /**
     * GOALS:
     * 1. Capture xml data from latest folder -- just the essentials: Name, Firm, Email, Phone, Circuit, Case
     * 2. Extract data
     * 
     * What kind of data --
     * 1. Quadient Emails --
     *      Case Number: caseNumber
     *      Case Name: shortTitle
     *      Attorney Name: fullName
     *      Email Address: email
     *      language
     *      country
     */

    internal class ConvertEssentialData
    {
        private string Source;
        private string SourceShortName;
        public string Destination;
        private readonly string FOLDER_CIRCUITCOURTS = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters";

        private string US_COUNTRY_CODE = "US";
        private string ENG_LANGUAGE_CODE = "en-us";

        public ConvertEssentialData(string src, string shortSrc, string dest)
        {
            this.Source = src;
            this.SourceShortName = shortSrc;
            this.Destination = dest;

            // get source files
            var _files = System.IO.Directory.EnumerateFiles(Source, "*.*", System.IO.SearchOption.AllDirectories);

            // Track and eliminate duplicates
            HashSet<string> _temp_attorneys = new HashSet<string>();
            HashSet<string> _temp_emails = new HashSet<string>();
            HashSet<string> _temp_attorney_pro_se_not_in_prison = new HashSet<string>();

            // Get unsubs from Google sheet
            var unsubscribed = new UnsubscribeGoogleSheet();
            unsubscribed.Unsubscribed_Names.ForEach(x => _temp_attorneys.Add(x));
            unsubscribed.Unsubscribed_Emails.ForEach(x => _temp_emails.Add(x));

            // Get archives of email addresses back to Week 193
            GetArchive_AttysAndEmails(_temp_attorneys, _temp_emails);

            // Create DataTables -- each of these will be a sheet in the Excel Workbook
            DataTable dt_all = InitiateAllInfoCircuitCourtDataTable();
            DataTable dt = InitiateCircuitCourtDataTable(); // MAIN SHEET -- NOT SURE IF THIS IS USED ANYMORE
            DataTable dt_email = InitiateEmailDataTable();
            DataTable dt_email_lookup = InitiateEmailLookupDataTable();
            DataTable dt_pro_se = InitiateAllInfoCircuitCourtDataTable();
            DataTable dt_pro_se_not_in_prison = InitiateAllInfoCircuitCourtDataTable();
            DataTable dt_pro_se_sms = InitiateSmsCircuitCourtDataTable();

            // Capture data from each xml & insert into table
            // (1)  Major loop through each xml file
            // (2)  Minor loop through attorney names in each xml
            int totalFiles = _files.Count();
            foreach (var _file in _files)
            {
                if (!_file.EndsWith(".xml")) continue;

                XDocument xml = null;
                try { xml = XDocument.Load(_file); }
                catch (Exception ex) { Console.WriteLine(ex); continue; }

                // GET DATA TRUE FOR ALL ATTORNEYS (outside of atty loop)
                // circuit court name & number
                string _circuit = string.Empty;
                var circuitNumberFromFileName = System.IO.Path.GetFileName(_file).Substring(0, 2);
                int out_num;
                if (int.TryParse(circuitNumberFromFileName, out out_num)) { _circuit = ConvertCircuitAbbrToString(out_num.ToString()); }
                else if (circuitNumberFromFileName.Equals("DC")) { _circuit = ConvertCircuitAbbrToString(circuitNumberFromFileName); }
                // capture items from stub parent
                var query_stub = from x in xml.Descendants("stub") select x;
                var _caseNumber = query_stub.ToList()[0].Attribute("caseNumber").Value;
                // capture item from caption parent
                var query_caption = from x in xml.Descendants("caption") select x;
                // capture parent: stub (shortTitle)
                var dr_shortTitle = (from x in xml.Descendants("stub") select x)
                                   .ToList().First().Attribute("shortTitle").Value;
                dr_shortTitle = ShortenCaseTitle(dr_shortTitle);


                // LOOP THROUGH EACH ATTORNEY
                // capture attorney item and cycle through them
                var query_attorneys = from x in xml.Descendants("attorney") select x;
                foreach (var atty in query_attorneys)
                {
                    try
                    {
                        var guid = Guid.NewGuid().ToString();

                        // CREATE DATAROW FOR dt_all
                        DataRow dr_all = dt_all.NewRow();
                        CreateDataRowAllData(_circuit, _caseNumber, dr_shortTitle, atty, dr_all);
                        var fullNameString = dr_all["fullName"].ToString();
                        if (!string.IsNullOrWhiteSpace(fullNameString))
                        {
                            dt_all.Rows.Add(dr_all);
                        }
                        else
                        {
                            // skip dr_all row if no name
                            Console.WriteLine("no name!");
                        }
                        var isProSe = dr_all["noticeInfo"].ToString();
                        if (isProSe.ToLower().Contains("pro se"))
                        {
                            // CREATE DATAROW FOR dt_pro_se
                            DataRow dr_pro_se = dt_pro_se.NewRow();
                            CreateDataRowAllData(_circuit, _caseNumber, dr_shortTitle, atty, dr_pro_se);
                            dt_pro_se.Rows.Add(dr_pro_se);

                            var office_check = dr_all["office"].ToString();
                            var inPrison = string.IsNullOrWhiteSpace(office_check) ? false : InPrisonOffice(office_check);
                            var proSeFullName = dr_all["fullName"].ToString();
                            if (!inPrison && !_temp_attorney_pro_se_not_in_prison.Contains(proSeFullName))
                            {
                                // ALSO -- PREVENT DUPLICATES; CREATE HASHSET TO MANAGE THIS (3/18 STILL HAVEN'T DONE THIS)
                                // CREATE DATAROW FOR dt_pro_se_not_in_prison
                                DataRow dr_pro_se_not_in_prison = dt_pro_se_not_in_prison.NewRow();
                                CreateDataRowAllData(_circuit, _caseNumber, dr_shortTitle, atty, dr_pro_se_not_in_prison);
                                dt_pro_se_not_in_prison.Rows.Add(dr_pro_se_not_in_prison);

                                _temp_attorney_pro_se_not_in_prison.Add(proSeFullName);

                                // CREATE DATAROW FOR dr_pro_se_sms
                                if (!string.IsNullOrWhiteSpace(dr_all["phoneFormatted"].ToString()))
                                {
                                    DataRow dr_pro_se_sms = dt_pro_se_sms.NewRow();
                                    dr_pro_se_sms["guid"] = guid;
                                    dr_pro_se_sms["fullName"] = dr_pro_se_not_in_prison["fullName"];
                                    dr_pro_se_sms["phoneFormatted"] = dr_pro_se_not_in_prison["phoneFormatted"];
                                    dr_pro_se_sms["language"] = ENG_LANGUAGE_CODE;
                                    dr_pro_se_sms["country"] = US_COUNTRY_CODE;
                                    dt_pro_se_sms.Rows.Add(dr_pro_se_sms);
                                }
                            }
                        } //  end pro se database rows


                        // CREATE ROW IN (OLD) MAIN SHEET
                        DataRow dr = dt.NewRow();
                        dr["circuit"] = _circuit;
                        dr["caseNumber"] = _caseNumber;
                        dr["shortTitle"] = dr_shortTitle;
                        var noticeInfo = atty.Attribute("noticeInfo").Value;
                        dr["noticeInfo"] = noticeInfo;
                        var fullName = string.Format("{0}{1} {2}{3}",
                            atty.Attribute("firstName").Value,
                            string.IsNullOrEmpty(atty.Attribute("middleName").Value) ? "" : " " + atty.Attribute("middleName").Value,
                            atty.Attribute("lastName").Value,
                            string.IsNullOrWhiteSpace(atty.Attribute("generation").Value) ? "" : string.Format(", {0}", atty.Attribute("generation").Value));


                        // CRITICAL PASSAGE -- SKIP IF EMPTY OR DUPLICATE
                        if (string.IsNullOrWhiteSpace(fullName))
                        {
                            continue;
                        }
                        if (_temp_attorneys.Contains(fullName))
                        {
                            continue;
                        }
                        else
                        {
                            _temp_attorneys.Add(fullName);
                            dr["fullName"] = fullName;
                        }

                        // PARSE LAW FIRM NAME
                        var office = ParseLawFirmName(atty.Attribute("office").Value);
                        // SKIP IF GOV OR PUB DEF
                        if (IsGovOrPubDef(office))
                        {
                            continue;
                        }
                        dr["office"] = office;

                        var email = atty.Attribute("email").Value;
                        if (!string.IsNullOrWhiteSpace(email))
                        {
                            if (email.EndsWith(".gov")) { continue; }
                            if (_temp_emails.Contains(email))
                            {
                                continue;
                            }
                            else
                            {
                                _temp_emails.Add(email);
                                dr["email"] = email;
                            }


                            dr["phone"] = !string.IsNullOrEmpty(atty.Attribute("personalPhone").Value) ? "" : atty.Attribute("businessPhone").Value;

                            dr["city"] = ToTitleCase(atty.Attribute("city").Value);
                            dr["state"] = atty.Attribute("state").Value;

                            dr["dateCollected"] = DateTime.Now.ToString("MM/dd/yyyy");

                            dr["language"] = ENG_LANGUAGE_CODE;
                            dr["country"] = US_COUNTRY_CODE;

                            // ADD DATAROW TO DATATABLE
                            dt.Rows.Add(dr);




                            // EXTRACT EMAIL DATA FROM ROW IN (OLD) MAIN SHEET
                            DataRow dr_email = dt_email.NewRow();
                            dr_email["guid"] = guid;
                            dr_email["caseNumber"] = dr["caseNumber"];
                            dr_email["shortTitle"] = dr["shortTitle"];
                            dr_email["fullName"] = dr["fullName"];
                            dr_email["email"] = dr["email"];
                            dr_email["language"] = dr["language"];
                            dr_email["country"] = dr["country"];
                            dt_email.Rows.Add(dr_email);
                        }
                        else
                        {
                            // CAPTURE DATA FOR EMAIL LOOKUP ( Attys without email entries in Pacer)
                            if (email.EndsWith(".gov")) { continue; }
                            var ofc = dr["office"].ToString();
                            if (InPrisonOffice(ofc)) { continue; }
                            if (IsGovOrPubDef(ofc)) { continue; }


                            DataRow dr_email_lookup = dt_email_lookup.NewRow();
                            dr_email_lookup["circuit"] = _circuit;
                            dr_email_lookup["caseNumber"] = dr["caseNumber"];
                            dr_email_lookup["shortTitle"] = dr["shortTitle"];
                            dr_email_lookup["fullName"] = dr["fullName"];
                            dr_email_lookup["email"] = "";
                            dr_email_lookup["office"] = dr["office"];
                            dr_email_lookup["address1"] = dr_all["address1"];
                            dr_email_lookup["address2"] = dr_all["address2"];
                            dr_email_lookup["city"] = dr_all["city"];
                            dr_email_lookup["state"] = dr_all["state"];
                            dr_email_lookup["guid"] = guid;
                            dr_email_lookup["language"] = ENG_LANGUAGE_CODE;
                            dr_email_lookup["country"] = US_COUNTRY_CODE;

                            dt_email_lookup.Rows.Add(dr_email_lookup);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                    }
                } // END ATTORNEY LOOP
            } // END XML FILE LOOP


            try
            {
                if (System.IO.File.Exists(Destination))
                {
                    System.IO.File.Delete(Destination);
                }
                //dt.WriteToCsvFile(Destination);
                //dt.WriteToExcelFile(Destination);
                CreateOrUpdateExcelFile(dt, "main", Destination);
                CreateOrUpdateExcelFile(dt_email, "email", Destination);
                CreateOrUpdateExcelFile(dt_email_lookup, "email_lookup", Destination);
                CreateOrUpdateExcelFile(dt_pro_se_not_in_prison, "pro_no_not_in_prison", Destination);
                CreateOrUpdateExcelFile(dt_pro_se_sms, "dt_pro_se_sms", Destination);
                CreateOrUpdateExcelFile(dt_pro_se, "pro_se", Destination);
                CreateOrUpdateExcelFile(dt_all, "all", Destination);




            }
            catch (Exception excpt) { Console.WriteLine(excpt); return; }

        }

        private bool InPrisonOffice(string office)
        {
            var off = office.ToLower();

            if (off.Contains("FCI".ToLower())) { return true; }
            if (off.Contains("F.C.I.".ToLower())) { return true; }
            if (off.Contains("SCI".ToLower())) { return true; }
            if (off.Contains("S.C.I.".ToLower())) { return true; }
            if (off.Contains("FDC".ToLower())) { return true; }
            if (off.Contains("F.D.C.".ToLower())) { return true; }
            if (off.Contains("FMC".ToLower())) { return true; }
            if (off.Contains("F.M.C.".ToLower())) { return true; }
            if (off.Contains("MDC".ToLower())) { return true; }
            if (off.Contains("M.D.C.".ToLower())) { return true; }
            if (off.Contains("USP".ToLower())) { return true; }
            if (off.Contains("U.S.P.".ToLower())) { return true; }
            if (off.Contains("CID".ToLower())) { return true; }
            if (off.Contains("C.I.D.".ToLower())) { return true; }
            if (off.Contains("CI McRae".ToLower())) { return true; }
            if (off.Contains("Corecivic".ToLower())) { return true; }
            if (off.Contains("Correction".ToLower())) { return true; }
            if (off.Contains("Penitentiary".ToLower())) { return true; }
            if (off.Contains("Jail".ToLower())) { return true; }
            if (off.Contains("Prison".ToLower())) { return true; }
            if (off.Contains("Detention".ToLower())) { return true; }
            if (off.Contains("Treatment".ToLower())) { return true; }
            if (off.Contains("Federal".ToLower())) { return true; }
            if (off.Contains("Inmate".ToLower())) { return true; }
            if (off.Contains("Institution".ToLower())) { return true; }
            if (off.Contains("Commitment".ToLower())) { return true; }
            if (off.Contains("Offender".ToLower())) { return true; }
            if (off.Contains("Reformatory".ToLower())) { return true; }
            if (off.Contains("Facility".ToLower())) { return true; }
            if (off.Contains("Attorney General".ToLower())) { return true; }
            if (off.Contains("Justice Center".ToLower())) { return true; }
            if (off.Contains("Health Center".ToLower())) { return true; }
            if (off.Contains("Attorney's Office".ToLower())) { return true; }
            if (off.Contains("U.S. Attorney".ToLower())) { return true; }
            if (off.Contains("Sheriff's Office".ToLower())) { return true; }

            return false;
        }

        private void CreateDataRowAllData(string _circuit, string _caseNumber, string dr_shortTitle, XElement atty, DataRow dr_all)
        {
            try
            {
                dr_all["circuit"] = _circuit;
                dr_all["caseNumber"] = _caseNumber;
                dr_all["shortTitle"] = dr_shortTitle;
                dr_all["partyInfo"] = atty.Parent.Attribute("info").Value;
                dr_all["partyType"] = atty.Parent.Attribute("type").Value;
                dr_all["firstName"] = atty.Attribute("firstName").Value;
                dr_all["middleName"] = atty.Attribute("middleName").Value;
                dr_all["lastName"] = atty.Attribute("lastName").Value;
                dr_all["generation"] = atty.Attribute("generation").Value; // Jr. II III etc.
                dr_all["fullName"] = string.Format("{0}{1} {2}{3}",
                    atty.Attribute("firstName").Value,
                    string.IsNullOrEmpty(atty.Attribute("middleName").Value) ? "" : " " + atty.Attribute("middleName").Value,
                    atty.Attribute("lastName").Value,
                    string.IsNullOrWhiteSpace(atty.Attribute("generation").Value) ? "" : string.Format(", {0}", atty.Attribute("generation").Value));
                dr_all["suffix"] = atty.Attribute("suffix").Value; // Esq. Esquire
                dr_all["title"] = atty.Attribute("title").Value; // 'Attorney' 'Managing Counsel' 'Of Counsel'
                                                                 //dr["terminationDate"] = atty.Attribute("terminationDate").Value;
                dr_all["noticeInfo"] = atty.Attribute("noticeInfo").Value;
                dr_all["office"] = ParseLawFirmName(atty.Attribute("office").Value);
                dr_all["address1"] = ParseAddress(atty.Attribute("address1").Value);
                dr_all["address2"] = ParseAddress(atty.Attribute("address2").Value);
                dr_all["address3"] = ParseAddress(atty.Attribute("address3").Value);
                dr_all["zip"] = atty.Attribute("zip").Value;
                dr_all["unit"] = ParseAddress(atty.Attribute("unit").Value);
                dr_all["room"] = ParseAddress(atty.Attribute("room").Value);
                dr_all["email"] = atty.Attribute("email").Value;
                dr_all["fax"] = atty.Attribute("fax").Value;
                dr_all["businessPhone"] = atty.Attribute("businessPhone").Value;
                dr_all["personalPhone"] = !string.IsNullOrEmpty(atty.Attribute("businessPhone").Value) ? "" : atty.Attribute("personalPhone").Value;

                var phoneFormatted = Regex.Replace(dr_all["personalPhone"].ToString(), @"[^\d]", "");

                dr_all["phoneFormatted"] = string.IsNullOrWhiteSpace(phoneFormatted) ? "" : "+1" + phoneFormatted;

                dr_all["city"] = ToTitleCase(atty.Attribute("city").Value);
                dr_all["state"] = atty.Attribute("state").Value;
                dr_all["dateCollected"] = DateTime.Now.ToString("MM/dd/yyyy");
            }
            catch (Exception ex)
            {
                Console.Write(ex);
            }
        }

        private void CreateOrUpdateExcelFile(DataTable data_table, string sheet_name, string dest_filename)
        {
            //if (!System.IO.File.Exists(dest_filename))

            var file_info = new System.IO.FileInfo(dest_filename);

            // Save to excel
            using (ExcelPackage package = new ExcelPackage(file_info))
            {
                ExcelWorksheet ws = package.Workbook.Worksheets.Add(sheet_name);

                ws.Cells["A1"].LoadFromDataTable(data_table, true);
                package.Save();
            }
        }
        /**
         * GetArchive_AttysAndEmails
         * 
         * Loop through Top-Level Archive Folders & Get (1) Attorney Full Names & (2) Email Addresses
         *   - None of these names & emails should appear in current marketing
         *   - Top Level Folder is '\\clbdc03\Public\Letters\Circuit_Court_Letters'
         */
        private void GetArchive_AttysAndEmails(HashSet<string> _temp_attorneys, HashSet<string> _temp_emails)
        {
            var current_week = Int32.Parse(Regex.Match(SourceShortName, @"\d+").Value);
            var archive_folders = new List<string>();
            var top_level_folders = System.IO.Directory.EnumerateDirectories(FOLDER_CIRCUITCOURTS);
            for (var i = current_week - 1; i >= 211; i--)
            {
                var preceding_week = top_level_folders.First(x => x.Contains(i.ToString()));
                var pacer_folder = System.IO.Directory.EnumerateDirectories(preceding_week).First(x => x.ToLower().Contains("pacer"));
                var _archive_files = System.IO.Directory.EnumerateFiles(pacer_folder, "*.*", System.IO.SearchOption.AllDirectories);
                foreach (var _archive_file in _archive_files)
                {
                    if (!_archive_file.EndsWith(".xml")) continue;

                    XDocument xml = null;
                    try
                    {
                        // create XDocument from xml file
                        xml = XDocument.Load(_archive_file);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine(ex);
                        continue;
                    }
                    var query_archive_attorneys = from x in xml.Descendants("attorney") select x;


                    foreach (var atty in query_archive_attorneys)
                    {
                        try
                        {
                            // ATTORNEY NAME
                            var fullName = string.Format("{0}{1} {2}{3}",
                                atty.Attribute("firstName").Value,
                                string.IsNullOrEmpty(atty.Attribute("middleName").Value) ? "" : " " + atty.Attribute("middleName").Value,
                                atty.Attribute("lastName").Value,
                                string.IsNullOrWhiteSpace(atty.Attribute("generation").Value) ? "" : string.Format(", {0}", atty.Attribute("generation").Value));
                            // SKIP IF EMPTY OR DUPLICATE
                            if (string.IsNullOrWhiteSpace(fullName))
                            {
                                continue;
                            }
                            if (_temp_attorneys.Contains(fullName))
                            {
                                continue;
                            }
                            else
                            {
                                _temp_attorneys.Add(fullName);
                            }
                            var email = atty.Attribute("email").Value;
                            if (!string.IsNullOrWhiteSpace(email))
                            {
                                _temp_emails.Add(email);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }
            }
        }
        /**
         * ShortenCaseTitle
         * 
         * Quadient will not accept entries > 50 characters
         * This is an attempt to logically shorten the text
         */
        private static string ShortenCaseTitle(string dr_shortTitle)
        {
            if (dr_shortTitle.Length >= 50)
            {
                dr_shortTitle = dr_shortTitle.Replace(", et al.", "");
                dr_shortTitle = dr_shortTitle.Replace(", et al", "");
            }
            if (dr_shortTitle.Length >= 50)
            {
                dr_shortTitle = dr_shortTitle.Replace(", LLC", "");
                dr_shortTitle = dr_shortTitle.Replace(", Incorporated", ", Inc.");
                dr_shortTitle = dr_shortTitle.Replace("Association", "Ass'n");
            }
            if (dr_shortTitle.Length >= 50)
            {
                dr_shortTitle = dr_shortTitle.Replace("Insurance Company", "Ins. Co.");
                dr_shortTitle = dr_shortTitle.Replace("Employees Insurance", "Emps. Ins.");
            }
            if (dr_shortTitle.Length >= 50)
            {
                dr_shortTitle = dr_shortTitle.Replace("The Estate", "Estate");
            }
            if (dr_shortTitle.Length >= 50)
            {
                dr_shortTitle = dr_shortTitle.Replace("Corporation", "Corp.");
                dr_shortTitle = dr_shortTitle.Replace("Company", "Co.");
                dr_shortTitle = dr_shortTitle.Replace("National", "Nat'l");
                dr_shortTitle = dr_shortTitle.Replace("International", "Int'l");
                dr_shortTitle = dr_shortTitle.Replace("United States of America", "U.S.A.");
                dr_shortTitle = dr_shortTitle.Replace("United States", "U.S.");
            }
            if (dr_shortTitle.Length >= 50)
            {
                Console.WriteLine(dr_shortTitle);
            }

            return dr_shortTitle;
        }

        private bool IsGovOrPubDef(string office)
        {
            // 1. Government Offices
            if (office.ToLower().Contains("US Attorney".ToLower())) { return true; }
            if (office.ToLower().Contains("U.S. Attorney".ToLower())) { return true; }
            if (office.ToLower().Contains("Department of Justice".ToLower())) { return true; }
            if (office.ToLower().Contains("Dept of Justice".ToLower())) { return true; }
            if (office.ToLower().Contains("United States".ToLower())) { return true; }
            if (office.ToLower().Contains("Attorney General".ToLower())) { return true; }
            if (office.ToLower().Contains("Corporation Counsel".ToLower())) { return true; }
            if (office.ToLower().Contains("District Attorney".ToLower())) { return true; }
            if (office.ToLower().Contains("Attorney Service".ToLower())) { return true; }
            if (office.ToLower().Contains("General Counsel".ToLower())) { return true; }
            if (office.ToLower().Contains("Federal Communications Commission".ToLower())) { return true; }
            if (office.ToLower().Contains("Department of".ToLower())) { return true; }
            if (office.ToLower().Contains("Merit Systems Protection Board".ToLower())) { return true; }
            if (office.ToLower().Contains("DOJ".ToLower())) { return true; }
            if (office.ToLower().Contains("Internal Revenue Service".ToLower())) { return true; }
            if (office.ToLower().Contains("IRS".ToLower())) { return true; }
            if (office.ToLower().Contains("State Bar".ToLower())) { return true; }
            if (office.ToLower().Contains("Social Security".ToLower())) { return true; }
            if (office.ToLower().Contains("County Counsel".ToLower())) { return true; }
            if (office.ToLower().Contains("National Labor Relations Board".ToLower())) { return true; }

            // 2. Public Defender
            if (office.ToLower().Contains("Public Defender".ToLower())) { return true; }
            if (office.ToLower().Contains("Defender Office".ToLower())) { return true; }
            if (office.ToLower().Contains("Federal Defender".ToLower())) { return true; }
            if (office.ToLower().Contains("Community Defender".ToLower())) { return true; }

            return false;
        }

        private DataTable InitiateSmsCircuitCourtDataTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add(new DataColumn("guid", typeof(string)));
            dt.Columns.Add(new DataColumn("fullName", typeof(string)));
            dt.Columns.Add(new DataColumn("phoneFormatted", typeof(string)));
            dt.Columns.Add(new DataColumn("language", typeof(string)));
            dt.Columns.Add(new DataColumn("country", typeof(string)));

            return dt;
        }
        private DataTable InitiateAllInfoCircuitCourtDataTable()
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

            dt.Columns.Add(new DataColumn("phoneFormatted", typeof(string)));

            dt.Columns.Add(new DataColumn("city", typeof(string)));
            dt.Columns.Add(new DataColumn("state", typeof(string)));
            dt.Columns.Add(new DataColumn("zip", typeof(string)));
            dt.Columns.Add(new DataColumn("terminationDate", typeof(string)));
            dt.Columns.Add(new DataColumn("noticeInfo", typeof(string)));
            dt.Columns.Add(new DataColumn("dateCollected", typeof(string)));

            return dt;
        }

        private DataTable InitiateEmailLookupDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("circuit", typeof(string)));
            dt.Columns.Add(new DataColumn("caseNumber", typeof(string)));
            dt.Columns.Add(new DataColumn("shortTitle", typeof(string)));
            dt.Columns.Add(new DataColumn("fullName", typeof(string)));

            dt.Columns.Add(new DataColumn("email", typeof(string)));

            dt.Columns.Add(new DataColumn("office", typeof(string)));
            dt.Columns.Add(new DataColumn("address1", typeof(string)));
            dt.Columns.Add(new DataColumn("address2", typeof(string)));
            dt.Columns.Add(new DataColumn("city", typeof(string)));
            dt.Columns.Add(new DataColumn("state", typeof(string)));

            dt.Columns.Add(new DataColumn("guid", typeof(string)));
            dt.Columns.Add(new DataColumn("language", typeof(string)));
            dt.Columns.Add(new DataColumn("country", typeof(string)));
            return dt;
        }
        private DataTable InitiateEmailDataTable()
        {
            DataTable dt = new DataTable();
            dt.Columns.Add(new DataColumn("guid", typeof(string)));
            dt.Columns.Add(new DataColumn("caseNumber", typeof(string)));
            dt.Columns.Add(new DataColumn("shortTitle", typeof(string)));
            dt.Columns.Add(new DataColumn("fullName", typeof(string)));
            dt.Columns.Add(new DataColumn("email", typeof(string)));
            dt.Columns.Add(new DataColumn("language", typeof(string)));
            dt.Columns.Add(new DataColumn("country", typeof(string)));
            return dt;
        }
        private DataTable InitiateCircuitCourtDataTable()
        {
            DataTable dt = new DataTable();

            dt.Columns.Add(new DataColumn("circuit", typeof(string)));
            dt.Columns.Add(new DataColumn("caseNumber", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyInfo", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyType", typeof(string)));

            dt.Columns.Add(new DataColumn("shortTitle", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyInfoOpposing", typeof(string)));
            //dt.Columns.Add(new DataColumn("partyTypeOpposing", typeof(string)));

            //dt.Columns.Add(new DataColumn("firstName", typeof(string)));
            //dt.Columns.Add(new DataColumn("middleName", typeof(string)));
            //dt.Columns.Add(new DataColumn("lastName", typeof(string)));
            dt.Columns.Add(new DataColumn("fullName", typeof(string)));
            //dt.Columns.Add(new DataColumn("generation", typeof(string)));
            //dt.Columns.Add(new DataColumn("suffix", typeof(string)));
            //dt.Columns.Add(new DataColumn("title", typeof(string)));
            dt.Columns.Add(new DataColumn("email", typeof(string)));
            //dt.Columns.Add(new DataColumn("fax", typeof(string)));
            //dt.Columns.Add(new DataColumn("address1", typeof(string)));
            //dt.Columns.Add(new DataColumn("address2", typeof(string)));
            //dt.Columns.Add(new DataColumn("address3", typeof(string)));
            dt.Columns.Add(new DataColumn("office", typeof(string)));
            //dt.Columns.Add(new DataColumn("unit", typeof(string)));
            //dt.Columns.Add(new DataColumn("room", typeof(string)));
            //dt.Columns.Add(new DataColumn("businessPhone", typeof(string)));
            //dt.Columns.Add(new DataColumn("personalPhone", typeof(string)));
            dt.Columns.Add(new DataColumn("phone", typeof(string)));
            dt.Columns.Add(new DataColumn("city", typeof(string)));
            dt.Columns.Add(new DataColumn("state", typeof(string)));
            //dt.Columns.Add(new DataColumn("zip", typeof(string)));
            //dt.Columns.Add(new DataColumn("terminationDate", typeof(string)));
            dt.Columns.Add(new DataColumn("noticeInfo", typeof(string)));
            dt.Columns.Add(new DataColumn("dateCollected", typeof(string)));

            dt.Columns.Add(new DataColumn("language", typeof(string)));
            dt.Columns.Add(new DataColumn("country", typeof(string)));
            return dt;
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
        private string ToTitleCase(string s)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLower());
        }
    }
}