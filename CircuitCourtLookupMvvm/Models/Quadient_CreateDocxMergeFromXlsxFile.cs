using System;
using Word = Microsoft.Office.Interop.Word;

namespace CircuitCourtLookupMvvm.Models
{
    internal class Quadient_CreateDocxMergeFromXlsxFile
    {
        public Quadient_CreateDocxMergeFromXlsxFile(string sourceSpreadsheet, string sourseTemplate, string destinationWordFile)
        {
            // create app object
            Word._Application app = new Word.Application();
            try
            {
                // open template
                object filename = sourseTemplate;
                Word._Document doc = app.Documents.Open(ref filename);

                // set up mailmerge
                doc.MailMerge.MainDocumentType = Word.WdMailMergeMainDocType.wdFormLetters;
                doc.MailMerge.Destination = Word.WdMailMergeDestination.wdSendToNewDocument;

                // connect mailmerge to source & execute
                string dataSource = sourceSpreadsheet;
                object query = "select * from [Cert_Letters$]";
                object subType = Word.WdMergeSubType.wdMergeSubTypeOther;
                doc.MailMerge.OpenDataSource(Name: dataSource, SQLStatement: query, SubType: subType);
                doc.MailMerge.Execute();

                // close template
                object saveChanges = Word.WdSaveOptions.wdDoNotSaveChanges;
                doc.Close(SaveChanges: saveChanges);

                // capture filename as only open doc in app
                object index = 1;
                doc = app.Documents.get_Item(Index: index);
                object newfileName = destinationWordFile;

                // make edits to doc
                Word.Range range = doc.Content;
                range.Find.Execute(FindText: "^l^l", ReplaceWith: "^l", Replace: Word.WdReplace.wdReplaceAll);
                range.Find.Execute(FindText: "^l^p", ReplaceWith: "^p", Replace: Word.WdReplace.wdReplaceAll);
                range.Find.Execute(FindText: "'", ReplaceWith: "'", Replace: Word.WdReplace.wdReplaceAll);
                range.Find.Execute(FindText: "\"", ReplaceWith: "\"", Replace: Word.WdReplace.wdReplaceAll);
                range.Find.Execute(FindText: "'", ReplaceWith: "'", Replace: Word.WdReplace.wdReplaceAll);

                // Caption
                try
                {
                    Word.Range caption_range = doc.Content;
                    foreach (Word.Section section in doc.Sections)
                    {
                        Word.Range r_section = section.Range;
                        var r_find = r_section.Find;
                        r_find.ClearFormatting();
                        r_find.Text = "(#CaptionStart#)*(#CaptionEnd#)";
                        r_find.MatchWildcards = true;
                        r_find.Execute();
                        while (r_find.Found)
                        {
                            long lstart = r_section.Start;
                            long lend = r_section.End;
                            Word.Range subrange = doc.Range(Start: lstart, End: lend);
                            subrange.Find.Execute(FindText: "^p", ReplaceWith: "^l", Replace: Word.WdReplace.wdReplaceAll);
                            subrange.Find.Execute(FindText: "#CaptionStart#", ReplaceWith: "", Replace: Word.WdReplace.wdReplaceAll);
                            subrange.Find.Execute(FindText: "#CaptionEnd#", ReplaceWith: "", Replace: Word.WdReplace.wdReplaceAll);
                            r_find.Execute();
                        }
                    }
                    caption_range = null;
                }
                catch (Exception excpt)
                {
                    Console.WriteLine(excpt);
                }
                // LowerCourtStart LowerCourtEnd
                try
                {
                    Word.Range lowercourt_range = doc.Content;
                    foreach (Word.Section section in doc.Sections)
                    {
                        Word.Range r_section = section.Range;
                        var r_find = r_section.Find;
                        r_find.ClearFormatting();
                        r_find.Text = "(#LowerCourtStart#)*(#LowerCourtEnd#)";
                        r_find.MatchWildcards = true;
                        r_find.Execute();
                        while (r_find.Found)
                        {
                            long lstart = r_section.Start;
                            long lend = r_section.End;
                            Word.Range subrange = doc.Range(Start: lstart, End: lend);
                            subrange.Find.Execute(FindText: "^p", ReplaceWith: "^l", Replace: Word.WdReplace.wdReplaceAll);
                            subrange.Find.Execute(FindText: "#LowerCourtStart#", ReplaceWith: "", Replace: Word.WdReplace.wdReplaceAll);
                            subrange.Find.Execute(FindText: "#LowerCourtEnd#", ReplaceWith: "", Replace: Word.WdReplace.wdReplaceAll);
                            r_find.Execute();
                        }
                    }
                    lowercourt_range = null;
                }
                catch (Exception excpt)
                {
                    Console.WriteLine(excpt);
                }
                range = null;

                // save doc & close
                doc.SaveAs2(FileName: newfileName);
                doc.Close();
            }
            catch (Exception excpt)
            {
                Console.WriteLine(excpt);
            }
            finally
            {
                // close app
                object saveOptions = Word.WdSaveOptions.wdDoNotSaveChanges;
                app.Quit(SaveChanges: saveOptions);
            }
        }
    }
}
