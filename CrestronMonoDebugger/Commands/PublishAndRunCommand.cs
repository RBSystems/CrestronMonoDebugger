using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using CrestronMonoDebugger.Services;
using CrestronMonoDebugger.Settings;
using Microsoft.VisualStudio.Shell;
using Task = System.Threading.Tasks.Task;

namespace CrestronMonoDebugger.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PublishAndRunCommand : CommandBase
    {
        #region Properties, Fields and Constants

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PublishAndRunCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IAsyncServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Command ID.
        /// </summary>
        private const int CommandId = 0x1110;

        private CompilerServices _compilerServices;

        private ControlSystem _controlSystem;

        private OptionPageGrid Settings => Package?.Settings;

        #endregion

        #region Initialization

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishAndRunCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private PublishAndRunCommand(CrestronMonoDebuggerPackage package, OleMenuCommandService commandService)
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
            // Switch to the main thread - the call to AddCommand in PublishAndRunCommand's constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new PublishAndRunCommand(package, commandService);
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
                _compilerServices = new CompilerServices();
                _controlSystem = new ControlSystem(Settings.Host, Settings.Port, Settings.Username, Settings.Password);

                //Check to see if a solution is open, if not, exit
                if (!_compilerServices.IsSolutionOpen())
                {
                    return;
                }

                Package.OutputWindowWriteLine($"Publishing to {Package.Settings.Host}.");

                //Stop the application on the server
                _controlSystem.StopProgram(Package);

                //Compile solution
                _compilerServices.BuildComplete += CompilerServices_BuildComplete;
                _compilerServices.Build(Package);
            }
            catch (Exception ex)
            {
                Package.OutputWindowWriteLine("Unable to publish.  An unknown error occured.");
                Package.DebugWriteLine(ex);
            }
        }

        private async void CompilerServices_BuildComplete(object sender, bool success)
        {
            _compilerServices.BuildComplete -= CompilerServices_BuildComplete;

            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(Package.DisposalToken);

            if (!success)
            {
                Package.OutputWindowWriteLine("The build failed. Fix and retry.");
            }

            var localSystem = new LocalSystem();

            //Create mdb files from pdb files.  (Mono requires mdb files)
            string localPath = _compilerServices.GetOutputFolder(Package);
            List<FileListingItem> localFileListing = localSystem.GetFileListing(Package, localPath);
            _compilerServices.CreateMdbFiles(Package, localPath, localFileListing);

            //Get a list of files (name, date, size) from the control system
            List<FileListingItem> remoteFileListing = _controlSystem.GetFileListing(Package, Settings.RelativePath);

            //Determine what files need to be removed from the control system
            List<FileDeltaItem> delta = localSystem.CreateFileListingDelta(Package, remoteFileListing, localPath);

            //Sync control system with local system
            _controlSystem.Sync(Package, localPath, delta);

            //Start program on control system
            _controlSystem.StartProgram(Package);

            Package.OutputWindowWriteLine("Finished.");
        }


        #endregion
    }
}
