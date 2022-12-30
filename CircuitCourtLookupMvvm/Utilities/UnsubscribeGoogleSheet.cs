using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Sheets.v4;

namespace CircuitCourtLookupMvvm.Utilities
{
    internal class UnsubscribeGoogleSheet
    {
        static readonly string[] Scopes = { SheetsService.Scope.Spreadsheets };
        static readonly string ApplicationName = "Unsubscribers";
        static readonly string SpreadsheetID = "1sw9S3XSyJvtbjSjGTjlArFQiCR0WYbF0t4VqP88DjgM";
        static readonly string sheet = "Sheet1";
        static SheetsService service;

        public UnsubscribeGoogleSheet()
        {
            GoogleCredential credential;
            // C:\Users\d4oke\apps\CircuitCourtLookupMvvm\lg-sheetdata-134f80ad81e4.json
            using (var stream = new FileStream(@"C:\Users\d4oke\apps\CircuitCourtLookupMvvm\lg-sheetdata-134f80ad81e4.json", FileMode.Open, FileAccess.Read))
            {
                credential = GoogleCredential.FromStream(stream)
                    .CreateScoped(Scopes);
            }
            service = new SheetsService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            Unsubscribed_Names = new List<string>();
            Unsubscribed_Emails = new List<string>();

            ReadEntries();
        }

        public List<string> Unsubscribed_Names { get; internal set; }
        public List<string> Unsubscribed_Emails { get; internal set; }

        private void ReadEntries()
        {
            var range = $"{sheet}!A1:B100";
            var request = service.Spreadsheets.Values.Get(SpreadsheetID, range);

            var response = request.Execute();
            var values = response.Values;
            if(values != null && values.Count > 0)
            {
                foreach(var row in values)
                {
                    Unsubscribed_Names.Add((string)row[0]);
                    Unsubscribed_Emails.Add((string)row[1]);
                    Console.WriteLine("{0} | {1}", row[0], row[1]);
                }
            }
            else
            {
                Console.WriteLine("No data found");
            }
        }
    }
}
