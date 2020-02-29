using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace CrestronMonoDebugger.Settings
{
    public class OptionPageGrid : DialogPage
    {
        [Category("Control System")]
        [DisplayName("Host IP")]
        [Description("IP Address of the Control System")]
        public int IpAddress { get; set; } = 256;

        [Category("Control System")]
        [DisplayName("Host Port")]
        [Description("SSL Port Number of the Control System")]
        public int Port { get; set; } = 22;

    }
}