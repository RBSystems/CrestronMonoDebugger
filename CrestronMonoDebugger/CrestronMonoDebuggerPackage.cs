using System;
using System.Runtime.InteropServices;
using System.Threading;
using CrestronMonoDebugger.Commands;
using CrestronMonoDebugger.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace CrestronMonoDebugger
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(CrestronMonoDebuggerPackage.PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Crestron", "Mono Debugger", 0, 0, true)]
    public sealed class CrestronMonoDebuggerPackage : AsyncPackage
    {
        #region Constants

        /// <summary>
        /// CrestronMonoDebuggerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "bd6ff629-c457-4390-8a42-cdeb97e668cd";

        #endregion

        #region Properties

        public OptionPageGrid Settings => (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));

        #endregion

        #region Public Methods

        public void DebugWriteLine(object message)
        {
#if DEBUG
            OutputWindowWriteLine($"[DEBUG] {message}");
#endif
        }

        public void OutputWindowWriteLine(object message)
        {
            ThreadHelper.JoinableTaskFactory.Run(async delegate
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

                const int visible = 1;
                const int doNotClearWithSolution = 0;

                var guidPane = new Guid("50C4E395-4E87-48BC-9BAC-7C4CD065F6E8");

                // Get the output window
                if (await GetServiceAsync(typeof(SVsOutputWindow)) is IVsOutputWindow outputWindow)
                {
                    // The General pane is not created by default. We must force its creation
                    int returnValue = outputWindow.CreatePane(guidPane, "Crestron Mono Debugger", visible, doNotClearWithSolution);
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(returnValue);

                    // Get the pane
                    returnValue = outputWindow.GetPane(guidPane, out var outputWindowPane);
                    Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(returnValue);

                    // Output the message
                    if (outputWindowPane != null)
                    {
                        outputWindowPane.Activate();
                        outputWindowPane.OutputString($"{DateTime.Now:T}: {message}{Environment.NewLine}");
                    }
                }
            });
        }

        #endregion

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            await PublishAndRunCommand.InitializeAsync(this);
            await PublishAndDebugCommand.InitializeAsync(this);
        }

        #endregion
    }
}
