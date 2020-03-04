using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using CrestronMonoDebugger.Annotations;
using Microsoft.VisualStudio.Shell;

namespace CrestronMonoDebugger.Settings
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    [Guid("911e6009-e93c-488c-ae21-2e0171b47598")]
    public class OptionPageGrid : UIElementDialogPage, INotifyPropertyChanged
    {
        #region Fields

        private string _host;
        private int _port;
        private string _username;
        private string _password;
        private string _relativePath;

        #endregion

        #region Events

        /// <summary>
        /// Inherited event from INotifyPropertyChanged.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        [Category("Connection")]
        [DisplayName("Host")]
        [Description("Nost name or port of the control system")]
        public string Host
        {
            get => _host;
            set
            {
                if (_host == value)
                    return;

                _host = value;
                OnPropertyChanged();
            }
        }

        [Category("Connection")]
        [DisplayName("Port")]
        [Description("SSL port number of the control system")]
        public int Port
        {
            get => _port;
            set
            {
                if (_port == value)
                    return;

                _port = value;
                OnPropertyChanged();
            }
        }

        [Category("Connection")]
        [DisplayName("Username")]
        [Description("Username for SSL connection")]
        public string Username
        {
            get => _username;
            set
            {
                if (value == _username) 
                    return;

                _username = value;
                OnPropertyChanged();
            }
        }

        [Category("Connection")]
        [DisplayName("Password")]
        [Description("Password for SSL connection")]
        public string Password
        {
            get => _password;
            set
            {
                if (Equals(value, _password)) 
                    return;

                _password = value;
                OnPropertyChanged();
            }
        }

        [Category("Program")]
        [DisplayName("Path")]
        [Description("Path to the program files.")]
        public string RelativePath
        {
            get => _relativePath;
            set
            {
                if (value == _relativePath) 
                    return;

                _relativePath = value;
                OnPropertyChanged();
            }
        }

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region Overridden Functions

        /// <summary>
        /// Gets the Windows Presentation Foundation (WPF) child element to be hosted inside the Options dialog page.
        /// </summary>
        /// <returns>The WPF child element.</returns>
        protected override UIElement Child => new OptionPageUserControl(this);

        /// <summary>
        /// Should be overridden to reset settings to their default values.
        /// </summary>
        public override void ResetSettings()
        {
            Host = string.Empty;
            Port = 22;
            Username = "admin";
            Password = string.Empty;
            RelativePath = "program0";

            base.ResetSettings();
        }

        #endregion
    }
}