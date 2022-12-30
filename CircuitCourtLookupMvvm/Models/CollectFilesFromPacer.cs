using System.Collections.Generic;
using CircuitCourtLookupMvvm.Models;
using System;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;


using System.Text.RegularExpressions;
using System.Linq;
using System.Globalization;

namespace CircuitCourtLookupMvvm.Models
{
    internal class CollectFilesFromPacer
    {
        #region FIELDS
        private Dictionary<int, string> links = new Dictionary<int, string>()
        {
            // Notice circuits 2 & 9 have different web addresses; will have to keep an eye on these going forward
            { 1, "https://ecf.ca1.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 2, "https://ecf.ca2.uscourts.gov/n/beam/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 3, "https://ecf.ca3.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 4, "https://ecf.ca4.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 5, "https://ecf.ca5.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 6, "https://ecf.ca6.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 7, "https://ecf.ca7.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 8, "https://ecf.ca8.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 9, "https://ecf.ca9.uscourts.gov/n/beam/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 10, "https://ecf.ca10.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 11, "https://ecf.ca11.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 12, "https://ecf.cadc.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" },
            { 13, "https://ecf.cafc.uscourts.gov/cmecf/servlet/TransportRoom?servlet=CaseSearch.jsp&advancedSearch=Advanced" }
        };
        private Dictionary<int, List<string>> casetypes = new Dictionary<int, List<string>>()
        {
            { 1, new List<string>() { "fcc", "ustc", "ag", "bk", "civil", "misc" } },
            { 2, new List<string>() { "ag", "bk", "cv", "ma", "mb", "mv", "M" } },
            { 3, new List<string>() { "misc", "nlrb", "ag", "bk", "cv", "tx" } },
            { 4, new List<string>() { "rvw", "rvw.imm", "bk.dc", "cv.copy", "cv.pri", "cv.pri60", "cv.ss", "cv.us", "cv.us60", "other.cv", "tax" } },
            { 5, new List<string>() { "ag", "bkcy", "img", "misc", "pcd", "pcf", "ss", "tax", "usc" } },
            { 6, new List<string>() { "ag", "bk", "cv", "ms", "nlrb", "tx" } },
            { 7, new List<string>() { "ag", "bk", "cv", "misc", "tax" } },
            { 8, new List<string>() { "ag", "bk", "cv", "ins", "ms" } },
            /*{ 9, new List<string>() { "ag", "bkb", "bkd", "cv", "misc", "tax" } },*/
            /*{ 9, new List<string>() { "ag", "bkb", "bkd", "cv", "misc", "tax", "1292","1292b","1453c","158d","23f","9a","atydis","bkp","cjm","cr","msop","op","pr","pr-dp"} },*/
            { 9, new List<string>() { "cv", "cr"} },
            { 10, new List<string>() { "bkp", "tax", "admin", "cv", "misc", "agpet" } },
            { 11, new List<string>() { "agen", "bk", "misc", "pricivil", "stp", "usc" } },
            { 12, new List<string>() { "app", "cvpri", "cvus", "msag", "mscv", "rev", "tx" } },
            { 13, new List<string>() { "ag", "bcaag", "cvPri", "cvUS", "mand-Age", "mand", "misc-Age", "misc" } }
        };
        /**
         * Added Jan 11 2022 to try to capture more pro se clients
         */
        private Dictionary<int, List<string>> casetypes_all = new Dictionary<int, List<string>>()
        {
            { 1, new List<string>() { "fcc", "ustc", "ag", "ad", "bail", "bk", "civil", "cr", "dp", "gj", "ms", "misc", "op", "pop" } },
            { 2, new List<string>() { "ag", "bk", "cv", "cr", "ma", "mb", "mv", "mc", "mo", "mp", "op", "pr", "M" } },
            { 3, new List<string>() { "adp", "bail", "dp", "misc", "misc158", "nlrb", "ag", "bk", "cv", "cr", "op", "pr", "tx" } },
            { 4, new List<string>() { "2255-dp", "2255", "rvw", "rvw.imm", "enf", "bail", "bk.mand", "bk.bc", "bk.dc", "cv.copy", "cv.pri", "cv.pri60", "cv.ss", "cv.us", "cv.us60", "cr", "cr-dp", "gj", "hc.fed", "hc.imm", "hc.st", "mand", "mand.72h", "op.hc", "op.o", "other.cv", "other.cr", "R5.bc", "R5.dc", "cvrgt", "pr-dp", "proh", "rclwt", "2244.fed", "2244.st", "tax", "jud.comp", "misc.jud" } },
            { 5, new List<string>() { "ag", "bkcy", "cr", "dcrim", "ncrim", "dpw", "dpwo", "bkcy-bc", "img", "misc", "misc-bc", "op", "prsw", "prswo", "pcd", "pcf", "ss", "spl-jmc", "tax", "usc" } },
            { 6, new List<string>() { "admin", "ag", "ad", "bail", "bk", "bap", "cv", "cr", "dp", "gjcv", "gjcr", "ms", "nlrb", "op", "perm", "pr", "tx", "pop" } },
            { 7, new List<string>() { "ag", "bkbk", "bk", "cv", "cr", "disc", "misc", "op", "pr", "tax" } },
            { 8, new List<string>() { "ag", "acomp", "disc", "bk", "bapop", "bap", "bkp", "bkop", "bkb", "cv", "cr", "crdp", "prcvrdp", "fhdp", "shcdp", "ins", "jcomp", "ms", "msbk", "opag", "op", "pr" } },
            { 9, new List<string>() { "1292", "1292b", "1453c", "158d", "23f", "9a", "ag", "atydis", "bkp", "bkb", "bkd", "cv", "cjm", "cr", "misc", "msop", "op", "pr", "pr-dp", "tax" } },
            { 10, new List<string>() { "-", "bkp", "tax", "admin", "cv", "cr", "disc", "misc", "op", "agenf", "agpet", "pr" } },
            { 11, new List<string>() { "agen", "bk", "crim", "dbk", "misc", "op", "pricivil", "stp", "usc", "usp" } },
            { 12, new List<string>() { "2255", "app", "enf", "ad.reg", "ad", "bail", "bkprid", "bkpri", "bkus", "bkusd", "cvpri", "cvus", "cr", "cr.dp", "x.enf", "gj", "gj.recal", "msag", "mscv", "mscr", "msop", "opag", "opcr", "oppri", "opus", "opvr", "1292b", "1453c", "158d", "23f", "perm", "rev", "prpri", "prus", "tx" } },
            { 13, new List<string>() { "admord", "ag", "atydiscp", "bcaag", "cvPri", "cvUS", "NOA", "mand-Age", "mand", "misc-Age", "misc" } }
        };

        //private List<PrecedingWeekFiles> filesFromPreviousWeeks;
        #endregion

        #region PROPERTIES
        public string DownloadDirectory { get; private set; }
        public bool SearchAllCircuits { get; private set; }
        public string StartDateString { get; private set; }
        public string EndDateString { get; private set; }
        public int LowCircuit { get; private set; }
        public int HighCircuit { get; private set; }
        public List<PrecedingWeekFiles> FilesFromPreviousWeeks { get; private set; }
        public List<string> FilesAlreadyDownloaded { get; private set; }
        public List<string> XmlFilesDownloaded { get; set; }
        #endregion
        #region CONSTRUCTOR
        public CollectFilesFromPacer(
                DateTime startDate, DateTime endDate,
                string downloadDirectory, bool searchAllCircuits,
                int circuitToSearchLow, int circuitToSearchHigh,
                List<PrecedingWeekFiles> filesFromPreviousWeeks)
        {
            // set properties
            DownloadDirectory = downloadDirectory;
            SearchAllCircuits = searchAllCircuits;
            StartDateString = startDate.ToString("MM'/'dd'/'yyyy");
            EndDateString = endDate.ToString("MM'/'dd'/'yyyy");
            LowCircuit = circuitToSearchLow;
            HighCircuit = circuitToSearchHigh;
            FilesFromPreviousWeeks = filesFromPreviousWeeks;

            if (string.IsNullOrWhiteSpace(DownloadDirectory) || !System.IO.Directory.Exists(DownloadDirectory))
            {
                throw new Exception();
            }
            else
            {
                // if subdirectory Pacer Files is empty, use it.
                // else: create subdirectory Pacer Files with date stamp
                var subDirectories = System.IO.Directory.EnumerateDirectories(DownloadDirectory).ToList();
                var pacerDirectory = subDirectories.Find(f => f.ToLower().Contains("pacer"));

                // test if directory has a "pacer" subdirectory, if it doesn't create new directory
                if (!string.IsNullOrEmpty(pacerDirectory))
                {
                    DownloadDirectory = pacerDirectory;
                }
                else
                {
                    var newDirectoryName = System.IO.Path.Combine(
                        DownloadDirectory, $"Pacer Files {System.DateTime.Now.ToShortDateString().Replace('/', '-')}");
                    if (System.IO.Directory.Exists(newDirectoryName))
                    {
                        throw new Exception();
                    }
                    else
                    {
                        var newDirectoryInfo = System.IO.Directory.CreateDirectory(newDirectoryName);
                        DownloadDirectory = newDirectoryName;
                    }
                }
            }

            // don't repeat downloads
            // 1. get existing files in directory
            FilesAlreadyDownloaded = System.IO.Directory.GetFiles(DownloadDirectory).ToList<string>();

            // set Chrome options
            var chromeOptions = setChromeBrowserOptions(DownloadDirectory);

            // check if driver exists
            if (!System.IO.Directory.Exists(@"C:\Program Files\chromedriver_win32\")
                || !System.IO.File.Exists(@"C:\Program Files\chromedriver_win32\chromedriver.exe"))
            {
                throw new Exception();
            }

            // initiate Chrome Driver
            /**
             * Aug 8, 2022
             * If error -- nav to https://chromedriver.chromium.org/downloads
             * & download the matching version.
             * 
             * Save to 2 locations -- 
             * C:\Program Files\Google\Chrome\Application
             * C:\Program Files\chromedriver_win32
             */
            using (IWebDriver driver = new ChromeDriver(@"C:\Program Files\chromedriver_win32\", chromeOptions))
            {
                // March 2022 CHANGE -- 
                // No longer nav to main Pacer page. Instead nav to each court's Pacer page and work from there.


                // Login to Pacer
                //var first_stop = navigateToPacerLoginPage(driver);

                // cycle through circuits 1 to 13 (DC=12,FC=13)
                for (int i = LowCircuit; i <= HighCircuit; i++)
                {
                    // navigate to advanced search page (in "links" Dictionary)
                    var _link = links[i];
                    driver.Navigate().GoToUrl(_link);

                    // relogin on court site
                    // March 2022 -- only login for first in list?
                    if (i == LowCircuit)
                    {
                        loginToCourtSite(driver);
                    }

                    // set Pacer Search Options
                    setPacerSearchOptions(driver, i);

                    // Click Search
                    driver.FindElement(By.Name("SearchButton")).Click();

                    // Collect Xml Files
                    captureFileAndDownload(i, driver, filesFromPreviousWeeks);
                }
            }
            Console.ReadLine();
        }

        private void loginToCourtSite(IWebDriver driver)
        {
            var elementLoginName = driver.FindElement(By.Id("loginForm:loginName"));
            //elementLoginName.SendKeys("cp0952");
            elementLoginName.SendKeys("cocklelegalbriefs");
            //var elementPassword = driver.FindElement(By.Id("login:password"));
            var elementPassword = driver.FindElement(By.Id("loginForm:password"));
            //elementPassword.SendKeys("!o39gisi");
            elementPassword.SendKeys("Lg3422831!");
            //var elementClientCode = driver.FindElement(By.Id("login:clientCode"));
            var elementClientCode = driver.FindElement(By.Id("loginForm:clientCode"));
            elementClientCode.SendKeys("DMO");

            var elementFBtnLogin = driver.FindElement(By.Id("loginForm:fbtnLogin"));
            elementFBtnLogin.Click();
        }
        #endregion
        #region PRIVATE METHODS
        private string navigateToPacerLoginPage(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://pacer.login.uscourts.gov/csologin/login.jsf");
            //var elementLoginName = driver.FindElement(By.Id("login:loginName"));
            var elementLoginName = driver.FindElement(By.Id("loginForm:loginName"));
            //elementLoginName.SendKeys("cp0952");
            elementLoginName.SendKeys("cocklelegalbriefs");
            //var elementPassword = driver.FindElement(By.Id("login:password"));
            var elementPassword = driver.FindElement(By.Id("loginForm:password"));
            //elementPassword.SendKeys("!o39gisi");
            elementPassword.SendKeys("Lg3422831!");
            //var elementClientCode = driver.FindElement(By.Id("login:clientCode"));
            var elementClientCode = driver.FindElement(By.Id("loginForm:clientCode"));
            elementClientCode.SendKeys("DMO");
            //var elementFBtnLogin = driver.FindElement(By.Id("login:fbtnLogin"));
            var elementFBtnLogin = driver.FindElement(By.Id("loginForm:courtId_label"));

            elementFBtnLogin.Click();

            return driver.Url;
        }
        private void setPacerSearchOptions(IWebDriver driver, int circuit)
        {
            // 2022: Problem here -- need to wait for page to load before trying to select with several court sites, but not all
            //       Add try/catch block to catch exception, wait 3 seconds, then try again. Something like -- Thread.Sleep(3000);

            // Set Options
            IWebElement options_casetype = null;
            try
            {
                options_casetype = driver.FindElement(By.Name("casetype"));
            }
            catch (Exception)
            {
                System.Threading.Thread.Sleep(3000);
                options_casetype = driver.FindElement(By.Name("casetype"));
            }

            //var options_casetype = driver.FindElement(By.Name("casetype"));
            SelectElement selector_casetype = new SelectElement(options_casetype);
            selector_casetype.DeselectAll();
            //foreach (string _type in casetypes[circuit])
            foreach (string _type in casetypes_all[circuit])
            {
                selector_casetype.SelectByValue(_type);
            }

            // Set Date Range
            var elementFileDateBegin = driver.FindElement(By.Name("filedate_begin"));
            elementFileDateBegin.Clear();
            elementFileDateBegin.SendKeys(StartDateString);
            var elementFileDateEnd = driver.FindElement(By.Name("filedate_end"));
            elementFileDateEnd.Clear();
            elementFileDateEnd.SendKeys(EndDateString);

            if (circuit == 9)
            {
                // Set Case Number Range for 9th Circuit
                //csnum1= 16 - 00001, csnum2 = 16 - 99999
                var elementCaseNumberBegin = driver.FindElement(By.Name("csnum1"));
                elementCaseNumberBegin.Clear();
                var yearTwoDigits = System.DateTime.Today.Year % 100;
                elementCaseNumberBegin.SendKeys($"{yearTwoDigits}-00001");
                var elementCaseNumberEnd = driver.FindElement(By.Name("csnum2"));
                elementCaseNumberEnd.Clear();
                elementCaseNumberEnd.SendKeys($"{yearTwoDigits}-99999");
            }

            // Open Cases Only
            driver.FindElement(By.Name("open_closed")).Click();
        }
        private void captureFileAndDownload(
            int circuit,
            IWebDriver driver,
            List<PrecedingWeekFiles> filesFromPreviousWeeks)
        {
            // get list of docket numbers to check
            var main_url = driver.Url;


            
            
            var table_element = string.Empty;
            try
            {
                table_element = driver.FindElement(By.XPath("/html/body/table[1]/tbody")).Text;
            }
            catch (Exception ex)
            {
                if (driver.PageSource.ToLower().Contains("No case found"))
                {
                    Console.WriteLine($"No cases were found in circuit number {circuit}!");
                    return;
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
                return;
            }
            if (string.IsNullOrEmpty(table_element))
            {
                Console.WriteLine($"Problem with lookup in circuit number {circuit}!");
                return;
            }

            // get list of docket numbers on webpage
            //var re = new Regex(@"\r\n(\d{2}-\d{1,})\r\n");
            var re = new Regex(@"\r\n(\d{2}-\d{1,})\r\n(.*)(?=\r\n)");

            // get first instance of each case name in pacer list
            var newDocketsFromPacer =
                from firstInstancesOfCasename in
                    (from match in re.Matches(table_element).Cast<Match>()
                     group match by match.Groups[2].Value // identify matching case names
                     into match_casenames
                     select new { matching = match_casenames.First() })
                select new
                {
                    docket = firstInstancesOfCasename.matching.Groups[1].Value,
                    casename = firstInstancesOfCasename.matching.Groups[2].Value,
                };

            // --Eliminate new dockets based solely on filenames in previous downloads
            // --There is no attempt to tap into casenames, etc.
            var docketsToClick =
                from newDockets in newDocketsFromPacer
                where !(from docketInPrevious in
                            (from file in filesFromPreviousWeeks
                             where file.CircuitNumber == circuit
                             select file.DocketNumber)
                        select docketInPrevious)
                      .Contains(newDockets.docket)
                select newDockets;

            // record main page of docket list so that program can return if error
            var main_page_url = driver.Url;

            // open and save each docket number
            int count = 0;
            bool error = false;
            bool costError = false;
            bool costErrorOnEarlyPage = false;
            foreach (var docket_itm in docketsToClick)
            {
                // method of returning to main page of docket list
                // could probably shorten to just the else if statement
                //      and remore the error variable.
                // both methods take the program home !
                if (count != 0 && !error)
                {
                    driver.Navigate().Back();
                    driver.Navigate().Back();
                    driver.Navigate().Back();
                }
                else if (costError)
                {
                    costError = false;
                    driver.Navigate().Back();
                    driver.Navigate().Back();
                    driver.Navigate().Back();
                }
                else if (costErrorOnEarlyPage)
                {
                    while (!driver.Url.Equals(main_page_url))
                    {
                        driver.Navigate().Back();
                    }
                    costErrorOnEarlyPage = false;
                }
                else if (error)
                {
                    while (!driver.Url.Equals(main_page_url))
                    {
                        driver.Navigate().Back();
                    }
                    error = false;
                }

                try
                {
                    // check for duplicates
                    var docket_itm_newfilename = System.IO.Path.Combine(DownloadDirectory, $"{circuit.ToString("00")}_{docket_itm.docket}_Docket.xml");
                    if (FilesAlreadyDownloaded.Contains(docket_itm_newfilename))
                    {
                        error = true;
                        continue;
                    }
                }
                catch { }

                try
                {
                    // DRIVER STALLS HERE WITHOUT THROWING EXCEPTION
                    // PERHAPS IT IS SELENIUM WAITING 60 SECS FOR A RESPONSE
                    // AT THE FOLLOWING LINK, THERE IS A WORKAROUND:
                    // http://stackoverflow.com/questions/31437515/selenium-stops-to-work-after-call-findelements

                    //var sw = new System.Diagnostics.Stopwatch();
                    //sw.Start();

                    //var findDocketNumOnPage = driver.FindElement(
                    //    By.CssSelector(string.Format($"a[href*='{m}']")));
                    //findDocketNumOnPage.Click();

                    // DO NOT DO THIS--IT MAY HAVE RESULTED IN EXTRA $200 CHARGE !!!
                    // driver.Navigate().Refresh(); 

                    var findDocketNumOnPage = driver.FindElement(By.CssSelector(string.Format($"a[href*='{docket_itm.docket}']")));
                    //OpenQA.Selenium.WebDriverResult.Comm




                    //var element = driver.FindElement(By.id("element-id"));
                    //var actions = new OpenQA.Selenium.Interactions.Actions(driver);
                    //actions.MoveToElement(element);

                    //actions.MoveToElement(findDocketNumOnPage);
                    //actions.Click();
                    //actions.Perform();

                    try
                    {
                        // make sure target="_blank" is turned off (added 2022)
                        IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
                        js.ExecuteScript("arguments[0].removeAttribute(\"target\")", findDocketNumOnPage);

                        // click link to docket number
                        findDocketNumOnPage.Click();
                    }
                    catch
                    {
                            var js_exec = (IJavaScriptExecutor)driver;
                            //js_exec.ExecuteScript("window.scrollBy(0, 250)", "");
                            js_exec.ExecuteScript("arguments[0].scrollIntoView(true);", findDocketNumOnPage);
                            findDocketNumOnPage.Click();
                    }
                }
                catch (Exception excpt)
                {
                    Console.WriteLine(excpt);
                    //error = true; // NEED 3 BACK CLICKS TO GET TO ORIGIN
                    costError = true;
                    continue;
                }


                // fullDocket // SEARCH PLRA, IFP, IN FORMA PAUPERIS RETURN IF FOUND
                //var text = driver.PageSource;
                //if (text.ToLower().Contains("plra")
                //    || text.ToLower().Contains("ifp")
                //    || text.ToLower().Contains("in forma pauperis"))
                //{
                //    error = true;
                //    continue;
                //}
                //else
                //{
                //    driver.FindElement(By.Name("fullDocket")).Click();
                //}


                // FIND AND CLICK "FULL DOCKET"
                //var notFoundFullDocket = false;
                IWebElement fullDocketElement = null;
                try
                {
                    // /html/body/form[2]/center/input[3]
                    // /html/body/form[2]/center
                    var _cost = driver.FindElement(By.XPath("/html/body/form[2]/center")).Text;
                    var _cost_split = _cost.Split('$');
                    _cost = _cost_split[_cost_split.Length - 1];
                    _cost = _cost.Replace(".", "");
                    var _val = int.Parse(_cost, NumberStyles.Number);
                    if (_val >= 30)
                    {
                        throw new Exception("Docket costs 0.30 or more");
                    }

                    fullDocketElement = driver.FindElement(By.Name("fullDocket"));
                }
                catch(Exception excpt)
                {
                    Console.WriteLine(excpt);
                    costErrorOnEarlyPage = true;
                    continue;
                }
                if (fullDocketElement == null)
                {
                    try
                    {
                        fullDocketElement = driver.FindElement(By.XPath("//*[contains(text(), 'Full Docket')]"));
                    }
                    catch
                    {
                        //notFoundFullDocket = true;
                    }
                    //notFoundFullDocket = false;
                }
                if(fullDocketElement == null)
                {
                    Console.WriteLine("Element not found");
                    error = true;
                    continue;
                }
                else
                {
                    fullDocketElement.Click();
                }

                try
                {
                    // June 15, 2017:
                    // Sean suggested we may save money by curtailing our search information
                    // Experiment with costs by unclicking some of the download options !!!

                    

                    // outputXML_TXT
                    driver.FindElement(By.Name("outputXML_TXT")).Click();
                }
                catch (Exception excpt)
                {
                    Console.WriteLine(excpt);
                    error = true;
                    continue;
                }
                try
                {
                    // Jan 2022: avoid Cost >= 0.30
                    // /html/body/center/center/table/tbody/tr[8]/td[2]/font 
                    var _cost = driver.FindElement(By.XPath("/html/body/center/center/table/tbody/tr[8]/td[2]/font")).Text;
                    _cost = _cost.Replace(".", "");
                    var _val = int.Parse(_cost, NumberStyles.Number);
                    if(_val >= 30)
                    {
                        throw new Exception("Docket costs 0.30 or more");
                    }
                    // submit
                    driver.FindElement(By.Name("f1")).Click();
                }
                catch (Exception excpt)
                {
                    Console.WriteLine(excpt);
                    costError = true;
                    continue;
                }
                count++;
            }

            // Add wait here so that last few downloads are allowed to complete
            System.Threading.Thread.Sleep(2000);


            
            Console.WriteLine("Filed captured from circuit " + circuit + ": " + docketsToClick.Count());
            //Console.WriteLine("New Url is: " + driver.Url);
            //Console.WriteLine("New Page Title is: " + driver.Title);
        }
        private static ChromeOptions setChromeBrowserOptions(string dwnloadDir)
        {
            // set Chrome Browser Options
            // safebrowsing.enabled: counterintuitive; it turns off warning re: saving xml files
            // default_directory: not working; it has worked, but doesn't now
            var chromeOptions = new ChromeOptions();
            chromeOptions.AddUserProfilePreference("safebrowsing.enabled", "true");
            chromeOptions.AddUserProfilePreference("download.default_directory", dwnloadDir);
            
            //chromeOptions.AddUserProfilePreference("--ash-host-window-bounds", "100+200-1024x768");
            //chromeOptions.AddUserProfilePreference("--ash-host-window-bounds", "400+400-800x800");
            //chromeOptions.AddArgument("--ash-host-window-bounds=800x800");
            //chromeOptions.AddArgument("--ash-host-window-bounds=1366x768,320x240");
            // NEW APRIL 16, 2018
            chromeOptions.AddArguments("disable-infobars");

            return chromeOptions;



        }
        #endregion
    }
}