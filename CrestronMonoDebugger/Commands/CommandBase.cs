using System;
using Microsoft.VisualStudio.Shell.Interop;

namespace CrestronMonoDebugger.Commands
{
    internal class CommandBase
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        protected readonly Guid CommandSet = new Guid("2a1630cf-d13c-496f-b736-876beb6b9bfb");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        protected CrestronMonoDebuggerPackage Package { get; set; }
    }
}