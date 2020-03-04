using System;

namespace CrestronMonoDebugger.Services
{
    public class FileListingItem
    {
        #region Properties

        public string Name { get; }
        
        public DateTime Timestamp { get; }
        
        public long Size { get; }

        #endregion

        #region Initialization

        public FileListingItem(string name, DateTime timestamp, long size)
        {
            Name = name;
            Timestamp = timestamp;
            Size = size;
        }
        #endregion
    }
}