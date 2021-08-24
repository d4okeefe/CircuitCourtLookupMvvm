using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CircuitCourtLookupMvvm.Viewmodels
{
    public abstract class ViewmodelBase : INotifyPropertyChanged
    {
        // INTERFACE ONLY
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void RaisePropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
