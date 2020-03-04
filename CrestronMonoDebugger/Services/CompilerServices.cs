using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace CrestronMonoDebugger.Services
{
    public class CompilerServices
    {
        #region Fields

        private readonly Timer _buildTimer;
        private Solution _solution;

        /// <summary>
        /// see https://www.codeproject.com/Reference/720512/List-of-Visual-Studio-Project-Type-GUIDs
        /// </summary>
        private readonly string ProjectKindSolutionFolder = "{66A26720-8FB5-11D2-AA7E-00C04F688DDE}";

        #endregion

        #region Events

        public event EventHandler<bool> BuildComplete;

        #region Invocators

        protected virtual void OnBuildComplete(bool success)
        {
            BuildComplete?.Invoke(this, success);
        }

        #endregion

        #endregion

        #region Initialization

        public CompilerServices()
        {
            _buildTimer = new Timer(CheckBuildState, null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        }

        #endregion

        #region Public Methods

        public bool IsSolutionOpen()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            return dte?.Solution != null && dte.Solution.IsOpen;
        }

        public string GetOutputFolder(CrestronMonoDebuggerPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Project startupProject = GetStartupProject(package);
            return GetFullOutputPath(startupProject);
        }

        public void Build(CrestronMonoDebuggerPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            // Get the DTE service
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            if (dte?.Solution == null)
            {
                package.OutputWindowWriteLine("Unable to locate the solution.");
                throw new InvalidOperationException("Unable to locate the solution.");
            }

            _solution = dte.Solution;

            string activeConfiguration = _solution.SolutionBuild.ActiveConfiguration.Name;
            if (activeConfiguration != "Debug" && activeConfiguration != "Release")
            {
                package.OutputWindowWriteLine("The active configuration must be either Debug or Release.");
                throw new InvalidOperationException("The active configuration must be either Debug or Release.");
            }

            // start the build
            _solution.SolutionBuild.Build();

            // wait until the status shows that the build is in progress
            while (_solution.SolutionBuild.BuildState != vsBuildState.vsBuildStateInProgress)
            {
                System.Threading.Thread.Sleep(100);
            }

            _buildTimer.Change(TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
        }

        public void CreateMdbFiles(CrestronMonoDebuggerPackage package, string localPath, List<FileListingItem> localFileListing)
        {
            // Get the DTE service
            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            if (dte?.Solution == null)
            {
                package.OutputWindowWriteLine("Unable to locate the solution.");
                throw new InvalidOperationException("Unable to locate the solution.");
            }

            foreach (FileListingItem file in localFileListing)
            {
                // only convert dll and exe files
                if (Path.GetExtension(file.Name) != ".dll" && Path.GetExtension(file.Name) != ".exe")
                {
                    continue;
                }

                // check to if there is also a related pdb file.
                string pdbFile = Path.ChangeExtension(file.Name, ".pdb");
                if (!string.IsNullOrEmpty(pdbFile))
                {
                    string pdbFileFullPath = Path.Combine(localPath, pdbFile);
                    if (File.Exists(pdbFileFullPath))
                    {
                        // perform the conversion
                        Pdb2Mdb.Converter.Convert(Path.Combine(localPath, file.Name));
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        private Project GetStartupProject(CrestronMonoDebuggerPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = Package.GetGlobalService(typeof(SDTE)) as DTE2;

            if (dte?.Solution == null)
            {
                package.OutputWindowWriteLine("Unable to locate the solution.");
                throw new InvalidOperationException("Unable to locate the solution.");
            }

            SolutionBuild2 sb = (SolutionBuild2)dte.Solution.SolutionBuild;
            List<string> startupProjects = ((Array)sb.StartupProjects).Cast<string>().ToList();

            try
            {
                var projects = Projects(dte.Solution);
                foreach (var project in projects)
                {
                    if (startupProjects.Contains(project.UniqueName))
                    {
                        if (IsCSharpProject(package, project))
                        {
                            // We are only support one C# project at once
                            return project;
                        }
                        else
                        {
                            package.DebugWriteLine($"Only C# projects are supported as startup project! ProjectName = {project.Name} Language = {project.CodeModel.Language}");
                        }
                    }
                }
            }
            catch (ArgumentException aex)
            {
                package.OutputWindowWriteLine($"No startup project extracted! The parameter StartupProjects = '{string.Join(",", startupProjects.ToArray())}' is incorrect.");
                package.DebugWriteLine(aex);
                throw new ArgumentException($"No startup project extracted! The parameter StartupProjects = '{string.Join(",", startupProjects.ToArray())}' is incorrect.", aex);
            }

            package.OutputWindowWriteLine($"No startup project found! Checked projects in StartupProjects = '{string.Join(",", startupProjects.ToArray())}'");
            throw new ArgumentException($"No startup project found! Checked projects in StartupProjects = '{string.Join(",", startupProjects.ToArray())}'");
        }

        private IList<Project> Projects(Solution solution)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Projects projects = solution.Projects;
            List<Project> list = new List<Project>();
            var item = projects.GetEnumerator();
            while (item.MoveNext())
            {
                var project = item.Current as Project;
                if (project == null)
                {
                    continue;
                }

                if (project.Kind == ProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(project));
                }
                else
                {
                    list.Add(project);
                }
            }

            return list;
        }

        private IEnumerable<Project> GetSolutionFolderProjects(Project solutionFolder)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            List<Project> list = new List<Project>();
            for (var i = 1; i <= solutionFolder.ProjectItems.Count; i++)
            {
                var subProject = solutionFolder.ProjectItems.Item(i).SubProject;
                if (subProject == null)
                {
                    continue;
                }

                // If this is another solution folder, do a recursive call, otherwise add
                if (subProject.Kind == ProjectKindSolutionFolder)
                {
                    list.AddRange(GetSolutionFolderProjects(subProject));
                }
                else
                {
                    list.Add(subProject);
                }
            }
            return list;
        }

        private bool IsCSharpProject(CrestronMonoDebuggerPackage package, Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            try
            {
                return project.CodeModel.Language == CodeModelLanguageConstants.vsCMLanguageCSharp;
            }
            catch (Exception ex)
            {
                package.OutputWindowWriteLine($"Project doesn't support property project.CodeModel.Language! No CSharp project. {ex.Message}");
                return false;
            }
        }

        private string GetFullOutputPath(Project project)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            string outputPath;
            try
            {
                outputPath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            }
            catch
            {
                return string.Empty;
            }

            var fullPath = string.Empty;
            foreach (Property property in project.Properties)
            {
                try
                {
                    if (property.Name == "FullPath" || (property.Name == "" && string.IsNullOrEmpty(fullPath)))
                    {
                        fullPath = property.Value?.ToString();
                    }
                }
                catch
                {
                    // ignored
                }
            }

            return string.IsNullOrWhiteSpace(fullPath) ? outputPath : Path.Combine(fullPath, outputPath);
        }


        private void CheckBuildState(object state)
        {
            if (_solution == null)
            {
                _buildTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
                return;
            }

            if (_solution?.SolutionBuild?.BuildState != vsBuildState.vsBuildStateDone)
            {
                return;
            }

            _buildTimer.Change(Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            OnBuildComplete(_solution?.SolutionBuild?.LastBuildInfo == 0);
        }

        #endregion
    }
}
