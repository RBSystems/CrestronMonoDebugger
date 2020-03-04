using System;
using System.Collections.Generic;
using System.IO;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace CrestronMonoDebugger.Services
{
    public class ControlSystem
    {
        #region Fields

        private readonly string _host;
        private readonly int _port;
        private readonly string _username;
        private readonly string _password;

        #endregion

        #region Initialization

        public ControlSystem(string host, int port, string username, string password)
        {
            _host = host;
            _port = port;
            _username = username;
            _password = password;
        }

        #endregion

        #region Public Methods

        public void StopProgram(CrestronMonoDebuggerPackage package)
        {
            try
            {
                using (var client = new SshClient(_host, _port, _username, _password))
                {
                    client.Connect();
                    SshCommand sshCommand = client.RunCommand("stopprog -p:0");
                    if (sshCommand.ExitStatus == 0)
                    {
                        package.OutputWindowWriteLine("Remote application stopped.");
                    }
                    else
                    {
                        package.OutputWindowWriteLine($"Unable to stop the remote application. {sshCommand.Error}");
                    }
                    client.Disconnect();
                }
            }
            catch (Exception e)
            {
                package.OutputWindowWriteLine($"Unable to stop the remote application.");
                package.DebugWriteLine(e);
                throw;
            }
        }

        public void StartProgram(CrestronMonoDebuggerPackage package)
        {
            try
            {
                using (var client = new SshClient(_host, _port, _username, _password))
                {
                    client.Connect();
                    SshCommand sshCommand = client.RunCommand("progres -p:0");
                    if (sshCommand.ExitStatus == 0)
                    {
                        package.OutputWindowWriteLine("Remote application starting.");
                    }
                    else
                    {
                        package.OutputWindowWriteLine($"Unable to start the remote application. {sshCommand.Error}");
                    }
                    client.Disconnect();
                }
            }
            catch (Exception e)
            {
                package.OutputWindowWriteLine($"Unable to start the remote application.");
                package.DebugWriteLine(e);
                throw;
            }
        }

        public void EnableProgramSupport()
        {
            using (var client = new SshClient(_host, _port, _username, _password))
            {
                client.Connect();
                client.RunCommand("enableprogramcmd");
                client.RunCommand("hiddendirectory show prog0");
                client.Disconnect();
            }
        }

        public List<FileListingItem> GetFileListing(CrestronMonoDebuggerPackage package, string path)
        {
            try
            {
                IEnumerable<SftpFile> listing;

                using (var client = new SftpClient(_host, _port, _username, _password))
                {
                    client.Connect();
                    listing = client.ListDirectory(path);
                    client.Disconnect();
                }

                var fileListing = new List<FileListingItem>();

                foreach (var file in listing)
                {
                    if (file.IsRegularFile)
                    {
                        fileListing.Add(new FileListingItem(file.Name, file.LastWriteTime, file.Length));
                    }
                }

                package.OutputWindowWriteLine($"Found {fileListing.Count} file on control system.");

                return fileListing;
            }
            catch (Exception e)
            {
                package.OutputWindowWriteLine("Unable to retrieve file listing from the control system.");
                package.DebugWriteLine(e);
                throw;
            }
        }

        public void Sync(CrestronMonoDebuggerPackage package, string localPath, List<FileDeltaItem> fileListingDelta)
        {
            using (var client = new SftpClient(_host, _port, _username, _password))
            {
                client.Connect();

                foreach (FileDeltaItem deltaItem in fileListingDelta)
                {
                    try
                    {
                        if (deltaItem.Delete)
                        {
                            package.OutputWindowWriteLine($"Deleting remote file {deltaItem.Name}");
                            client.Delete($"{package.Settings.RelativePath}/{deltaItem.Name}");
                        }
                        else if (deltaItem.New)
                        {
                            package.OutputWindowWriteLine($"Uploading new file {deltaItem.Name}");
                            using (var stream = new FileStream(Path.Combine(localPath, deltaItem.Name), FileMode.Open))
                            {
                                client.UploadFile(stream, $"{package.Settings.RelativePath}/{deltaItem.Name}", true);
                            }
                        }
                        else if (deltaItem.Changed)
                        {
                            package.OutputWindowWriteLine($"Uploading changed file {deltaItem.Name}");
                            using (var stream = new FileStream(Path.Combine(localPath, deltaItem.Name), FileMode.Open))
                            {
                                client.UploadFile(stream, $"{package.Settings.RelativePath}/{deltaItem.Name}", true);
                            }
                        }
                        else
                        {
                            package.OutputWindowWriteLine($"File is unchanged: {deltaItem.Name}");
                        }
                    }
                    catch (Exception e)
                    {
                        package.OutputWindowWriteLine($"The was a problem {(deltaItem.Delete ? "deleting" : "uploading")} the file {deltaItem.Name}.");
                        package.DebugWriteLine(e);
                    }
                }

                client.Disconnect();
            }
        }

        #endregion
    }
}
