using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CircuitCourtLookupMvvm.Models
{
    public class WorkingFolder : INotifyPropertyChanged
    {
        #region FIELDS
        private string foldernameShort;
        private string foldernameComplete;
        private DateTime? xmlDownloadDateDateTime;
        private int countXmlFiles;
        private bool hasWordMergeFile;
        private bool hasExcelSpreadsheetFile;
        #endregion

        #region PROPERTIES
        public string FoldernameShort
        {
            get { return foldernameShort; }
            set
            {
                foldernameShort = value;
                RaisePropertyChanged();
            }
        }
        public string FoldernameComplete
        {
            get { return foldernameComplete; }
            set
            {
                foldernameComplete = value;
                RaisePropertyChanged();
            }
        }
        public string XmlDownloadDateString
        {
            get
            {
                return null != XmlDownloadDateDateTime
                    ? string.Format("{0:MMM d, yyyy}", XmlDownloadDateDateTime) : "";
            }
        }
        public DateTime? XmlDownloadDateDateTime
        {
            get { return xmlDownloadDateDateTime; }
            set
            {
                xmlDownloadDateDateTime = value;
                RaisePropertyChanged();
            }
        }
        public int CountXmlFiles
        {
            get { return countXmlFiles; }
            set
            {
                countXmlFiles = value;
                RaisePropertyChanged();
            }
        }
        public bool HasWordMergeFile
        {
            get { return hasWordMergeFile; }
            set
            {
                hasWordMergeFile = value;
                RaisePropertyChanged();
            }
        }
        public bool HasExcelSpreadsheetFile
        {
            get { return hasExcelSpreadsheetFile; }
            set
            {
                hasExcelSpreadsheetFile = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region INTERFACE
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
