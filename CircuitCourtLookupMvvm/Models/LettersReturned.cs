using System.Collections.Generic;
using System.Linq;

namespace CircuitCourtLookupMvvm.Models
{
    public class LettersReturned
    {
        public List<AttorneyInfoPacerSentAddress> ListOfAttyInfo { get; private set; }
        public LettersReturned()
        {
            using(var cxn = new PacerSentLettersDbContextLinqToSqlDataContext())
            {
                
                ListOfAttyInfo = cxn.AttorneyInfoPacerSentAddresses.Where(x => null != x
                    && x.LetterReturned).ToList();
            }

            //if(ListOfAttyInfo.Any(x=>x.FullName.Contains("Taki")))
            //{
            //    System.Console.WriteLine("Found");
            //}
        }
    }
}
