using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using FileManagment.FileSystem.Structure;
using FileManagment.FileSystem.Persistence;
using FileManagment.FileSystem.Managers;

namespace FileManagment.FileSystem
{
    public class Context
    {
        private string _containerPath;
        private FileManager _fileManager;
        private DirectoryManager _dirManager;

        private const int MetadataEntrySize = 
            Constants.MaxFileNameLength 
            + Constants.SizeLength 
            + Constants.OffsetLength
            + Constants.CheckSumLenght
            + Constants.TypeLength 
            + Constants.ParentIdLength
            + Constants.NextSlotIdLength;

        // Reserving space for metadata entries to prevent overlap with data
        private const int DataStartOffset = Constants.MetadataStart + Constants.MaxFiles * MetadataEntrySize;

        public int CurrentFolderID { get; set; } = Constants.RootDirectory; // -1 represents the root

        public Context()
        {
            _containerPath = Constants.ContainerFileName;

            if (!File.Exists(_containerPath))
            {
                Initializer.InitializeContainer(_containerPath, MetadataEntrySize);
            }

            _fileManager = new FileManager(_containerPath, CurrentFolderID, MetadataEntrySize, DataStartOffset);
            _dirManager = new DirectoryManager(_containerPath, CurrentFolderID, MetadataEntrySize, DataStartOffset);
        }

        // Files functions
        public void WriteFile(string sourceFilePath, string targetName)
        {
            _fileManager.CurrentFolderID = this.CurrentFolderID;
            _fileManager.WriteFile(sourceFilePath, targetName);
        }

        public void ReadFile(string sourceName, string destinationPath)
        {
            _fileManager.CurrentFolderID = this.CurrentFolderID;
            _fileManager.ReadFile(sourceName, destinationPath);
        }

        public void RemoveFile(string fileName)
        {
            _fileManager.CurrentFolderID = this.CurrentFolderID;
            _fileManager.RemoveFile(fileName);
        }

        // Directories functions
        public void MakeDirectory(string dirName)
        {
            _dirManager.CurrentFolderID = this.CurrentFolderID;
            _dirManager.MakeDirectory(dirName);
        }

        public void ChangeDirectory(string dirName)
        {
            _dirManager.CurrentFolderID = this.CurrentFolderID;
            _dirManager.ChangeDirectory(dirName);
            this.CurrentFolderID = _dirManager.CurrentFolderID;
        }

        public void RemoveDirectory(string dirName)
        {
            _dirManager.CurrentFolderID = this.CurrentFolderID;
            _dirManager.RemoveDirectory(dirName);
        }

        // Shared functions
        public IEnumerable<Metadata.MetadataRecord> ListCurrentDirectory()
        {
            _dirManager.CurrentFolderID = this.CurrentFolderID;
            return _dirManager.ListCurrentDirectory();
        }
    }
}