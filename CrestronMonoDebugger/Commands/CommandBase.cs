using System;

namespace CrestronMonoDebugger.Commands
{
    internal class CommandBase
    {
        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        protected readonly Guid CommandSet = new Guid("2a1630cf-d13c-496f-b736-876beb6b9bfb");
    }
}