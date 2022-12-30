﻿#pragma checksum "..\..\..\Views\LookupView.xaml" "{8829d00f-11b8-4213-878b-770e8597ac16}" "F3EE227FA75448F3AB8E6A1861D50743FF0A95C0ED5C36BE267C40EF3DE91B16"
//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using CircuitCourtLookupMvvm.Behaviors;
using CircuitCourtLookupMvvm.Converters;
using CircuitCourtLookupMvvm.Utilities;
using CircuitCourtLookupMvvm.Viewmodels;
using CircuitCourtLookupMvvm.Views;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace CircuitCourtLookupMvvm.Views {
    
    
    /// <summary>
    /// LookupView
    /// </summary>
    public partial class LookupView : System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector {
        
        
        #line 49 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ComboBox circuitCourtValues;
        
        #line default
        #line hidden
        
        
        #line 57 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid rangeStackPanel;
        
        #line default
        #line hidden
        
        
        #line 63 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBox circuitRangeLowValueTextBox;
        
        #line default
        #line hidden
        
        
        #line 73 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.TextBlock circuitRangeHighValueTextBox;
        
        #line default
        #line hidden
        
        
        #line 85 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DatePicker startDatePicker;
        
        #line default
        #line hidden
        
        
        #line 89 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.DatePicker endDatePicker;
        
        #line default
        #line hidden
        
        
        #line 95 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelForListBoxTab1;
        
        #line default
        #line hidden
        
        
        #line 189 "..\..\..\Views\LookupView.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.StackPanel stackPanelForListBoxTab2;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/CircuitCourtLookupMvvm;component/views/lookupview.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\..\Views\LookupView.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal System.Delegate _CreateDelegate(System.Type delegateType, string handler) {
            return System.Delegate.CreateDelegate(delegateType, this, handler);
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            this.circuitCourtValues = ((System.Windows.Controls.ComboBox)(target));
            return;
            case 2:
            this.rangeStackPanel = ((System.Windows.Controls.Grid)(target));
            return;
            case 3:
            this.circuitRangeLowValueTextBox = ((System.Windows.Controls.TextBox)(target));
            return;
            case 4:
            this.circuitRangeHighValueTextBox = ((System.Windows.Controls.TextBlock)(target));
            return;
            case 5:
            this.startDatePicker = ((System.Windows.Controls.DatePicker)(target));
            return;
            case 6:
            this.endDatePicker = ((System.Windows.Controls.DatePicker)(target));
            return;
            case 7:
            this.stackPanelForListBoxTab1 = ((System.Windows.Controls.StackPanel)(target));
            return;
            case 8:
            this.stackPanelForListBoxTab2 = ((System.Windows.Controls.StackPanel)(target));
            return;
            }
            this._contentLoaded = true;
        }
    }
}
