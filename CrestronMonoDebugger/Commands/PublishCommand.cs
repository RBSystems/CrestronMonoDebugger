using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CrestronMonoDebugger.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PublishCommand : CommandBase
    {
        #region Properties, Fields and Constants

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PublishCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private Microsoft.VisualStudio.Shell.IAsyncServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Command ID.
        /// </summary>
        private const int CommandId = 0x1110;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PublishCommand(CrestronMonoDebuggerPackage package, OleMenuCommandService commandService)
        {
            Package = package ?? throw new ArgumentNullException(nameof(package));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            Initialize(commandService);
        }

        private void Initialize(OleMenuCommandService commandService)
        {
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static async Task InitializeAsync(CrestronMonoDebuggerPackage package)
        {
            // Switch to the main thread - the call to AddCommand in PublishCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PublishCommand(package, commandService);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                string message = $"IP: {Package.Settings.IpAddress}";
                string title = "Publish Command";

                Package.OutputWindowWriteLine($"{title} - {message}");

                // Show a message box to prove we were here
                VsShellUtilities.ShowMessageBox(
                    Package,
                    message,
                    title,
                    OLEMSGICON.OLEMSGICON_INFO,
                    OLEMSGBUTTON.OLEMSGBUTTON_OK,
                    OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            }
            catch (Exception ex)
            {
                Package.OutputWindowWriteLine("Unable to publish.  An unknown error occured.");
                Package.DebugWriteLine(ex);
            }
        }

        #endregion
    }
}
