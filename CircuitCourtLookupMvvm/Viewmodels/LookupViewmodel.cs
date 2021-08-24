using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CircuitCourtLookupMvvm.Commands;
using System.Windows.Input;
using CircuitCourtLookupMvvm.Models;
using System.Windows;
using System.Windows.Threading;
using System.Collections.ObjectModel;
using CircuitCourtLookupMvvm.Utilities;
using System.Windows.Data;

namespace CircuitCourtLookupMvvm.Viewmodels
{
    class LookupViewmodel : ViewmodelBase
    {
        #region CONSTRUCTOR
        public LookupViewmodel()
        {
            #region initialize property values
            initializeSearchDates();
            loadCircuitCourtDataFoldersDataGrid();
            checkChromeWebDriver();
            CircuitCourtXmlOrderInfo = new ObservableCollection<DataFromXmlFiles>();

            //BindingOperations.EnableCollectionSynchronization(CircuitCourtDataFolders, lockObject);

            BindingOperations.CollectionRegistering += BindingOperations_CollectionRegistering;

            #endregion
            #region register relay commands
            RunAutomatePacerSearch = new RelayCommand(o => automatePacerSearch(), o => canAutomatePacerSearch());
            RunCreateMergeFile = new RelayCommand(o => createMergeFile(), o => canCreateMergeFile());
            RunOpenSelectedFolder = new RelayCommand(o => openSelectedFolder(), o => canOpenSelectedFolder());
            RunCreateExcelFile = new RelayCommand(o => createExcelFile(), o => canCreateExcelFile());
            RunCreateExtendedAddressExcelFile = new RelayCommand(o => createExtendedAddressExcelFile(), o => canCreateExtendedAddressExcelFile());
            RunCreateNewCircuitCourtFolder = new RelayCommand(o => createNewCircuitCourtFolder(), o => canCreateNewCircuitCourtFolder());
            RunGetOrderInfoFromFiles = new RelayCommand(o => getOrderInfoFromFiles());
            RunOpenSelectedXmlFile = new RelayCommand(o => openSelectedXmlFile(), o=>canOpenSelectedXmlFile());
            #endregion
        }
        #endregion
        #region FIELDS
        private readonly string FOLDER_CIRCUITCOURTS = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters";
        private readonly string ARCHIVE_CIRCUITCOURTS = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters\Archive_of_Pacer_Files";
        private readonly string CIRCUIT_ENVELOPE_TEMPLATE = @"\\CLBDC03\Public\Letters\Circuit_Court_Letters\CircuitEnvelopeTemplate.dotx";
        private bool isExecutingSearch;
        private string circuitRangeHighValue;
        private string circuitRangeLowValue;
        private KeyValuePair<int, string>? circuitCourtSelected;
        private string textUpdateTab1;
        private string textUpdateTab2;
        private object lockObject = new object();
        private DataFromXmlFiles selectXmlFile;
        private WorkingFolder selectedFolder;
        #endregion
        #region PROPERTIES
        public ObservableCollection<DataFromXmlFiles> CircuitCourtXmlOrderInfo { get; set; }
        public string TextUpdateTab1
        {
            get { return textUpdateTab1; }
            set
            {
                textUpdateTab1 = value;
                RaisePropertyChanged();
            }
        }
        public string TextUpdateTab2
        {
            get { return textUpdateTab2; }
            set
            {
                textUpdateTab2 = value;
                RaisePropertyChanged();
            }
        }
        public DataFromXmlFiles SelectXmlFile
        {
            get { return selectXmlFile; }
            set
            {
                selectXmlFile = value;
                RaisePropertyChanged();
            }
        }
        public WorkingFolder SelectedFolder
        {
            get { return selectedFolder; }
            set
            {
                selectedFolder = value;
                RaisePropertyChanged();
            }
        }
        public int SelectedIndex { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public KeyValuePair<int, string>? CircuitCourtSelectedItem
        {
            get { return circuitCourtSelected; }
            set
            {
                circuitCourtSelected = value;
                if (value != null)
                {
                    CircuitRangeLowValue = "";
                    CircuitRangeHighValue = "";
                }
                RaisePropertyChanged();
            }
        }
        public Dictionary<int, string> CircuitCourtOptions
        {
            get
            {
                return new Dictionary<int, string>
                {
                    //{ 0, "" },
                    { 1, "First Circuit" },
                    { 2, "Second Circuit" },
                    { 3, "Third Circuit" },
                    { 4, "Fourth Circuit" },
                    { 5, "Fifth Circuit" },
                    { 6, "Sixth Circuit" },
                    { 7, "Seventh Circuit" },
                    { 8, "Eighth Circuit" },
                    { 9, "Ninth Circuit" },
                    { 10, "Tenth Circuit" },
                    { 11, "Eleventh Circuit" },
                    { 12, "DC Circuit" },
                    { 13, "Federal Circuit" }
                };
            }
        }
        public string CircuitRangeLowValue
        {
            get { return circuitRangeLowValue; }
            set
            {
                circuitRangeLowValue = value;
                RaisePropertyChanged();

                if (!(string.IsNullOrEmpty(CircuitRangeLowValue)
                    && string.IsNullOrEmpty(CircuitRangeHighValue)))
                {
                    CircuitCourtSelectedItem = null;
                    RaisePropertyChanged("CircuitCourtSelected");
                }
            }
        }
        public string CircuitRangeHighValue
        {
            get { return circuitRangeHighValue; }
            set
            {
                circuitRangeHighValue = value;
                RaisePropertyChanged();

                if (!(string.IsNullOrEmpty(CircuitRangeLowValue)
                    && string.IsNullOrEmpty(CircuitRangeHighValue)))
                {
                    CircuitCourtSelectedItem = null;
                    RaisePropertyChanged("CircuitCourtSelected");
                }
            }
        }
        public ObservableCollection<WorkingFolder> CircuitCourtDataFolders { get; set; }
        //{
        //    get { return circuitCourtLettersFolders; }
        //    set
        //    {
        //        circuitCourtLettersFolders = value;
        //        RaisePropertyChanged();
        //    }
        //}
        //public System.Windows.Data.ListCollectionView CircuitCourtDataFoldersLCV { get; set; }
        public System.IO.FileSystemWatcher CircuitCourtDataFoldersWatcher { get; set; }
        public bool IsExecutingSearch
        {
            get { return isExecutingSearch; }
            set
            {
                isExecutingSearch = value;
                RaisePropertyChanged();
            }
        }
        #endregion
        #region PRIVATE METHODS
        private async void getOrderInfoFromFiles()
        {

            var circuitCourtXmlOrderInfo = await Task.Run(() =>
            {
                return  new CollectInfoFromXmlFilesForSelectedFolder(SelectedFolder.FoldernameComplete).CollectedXmlData;
            });
            CircuitCourtXmlOrderInfo.Clear();
            if (null != circuitCourtXmlOrderInfo && circuitCourtXmlOrderInfo.Count > 0)
            {
                circuitCourtXmlOrderInfo.ForEach(f => CircuitCourtXmlOrderInfo.Add(f));
            }

        }
        private bool canCreateExtendedAddressExcelFile()
        {
            if (SelectedFolder == null || SelectedFolder.CountXmlFiles == 0)
            {
                return false;
            }
            return true;
        }
        private async void createExtendedAddressExcelFile()
        {
            try
            {
                // gui updates
                IsExecutingSearch = true;
                TextUpdateTab2 = "";

                // create name for new excel file
                var destExcelFile = System.IO.Path.Combine(SelectedFolder.FoldernameComplete, "datafile_extended.xlsx");

                // gather information and create new xlsx file
                await Task.Run(() =>
                {
                    new ConvertXmlsToXlsxFileWithExtendedAddresses(SelectedFolder.FoldernameComplete, destExcelFile);
                });
                TextUpdateTab2 = $"{System.IO.Path.GetFileName(destExcelFile)} was created.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TextUpdateTab2 = ex.Message;
            }
            finally
            {
                IsExecutingSearch = false;
            }
        }
        private bool canCreateExcelFile()
        {
            if (SelectedFolder == null || SelectedFolder.CountXmlFiles == 0)
            {
                return false;
            }
            if (SelectedFolder.HasExcelSpreadsheetFile)
            {
                //return false;
            }
            return true;
        }
        private async void createExcelFile()
        {
            try
            {
                // gui updates
                IsExecutingSearch = true;
                TextUpdateTab2 = "";

                // create name for new excel file
                var destExcelFile = System.IO.Path.Combine(SelectedFolder.FoldernameComplete, "datafile_new.xlsx");

                // gather information and create new xlsx file
                await Task.Run(() =>
                {
                    new ConvertXmlsToXlsxFile(SelectedFolder.FoldernameComplete, destExcelFile);
                });
                TextUpdateTab2 = $"{System.IO.Path.GetFileName(destExcelFile)} was created.";
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TextUpdateTab2 = ex.Message;
            }
            finally
            {
                IsExecutingSearch = false;
            }
        }
        private void checkChromeWebDriver()
        {
            if (!System.IO.Directory.Exists(@"C:\Program Files\chromedriver_win32\")
                || !System.IO.File.Exists(@"C:\Program Files\chromedriver_win32\chromedriver.exe"))
            {
                TextUpdateTab1 = "Chrome WebDriver is not installed on this PC!";
            }
        }
        private bool canOpenSelectedXmlFile()
        {
            return SelectXmlFile != null;
        }
        private void openSelectedXmlFile()
        {
            System.Diagnostics.Process.Start(SelectXmlFile.FileName);

            // attempt to open xml file with Visual Studio Code (problem: opens as folder, not file)
            //var filename = SelectXmlFile.FileName.Replace(@"\\CLBDC03\Public\", @"P:\");
            //System.Diagnostics.Process.Start("code", filename);
        }
        private bool canOpenSelectedFolder()
        {
            return SelectedFolder != null;
        }
        private void openSelectedFolder()
        {
            System.Diagnostics.Process.Start(SelectedFolder.FoldernameComplete);
        }
        private bool canCreateMergeFile()
        {
            if (null != SelectedFolder
                && SelectedFolder.HasExcelSpreadsheetFile
                && System.IO.File.Exists(CIRCUIT_ENVELOPE_TEMPLATE))
            {
                return true;
            }
            return false;
        }
        private async void createMergeFile()
        {
            try
            {
                IsExecutingSearch = true;
                TextUpdateTab2 = "";

                // check for more than one excel file ???
                var sourceXlsxFile = (from x in System.IO.Directory
                                      .EnumerateFiles(SelectedFolder.FoldernameComplete, "*.xls?", System.IO.SearchOption.AllDirectories)
                                      select x).FirstOrDefault();
                if (!string.IsNullOrEmpty(sourceXlsxFile))
                {
                    var destDocxFile = System.IO.Path.Combine(SelectedFolder.FoldernameComplete, "merge.docx");

                    await Task.Run(() =>
                    {
                        new CreateDocxMergeFromXlsxFile(sourceXlsxFile, CIRCUIT_ENVELOPE_TEMPLATE, destDocxFile);
                    });

                    TextUpdateTab2 = $"{System.IO.Path.GetFileName(destDocxFile)} was created.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TextUpdateTab2 = ex.Message;
            }
            finally
            {
                IsExecutingSearch = false;
            }
        }
        private void loadCircuitCourtDataFoldersDataGrid()
        {
            CircuitCourtDataFolders = new ObservableCollection<WorkingFolder>();
            //CircuitCourtDataFoldersLCV = new System.Windows.Data.ListCollectionView(CircuitCourtDataFolders);

            foreach (var folder in System.IO.Directory.EnumerateDirectories(FOLDER_CIRCUITCOURTS))
            {
                if (folder.ToLower().Contains("week"))
                {
                    var dirInfo = new System.IO.DirectoryInfo(folder);

                    var xml_files =
                        from f in dirInfo.EnumerateFiles("*.xml*", System.IO.SearchOption.AllDirectories)
                        where f.FullName.ToLower().EndsWith(".xml")
                        select f;

                    var top_folder_files =
                        from f in dirInfo.EnumerateFiles("*.*", System.IO.SearchOption.TopDirectoryOnly)
                        select f;

                    var wf = new WorkingFolder
                    {
                        FoldernameComplete = folder,
                        FoldernameShort = System.IO.Path.GetFileName(folder),
                        CountXmlFiles = xml_files.Count(),
                        XmlDownloadDateDateTime = xml_files.Count() > 0
                            ? new DateTime?(xml_files.First().LastWriteTime) : null,
                        HasWordMergeFile =
                            (from f in dirInfo.EnumerateFiles("*.docx", System.IO.SearchOption.TopDirectoryOnly)
                             select f).Count() > 0,
                        HasExcelSpreadsheetFile =
                            (from f in dirInfo.EnumerateFiles("*.xls?", System.IO.SearchOption.TopDirectoryOnly)
                             select f).Count() > 0
                            || (from f in dirInfo.EnumerateFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly)
                                select f).Count() > 0
                    };

                    CircuitCourtDataFolders.Add(wf);

                    // create watcher to track FolderCollection
                    CircuitCourtDataFoldersWatcher = new System.IO.FileSystemWatcher(FOLDER_CIRCUITCOURTS)
                    {
                        Path = FOLDER_CIRCUITCOURTS,
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = true,
                        NotifyFilter = System.IO.NotifyFilters.CreationTime
                            | System.IO.NotifyFilters.DirectoryName
                            | System.IO.NotifyFilters.FileName
                            | System.IO.NotifyFilters.LastAccess
                            | System.IO.NotifyFilters.LastWrite
                    };

                    CircuitCourtDataFoldersWatcher.Changed += (obj, e) =>
                        Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        new Action(() => { circuitCourtDataFolderChanged(obj, e); }));
                    CircuitCourtDataFoldersWatcher.Created += (obj, e) =>
                        Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        new Action(() => { circuitCourtDataFolderChanged(obj, e); }));
                    CircuitCourtDataFoldersWatcher.Deleted += (obj, e) =>
                        Application.Current.Dispatcher.BeginInvoke(
                        DispatcherPriority.Send,
                        new Action(() => { circuitCourtDataFolderChanged(obj, e); }));
                }
            }
        }
        private void circuitCourtDataFolderChanged(object sender, System.IO.FileSystemEventArgs e)
        {
            if (!System.IO.Directory.Exists(FOLDER_CIRCUITCOURTS)) return;

            var watcher = sender as System.IO.FileSystemWatcher;
            var e_FullPath = System.IO.Path.Combine(watcher.Path, e.Name);

            // test extension to determine file type
            var file_type = new CircuitCourtFileType();
            var file_extension = System.IO.Path.GetExtension(e.FullPath);
            if (string.IsNullOrWhiteSpace(file_extension))
            {
                file_type = CircuitCourtFileType.DIRECTORY;
            }
            else
            {
                if (".xml".Equals(file_extension))
                {
                    file_type = CircuitCourtFileType.XML_FILE;
                }
                else if (".xlsx".Equals(file_extension) || ".xls".Equals(file_extension) || ".csv".Equals(file_extension))
                {
                    file_type = CircuitCourtFileType.EXCEL_FILE;
                }
                else if (".docx".Equals(file_extension) || ".doc".Equals(file_extension))
                {
                    file_type = CircuitCourtFileType.WORD_FILE;
                }
                else
                {
                    return; // or throw exception
                }
            }

            if (e.ChangeType == System.IO.WatcherChangeTypes.Created
                || e.ChangeType == System.IO.WatcherChangeTypes.Deleted
                || e.ChangeType == System.IO.WatcherChangeTypes.Changed)
            {
                try
                {
                    if (file_type == CircuitCourtFileType.DIRECTORY)
                    {
                        // make sure the directory is one step below top level
                        var topLevelParentDirectory = System.IO.Directory.GetParent(e.FullPath);
                        if (FileNavigationStaticUtilities.NormalizeFilePath(FOLDER_CIRCUITCOURTS).Equals(
                            FileNavigationStaticUtilities.NormalizeFilePath(topLevelParentDirectory.FullName)))
                        {
                            if (e.FullPath.ToLower().Contains("week"))
                            {
                                if (e.ChangeType == System.IO.WatcherChangeTypes.Created)
                                {
                                    if (0 == CircuitCourtDataFolders.Where(f => f.FoldernameComplete.Equals(e.FullPath)).Count())
                                    {

                                        var dirInfo = new System.IO.DirectoryInfo(e.FullPath);

                                        var xml_files =
                                            from f in dirInfo.EnumerateFiles("*.xml*", System.IO.SearchOption.AllDirectories)
                                            where f.FullName.ToLower().EndsWith(".xml")
                                            select f;

                                        var wf = new WorkingFolder
                                        {
                                            FoldernameComplete = e.FullPath,
                                            FoldernameShort = System.IO.Path.GetFileName(e.FullPath),
                                            CountXmlFiles = xml_files.Count(),
                                            XmlDownloadDateDateTime = xml_files.Count() > 0
                                                ? new DateTime?(xml_files.First().LastWriteTime) : null,
                                            HasWordMergeFile =
                                                (from f in dirInfo.EnumerateFiles("*.doc?", System.IO.SearchOption.TopDirectoryOnly)
                                                 select f).Count() > 0,
                                            HasExcelSpreadsheetFile =
                                                (from f in dirInfo.EnumerateFiles("*.xls?", System.IO.SearchOption.TopDirectoryOnly)
                                                 select f).Count() > 0
                                                || (from f in dirInfo.EnumerateFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly)
                                                    select f).Count() > 0
                                        };
                                        try
                                        {
                                            Task.Run(() =>
                                            {
                                                lock (lockObject)
                                                {
                                                    if (0 == CircuitCourtDataFolders.Where(f => f.FoldernameComplete.Equals(wf.FoldernameComplete)).Count())
                                                    {
                                                        CircuitCourtDataFolders.Add(wf);
                                                    }
                                                }
                                            });
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine(ex.Message);
                                        }
                                    }
                                }
                                else if (e.ChangeType == System.IO.WatcherChangeTypes.Deleted)
                                {
                                    try
                                    {
                                        Task.Run(() =>
                                        {
                                            lock (lockObject)
                                            {
                                                var wf = CircuitCourtDataFolders.Where(f => f.FoldernameComplete.Equals(e.FullPath));
                                                if (1 == wf.Count())
                                                {
                                                    {
                                                        CircuitCourtDataFolders.Remove(wf.First());
                                                    }
                                                }
                                            }
                                        });
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        var watched_folder = System.IO.Path.GetDirectoryName(e.FullPath);
                        // identify which parent directory has match in our CircuitCourtDataFolders
                        while (0 == CircuitCourtDataFolders.Where(f => f.FoldernameComplete.Equals(watched_folder)).Count())
                        {
                            watched_folder = System.IO.Directory.GetParent(watched_folder).FullName;
                            // figure out way to break if this never happens ???
                        }
                        var watched_folder_dir_info = new System.IO.DirectoryInfo(watched_folder);

                        if (file_type == CircuitCourtFileType.XML_FILE)
                        {
                            var xml_files =
                                from f in new System.IO.DirectoryInfo(
                                    watched_folder).EnumerateFiles(
                                    "*.xml*", System.IO.SearchOption.AllDirectories)
                                where f.FullName.ToLower().EndsWith(".xml")
                                select f;

                            // update xml count on that folder
                            var working_folder =
                                (from f in CircuitCourtDataFolders
                                 where f.FoldernameComplete.Equals(watched_folder)
                                 select f).First();
                            working_folder.CountXmlFiles = xml_files.Count();
                            working_folder.XmlDownloadDateDateTime = xml_files.Count() > 0 ? new DateTime?(xml_files.First().LastWriteTime) : null;
                        }
                        else if (file_type == CircuitCourtFileType.EXCEL_FILE)
                        {
                            var working_folder =
                                (from f in CircuitCourtDataFolders
                                 where f.FoldernameComplete.Equals(watched_folder)
                                 select f).First();
                            working_folder.HasExcelSpreadsheetFile =
                                (from f in watched_folder_dir_info.EnumerateFiles("*.xls?", System.IO.SearchOption.TopDirectoryOnly)
                                 select f).Count() > 0
                                 || (from f in watched_folder_dir_info.EnumerateFiles("*.csv", System.IO.SearchOption.TopDirectoryOnly)
                                     select f).Count() > 0;
                        }
                        else if (file_type == CircuitCourtFileType.WORD_FILE)
                        {
                            var working_folder =
                                (from f in CircuitCourtDataFolders
                                 where f.FoldernameComplete.Equals(watched_folder)
                                 select f).First();
                            working_folder.HasWordMergeFile =
                                (from f in watched_folder_dir_info.EnumerateFiles("*.doc?", System.IO.SearchOption.TopDirectoryOnly)
                                 select f).Count() > 0;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                    return;
                }
            }
        }
        private bool canAutomatePacerSearch()
        {
            if ((CircuitCourtSelectedItem == null
                && (string.IsNullOrEmpty(CircuitRangeHighValue)
                && string.IsNullOrEmpty(CircuitRangeLowValue)))
                || SelectedFolder == null
                || IsExecutingSearch)
            {
                return false;
            }
            return true;
        }
        private async void automatePacerSearch()
        {

            try
            {
                IsExecutingSearch = true;
                TextUpdateTab1 = "";

                var downloadLocation = SelectedFolder.FoldernameComplete;

                var circuitsToSearch = new int[2];
                if (null != CircuitCourtSelectedItem)
                {
                    circuitsToSearch[0] = CircuitCourtSelectedItem.Value.Key;
                }
                else
                {
                    int out_num;
                    if (int.TryParse(CircuitRangeLowValue, out out_num))
                    {
                        circuitsToSearch[0] = out_num;
                    }
                    if (int.TryParse(CircuitRangeHighValue, out out_num))
                    {
                        circuitsToSearch[1] = out_num;
                    }
                }

                if (circuitsToSearch[0] == 0)
                {
                    circuitsToSearch[0] = circuitsToSearch[1];
                }
                else if (circuitsToSearch[1] == 0)
                {
                    circuitsToSearch[1] = circuitsToSearch[0];
                }
                else if (circuitsToSearch[1] < circuitsToSearch[0])
                {
                    var temp = circuitsToSearch[1];
                    circuitsToSearch[1] = circuitsToSearch[0];
                    CircuitRangeHighValue = circuitsToSearch[0].ToString();
                    circuitsToSearch[0] = temp;
                    CircuitRangeLowValue = temp.ToString();
                }

                var filesFromPreviousWeeks = new GetDocketsCollectedFromPreviousWeeks(
                    downloadLocation, FOLDER_CIRCUITCOURTS, ARCHIVE_CIRCUITCOURTS)
                    .DocketsCollectedFromPreviousWeeks;

                var searchAllCircuits = circuitsToSearch[0] == 1 && circuitsToSearch[1] == 13 ? true : false;

                await Task.Run(() =>
                {
                    new CollectFilesFromPacer(StartDate, EndDate, downloadLocation, searchAllCircuits,
                        circuitsToSearch[0], circuitsToSearch[1], filesFromPreviousWeeks);
                });

                TextUpdateTab1 = "Search complete";
            }
            catch (Exception ex)
            {
                TextUpdateTab1 = ex.Message;
            }
            finally
            {
                IsExecutingSearch = false;
            }
        }
        private void initializeSearchDates()
        {
            var initialDates = new DateTime[2];
            var current_date = DateTime.Now;

            // get previous Friday
            var previous_friday = current_date;
            while (previous_friday.DayOfWeek != DayOfWeek.Friday)
            {
                previous_friday = previous_friday.AddDays(-1);
            }
            initialDates[1] = previous_friday;

            // get previous Monday
            var previous_saturday = previous_friday;
            while (previous_saturday.DayOfWeek != DayOfWeek.Saturday)
            {
                previous_saturday = previous_saturday.AddDays(-1);
            }
            initialDates[0] = previous_saturday;

            // set properties
            StartDate = initialDates[0];
            EndDate = initialDates[1];
        }
        private async void createNewCircuitCourtFolder()
        {
            await Task.Run(() =>
            {
                var new_folder_name = string.Empty;
                try
                {
                    var ccFolders = new List<ArchiveCircuitCourtFolder>();
                    foreach (var f in CircuitCourtDataFolders)
                    {
                        ccFolders.Add(new ArchiveCircuitCourtFolder(f.FoldernameComplete));
                    }

                    var highestFolderNumber =
                        (from f in ccFolders
                         select f.FolderNumber).Max();

                    // create new folder
                    new_folder_name = System.IO.Path.Combine
                        (FOLDER_CIRCUITCOURTS, "Week_" + ++highestFolderNumber);

                    System.IO.Directory.CreateDirectory(new_folder_name);

                    // let filesystemwatcher add to datagrid
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    TextUpdateTab1 = "Could not create new folder\n" + ex.Message;
                }
            });
        }
        private bool canCreateNewCircuitCourtFolder()
        {
            return true;
        }
        private void BindingOperations_CollectionRegistering(object sender, CollectionRegisteringEventArgs e)
        {
            if (e.Collection == CircuitCourtDataFolders)
            {
                BindingOperations.EnableCollectionSynchronization(CircuitCourtDataFolders, lockObject);
            }
        }
        #endregion
        #region ICOMMAND PROPERTIES
        public ICommand RunOpenSelectedXmlFile { get; private set; }
        public ICommand RunAutomatePacerSearch { get; private set; }
        public ICommand RunCreateMergeFile { get; private set; }
        public ICommand RunOpenSelectedFolder { get; private set; }
        public ICommand RunDeselectDataGridItem { get; private set; }
        public ICommand RunCreateExcelFile { get; private set; }
        public ICommand RunCreateExtendedAddressExcelFile { get; private set; }
        public ICommand RunCreateNewCircuitCourtFolder { get; private set; }
        public ICommand RunGetOrderInfoFromFiles { get; private set; }
        #endregion
    }
}
