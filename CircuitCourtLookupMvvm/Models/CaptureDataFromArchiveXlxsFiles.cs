using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using OfficeOpenXml;

namespace CircuitCourtLookupMvvm.Models
{
    internal class CaptureDataFromArchiveXlxsFiles
    {
        // properties
        public HashSet<string> attys { get; set; }
        public HashSet<string> caseCaptions { get; set; }
        public HashSet<string> caseShortCaptions { get; set; }
        public DataTable ArchiveDataTable { get; set; }
        public List<PrecedingWeekData> PreviousWeekFileData { get; set; }

        // fields
        private readonly string FOLDER_CIRCUITCOURTS = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters";
        private readonly string ARCHIVE_CIRCUITCOURTS = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters\Archive_of_Pacer_Files";
        private string _circuitFolderCurrent;

        // constructor
        // plan: get xls files from previous 10 weeks
        public CaptureDataFromArchiveXlxsFiles(string circuitFolderCurrent)
        {
            _circuitFolderCurrent = circuitFolderCurrent;
            PreviousWeekFileData = new List<PrecedingWeekData>();

            // get current folder week number
            var circuitFolderCurrentShortName = System.Text.RegularExpressions.Regex.Replace(System.IO.Path.GetFileName(circuitFolderCurrent), @"\D+", "");
            int circuitFolderCurrentWeekNumber;
            if (!int.TryParse(circuitFolderCurrentShortName, out circuitFolderCurrentWeekNumber)) { /*exit if not found*/ }

            var dirInCircuitFolderMain = System.IO.Directory.EnumerateDirectories(FOLDER_CIRCUITCOURTS);
            var dirInCircuitFolderArchive = System.IO.Directory.EnumerateDirectories(ARCHIVE_CIRCUITCOURTS);

            // collect folders from previous four weeks
            var previousWeekFolders = new List<System.IO.DirectoryInfo>();
            var previousWeekXlsxFiles = new List<System.IO.FileInfo>();
            for (int previousWeekNumber = circuitFolderCurrentWeekNumber - 1;
                previousWeekNumber >= circuitFolderCurrentWeekNumber - 11;
                previousWeekNumber--)
            {
                var found = false;
                foreach (var subDirInMain in dirInCircuitFolderMain)
                {
                    var subFolder = System.IO.Path.GetFileName(subDirInMain);
                    if (subFolder.Contains(previousWeekNumber.ToString()))
                    {
                        var previousWeekDir = new System.IO.DirectoryInfo(subDirInMain);
                        if (previousWeekDir.EnumerateFiles("*datafile_new.xlsx").Count() == 1)
                        {
                            previousWeekFolders.Add(previousWeekDir);
                            found = true;

                            var xlsx = previousWeekDir.EnumerateFiles("*datafile_new.xlsx").First();
                            previousWeekXlsxFiles.Add(xlsx);
                        }
                    }
                }
                if (!found)
                {
                    foreach (var subDirInArchive in dirInCircuitFolderArchive)
                    {
                        var subFolder = System.IO.Path.GetFileName(subDirInArchive);
                        if (subFolder.Contains(previousWeekNumber.ToString()))
                        {
                            var previousWeekDir = new System.IO.DirectoryInfo(subDirInArchive);
                            previousWeekFolders.Add(previousWeekDir);
                            found = true;

                            var xlsx = previousWeekDir.EnumerateFiles("*datafile_new.xlsx").First();
                            previousWeekXlsxFiles.Add(xlsx);
                        }
                    }
                }
                found = false;
            }

            // get data from previous 10 weeks sent xlsx files
            foreach (var _file in previousWeekXlsxFiles)
            {
                // place preceding week data in object
                PreviousWeekFileData.AddRange(collectDataFromExcelFile(_file));
            }





            //var archiveFolders = Directory.EnumerateDirectories(
            //    @"\\CLBDC0\Public\Letters\Circuit_Court_Letters\Archive_of_Pacer_Files\");

            //var folder_list = new List<ArchiveCircuitCourtFolder>();

            //foreach (var f in archiveFolders)
            //{
            //    var new_item = new ArchiveCircuitCourtFolder(f);
            //    if (new_item.FolderNumber != null)
            //    {
            //        folder_list.Add(new_item);
            //    }
            //}

            //var latestThreeFolders = folder_list
            //    .OrderByDescending(o => o.FolderNumber)
            //    .Take<ArchiveCircuitCourtFolder>(4);


            //attys = new HashSet<string>();
            //caseCaptions = new HashSet<string>();
            //caseShortCaptions = new HashSet<string>();
            //ArchiveDataTable = new DataTable();

            //foreach (var f in latestThreeFolders)
            //{
            //    var file = f.FolderName + @"\datafile.csv";

            //    if (System.IO.File.Exists(file))
            //    {
            //        var table = CreateDataTableFromCsvFile(file);

            //        ArchiveDataTable.Merge(table);

            //        var attys_from_file = CaptureDataFromDatatable(table, "fullName");
            //        attys_from_file.ForEach(a => attys.Add(a));

            //        var cases_from_file = CaptureDataFromDatatable(table, "caption");
            //        cases_from_file.ForEach(a => caseCaptions.Add(a));

            //        var shortCases_from_file = CaptureDataFromDatatable(table, "shortTitle");
            //        shortCases_from_file.ForEach(a => caseShortCaptions.Add(a));


            //    }
            //}
        }

        private List<PrecedingWeekData> collectDataFromExcelFile(FileInfo _file)
        {
            var precedingWeekDataList = new List<PrecedingWeekData>();
            using (var excelPackage = new OfficeOpenXml.ExcelPackage())
            {
                using (var excelFileStream = System.IO.File.OpenRead(_file.FullName))
                {
                    excelPackage.Load(excelFileStream);
                }
                var ws = excelPackage.Workbook.Worksheets.First();
                DataTable dt = new DataTable();
                bool hasHeader = true;
                foreach (var firstRowCell in ws.Cells[1, 1, 1, ws.Dimension.End.Column])
                {
                    dt.Columns.Add(hasHeader ? firstRowCell.Text
                        : string.Format("Column {0}", firstRowCell.Start.Column));
                }

                // NEW: add last column with filename (added May 9, 2017)
                dt.Columns.Add("filename");

                var startRow = hasHeader ? 2 : 1;
                for (var rowNum = startRow; rowNum <= ws.Dimension.End.Row; rowNum++)
                {
                    var wsRow = ws.Cells[rowNum, 1, rowNum, ws.Dimension.End.Column];
                    var row = dt.NewRow();

                    var lastCellNumber = 0;

                    foreach (var cell in wsRow)
                    {
                        row[cell.Start.Column - 1] = cell.Text;
                        lastCellNumber = cell.Start.Column;
                    }

                    dt.Rows.Add(row);
                }

                DataTable ExcelDataTable = dt;

                foreach (DataRow row in ExcelDataTable.Rows)
                {
                    PrecedingWeekData blah = null;
                    var missingField = false;
                    try
                    {

                        blah =new PrecedingWeekData
                        {
                            FullName = row.Field<string>("fullName"),
                            CombinedAddress = row.Field<string>("combinedAddress"),
                            Circuit = row.Field<string>("circuit"),
                            ShortTitle = row.Field<string>("shortTitle"),
                            Caption = row.Field<string>("caption"),
                            Email = row.Field<string>("email"),
                            Phone = row.Field<string>("phone")
                        };
                    }
                    catch (MissingFieldException ex)
                    {
                        System.Diagnostics.Debug.Write(ex);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.Write(ex);
                        precedingWeekDataList = null;
                        missingField = true;
                    }
                    if (null == blah || missingField == true)
                    {
                        continue;
                    }
                    else
                    {
                        precedingWeekDataList.Add(blah);
                    }
                    
                }
            }
            return precedingWeekDataList;
        }

        private List<string> CaptureDataFromDatatable(DataTable dt, string column)
        {
            var rows = from _rows in dt.AsEnumerable()
                       select _rows;
            // Add Attorneys
            var attys = from _attys in rows
                        select _attys.Field<string>(column);
            return attys.ToList();

        }

        private DataTable CreateDataTableFromCsvFile(string csvFile)
        {
            // open CSV file and collect data
            List<string> list = new List<string>();
            var alltext = string.Empty;
            using (var reader = new StreamReader(File.OpenRead(csvFile)))
            { alltext = reader.ReadToEnd(); }
            var lines = alltext.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var l in lines) { list.Add(l); }

            // create datatable
            DataTable dt = new DataTable();
            // create column headers from first line of CSV
            var headerLine = list[0].Split(',');
            for (int i = 0; i < headerLine.Length; i++)
            {
                var header = headerLine[i];
                dt.Columns.Add(header);
            }
            // create rows from rest of CSV lines
            for (int i = 1; i < list.Count(); i++)
            {
                DataRow row = dt.NewRow();
                // USE THIS FOR CIRCUIT WEEK IMPORT
                var row_items = list[i].Split(new string[] { "\",\"" }, StringSplitOptions.None);
                //var row_items = list[i].Split(new string[] { "," }, StringSplitOptions.None);
                row_items[0] = row_items[0].Substring(1, row_items[0].Length - 1);
                row_items[row_items.Length - 1] =
                    row_items[row_items.Length - 1].Substring(
                        0, row_items[row_items.Length - 1].Length - 1);

                for (int j = 0; j < row_items.Length; j++)
                {
                    var itm = row_items[j];
                    row[j] = itm;
                }

                // add new row to table
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}