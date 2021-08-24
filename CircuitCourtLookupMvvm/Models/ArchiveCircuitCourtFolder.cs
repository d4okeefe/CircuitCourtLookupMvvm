using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircuitCourtLookupMvvm.Models
{
    class ArchiveCircuitCourtFolder
    {
        public string FolderName { get; set; }
        public int? FolderNumber
        {
            get
            {
                var shortfilename = FolderName.Substring(
                    FolderName.LastIndexOf(@"\") + 1,
                    FolderName.Length - FolderName.LastIndexOf(@"\") - 1);
                var numFolder = System.Text.RegularExpressions.Regex.Match(shortfilename, @"\d+$").Value;

                int out_num;
                if (int.TryParse(numFolder, out out_num))
                {
                    return out_num;
                }
                return null;
            }
        }
        public ArchiveCircuitCourtFolder(string folderName)
        {
            FolderName = folderName;
        }
    }
}
