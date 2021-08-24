using System.Collections.Generic;

namespace CircuitCourtLookupMvvm.Models
{
    internal class GetDocketsCollectedFromPreviousWeeks
    {
        public List<PrecedingWeekFiles> DocketsCollectedFromPreviousWeeks { get; set; }

        public GetDocketsCollectedFromPreviousWeeks(
            string circuitFolderCurrent, 
            string FOLDER_CIRCUITCOURTS, 
            string ARCHIVE_CIRCUITCOURTS)
        {
            // set two locations:
            //var circuitFolderMain = @"\\CLBDC03\public\Letters\Circuit_Court_Letters\";
            //var circuitFolderArchive = @"\\CLBDC03\public\Letters\Circuit_Court_Letters\Archive_of_Pacer_Files\";

            var circuitFolderCurrentShortName = System.Text.RegularExpressions.Regex.Replace(System.IO.Path.GetFileName(circuitFolderCurrent), @"\D+", "");
            int circuitFolderCurrentWeekNumber;
            if (!int.TryParse(circuitFolderCurrentShortName, out circuitFolderCurrentWeekNumber)) { /*exit if not found*/ }

            var dirInCircuitFolderMain = System.IO.Directory.EnumerateDirectories(FOLDER_CIRCUITCOURTS);
            var dirInCircuitFolderArchive = System.IO.Directory.EnumerateDirectories(ARCHIVE_CIRCUITCOURTS);

            // collect folders from previous four weeks
            var previousWeekFolders = new List<System.IO.DirectoryInfo>();
            for (int previousWeekNumber = circuitFolderCurrentWeekNumber - 1;
                previousWeekNumber >= circuitFolderCurrentWeekNumber - 4;
                previousWeekNumber--)
            {
                var found = false;
                foreach (var subDirInMain in dirInCircuitFolderMain)
                {
                    var subFolder = System.IO.Path.GetFileName(subDirInMain);
                    if (subFolder.Contains(previousWeekNumber.ToString()))
                    {
                        previousWeekFolders.Add(new System.IO.DirectoryInfo(subDirInMain));
                        found = true;
                    }
                }
                if (!found)
                {
                    foreach (var subDirInArchive in dirInCircuitFolderArchive)
                    {
                        var subFolder = System.IO.Path.GetFileName(subDirInArchive);
                        if (subFolder.Contains(previousWeekNumber.ToString()))
                        {
                            previousWeekFolders.Add(new System.IO.DirectoryInfo(subDirInArchive));
                            found = true;
                        }
                    }
                }
                found = false;
            }

            // collect files from previous four weeks
            //var already_searched = new List<PrecedingWeekFiles>();
            DocketsCollectedFromPreviousWeeks = new List<PrecedingWeekFiles>();
            foreach (var previousWeekFolder in previousWeekFolders)
            {
                foreach (var previousWeekSubfolder in previousWeekFolder.EnumerateDirectories())
                {
                    // dig down to find pacer folder
                    if (previousWeekSubfolder.Name.ToLower().Contains("pacer"))
                    {
                        foreach (var previousWeekPacerFile in previousWeekSubfolder.EnumerateFiles())
                        {
                            int circuit;
                            var docket = string.Empty;

                            var re = new System.Text.RegularExpressions.Regex(@"(\d+)_(\d+\-\d+)");

                            if (re.Match(previousWeekPacerFile.Name).Success)
                            {
                                var match = re.Match(previousWeekPacerFile.Name);
                                int.TryParse(match.Groups[1].ToString(), out circuit);
                                docket = match.Groups[2].ToString();
                                DocketsCollectedFromPreviousWeeks.Add(
                                    new PrecedingWeekFiles { CircuitNumber = circuit, DocketNumber = docket, FileInfo = previousWeekPacerFile });
                            }
                        }
                    }
                }
            }
        
    }
    }
}