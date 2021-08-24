using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using OfficeOpenXml;

namespace CircuitCourtLookupMvvm.Utilities
{
    static class DatatableExtensionMethods
    {
        public static void WriteToExcelFile(this DataTable data_table, string dest_filename)
        {
            if (!System.IO.File.Exists(dest_filename))
            {
                var file_info = new System.IO.FileInfo(dest_filename);

                // Save to excel
                using (ExcelPackage package = new ExcelPackage(file_info))
                {
                    ExcelWorksheet ws = package.Workbook.Worksheets.Add("Cert_Letters");

                    ws.Cells["A1"].LoadFromDataTable(data_table, true);
                    package.Save();
                }
            }
        }

        public static DataView ApplySort(this DataTable table, Comparison<DataRow> comparison)
        {

            DataTable clone = table.Clone();
            List<DataRow> rows = new List<DataRow>();
            foreach (DataRow row in table.Rows)
            {
                rows.Add(row);
            }

            rows.Sort(comparison);

            foreach (DataRow row in rows)
            {
                clone.Rows.Add(row.ItemArray);
            }

            return clone.DefaultView;
        }

        public static void WriteToCsvFile(this DataTable dataTable, string filePath)
        {
            StringBuilder fileContent = new StringBuilder();

            foreach (var col in dataTable.Columns)
            {
                var replace_quotation = col.ToString().Replace("\"", "");

                fileContent.Append(replace_quotation + ",");
            }

            fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);


            // problem with quotations in text



            foreach (DataRow dr in dataTable.Rows)
            {

                foreach (var column in dr.ItemArray)
                {
                    if (column.ToString().Contains("\""))
                    {
                        var replace_quotation = column.ToString().Replace("\"", "");
                        fileContent.Append("\"" + replace_quotation + "\",");
                    }
                    else
                    {
                        fileContent.Append("\"" + column.ToString() + "\",");
                    }


                }

                fileContent.Replace(",", System.Environment.NewLine, fileContent.Length - 1, 1);
            }

            System.IO.File.WriteAllText(filePath, fileContent.ToString());
        }
    }
}
