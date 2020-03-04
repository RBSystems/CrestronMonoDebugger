
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Text.Editor;

namespace CrestronMonoDebugger.Services
{
    public class LocalSystem
    {
        public List<FileDeltaItem> CreateFileListingDelta(CrestronMonoDebuggerPackage package, List<FileListingItem> remoteFileListing, string localPath)
        {
            List<FileListingItem> localFileListingSorted = GetFileListing(package, localPath).OrderBy(x => x.Name).ToList();
            var remoteFileListingSorted = remoteFileListing.OrderBy(x => x.Name).ToList();

            List<FileDeltaItem> fileListingDelta = localFileListingSorted.Select(x => x.Name)
                .Union(remoteFileListingSorted.Select(x => x.Name))
                .Select(x => new FileDeltaItem { Name = x })
                .OrderBy(x => x.Name)
                .ToList();

            int i = 0, j = 0;
            int iMax = remoteFileListingSorted.Count;
            int jMax = localFileListingSorted.Count;
            foreach (FileDeltaItem deltaItem in fileListingDelta)
            {
                if (i < iMax && remoteFileListingSorted[i].Name == deltaItem.Name)
                {
                    if (j >= jMax || localFileListingSorted[j].Name != deltaItem.Name)
                    {
                        // The file needs to be deleted from the control system
                        deltaItem.Delete = true;
                        i++;
                        continue;
                    }
                }

                if (j < jMax && localFileListingSorted[j].Name == deltaItem.Name)
                {
                    if (i >= iMax || remoteFileListingSorted[i].Name != deltaItem.Name)
                    {
                        // The file needs to be added to the control system
                        deltaItem.New = true;
                        j++;
                        continue;
                    }
                }

                if (i < iMax && j < jMax)
                {
                    if ((remoteFileListingSorted[i].Name == deltaItem.Name && localFileListingSorted[j].Name == deltaItem.Name)
                      && (remoteFileListingSorted[i].Timestamp != localFileListingSorted[j].Timestamp
                          || remoteFileListingSorted[i].Size != localFileListingSorted[j].Size))
                    {
                        deltaItem.Changed = true;
                        i++;
                        j++;
                    }
                }
            }

            return fileListingDelta;
        }

        public List<FileListingItem> GetFileListing(CrestronMonoDebuggerPackage package, string localPath)
        {
            string[] fileNames = Directory.GetFiles(localPath);

            var fileListing = new List<FileListingItem>();

            foreach (var filename in fileNames)
            {
                var fileInfo = new FileInfo(filename);

                fileListing.Add(new FileListingItem(fileInfo.Name, fileInfo.LastWriteTime, fileInfo.Length));
            }

            package.OutputWindowWriteLine($"Found {fileListing.Count} file on control system.");

            return fileListing;
        }
    }
}
