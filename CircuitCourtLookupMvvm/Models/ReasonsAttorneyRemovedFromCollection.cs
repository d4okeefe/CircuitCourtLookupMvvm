using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CircuitCourtLookupMvvm.Models
{
    class ReasonsAttorneyRemovedFromCollection
    {
        public int BadCircuitNumber { get; set; }
        public int BadCaption { get; set; }
        public int BadNoticeInfo { get; set; }
        public int BadZipCode { get; set; }
        public int BadPartyType { get; set; }
        public int RepeatPartyInfo { get; set; }
        public int RepeatPartyType { get; set; }
        public int RepeatFullName { get; set; }
        public int RepeatFullAddress { get; set; }
        public int GovernmentAttorney { get; set; }
        public int Prisoner { get; set; }
        public int PublicDefender { get; set; }
        public int AddressInReturnedList { get; set; }
        public int NoEmail { get; set; } 
        public override string ToString()
        {
            return "REASONS ATTORNEYS ELIMINATED FROM LETTERS\r\n" +
                BadCircuitNumber + "\t: BadCircuitNumber\r\n" +
                BadCaption + "\t: BadCaption\r\n" +
                BadNoticeInfo + "\t: BadNoticeInfo\r\n" +
                BadZipCode + "\t: BadZipCode\r\n" +
                BadPartyType + "\t: BadPartyType\r\n" +
                RepeatPartyInfo + "\t: RepeatPartyInfo\r\n" +
                RepeatPartyType + "\t: RepeatPartyType\r\n" +
                RepeatFullName + "\t: RepeatFullName\r\n" +
                RepeatFullAddress + "\t: RepeatFullAddress\r\n"+
                GovernmentAttorney + "\t: GovernmentAttorney\r\n"+
                AddressInReturnedList + "\t: AddressInReturnedList\r\n";
        }
    }
}
