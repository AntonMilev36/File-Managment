using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagment.Utils;

namespace FileManagment.FileSystem.Managers
{
    public class DirectoryManager : BaseManager
    {
        public DirectoryManager(string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset) : base(containerPath, folderId, MetadataEntrySize, DataStartOffset)
        {
        }
        public void MakeDirectory(string dirName)
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();
                if (count >= Constants.MaxFiles)
                    throw new Exception("Container is full.");

                int slotIndex = FindFreeSlot(reader, stream);

                // write Directory Metadata
                stream.Seek(Constants.MetadataStart + slotIndex * _MetadataEntrySize, SeekOrigin.Begin);

                byte[] nameBytes = new byte[Constants.MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(dirName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, Constants.MaxFileNameLength));

                writer.Write(nameBytes);
                writer.Write((long)0);
                writer.Write((long)-1);
                writer.Write((byte)Structure.FsObjectType.Directory);
                writer.Write(CurrentFolderID);

                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(count + 1);
            }
        }

        public void ChangeDirectory(string dirName)
        {
            if (dirName == "..")
            {
                if (CurrentFolderID == Constants.RootDirectory) return; // Already at root

                // Find current folder's record to see who its parent is
                var currentFolder = GetRecordById(CurrentFolderID);
                CurrentFolderID = currentFolder.ParentId;
            }
            else if (dirName == "/" || dirName == "\\")
            {
                CurrentFolderID = Constants.RootDirectory;
            }
            else
            {
                // Use the ls logic you already wrote to find the child
                var children = ListCurrentDirectory();
                bool found = false;

                foreach (var child in children)
                {
                    if (child.Name == dirName && child.Type == Structure.FsObjectType.Directory)
                    {
                        CurrentFolderID = child.Id;
                        found = true;
                        break;
                    }
                }

                if (!found) 
                    throw new Exception($"Directory '{dirName}' not found.");
            }
        }

        public void RemoveDirectory(string dirName)
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int currentCount = reader.ReadInt32();
                int targetSlotIndex = -1;

                // Step 1: Find the directory in the CURRENT folder
                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    stream.Seek(Constants.MetadataStart + i * _MetadataEntrySize, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                    // Skip size and offset
                    stream.Seek(8 + 8, SeekOrigin.Current);
                    Structure.FsObjectType type = (Structure.FsObjectType)reader.ReadByte();
                    int parentId = reader.ReadInt32();

                    if (type == Structure.FsObjectType.Directory && parentId == CurrentFolderID && name == dirName)
                    {
                        targetSlotIndex = i;
                        break;
                    }
                }

                if (targetSlotIndex == -1)
                    throw new Exception($"Directory '{dirName}' not found in current location.");

                // Step 2: Recursively delete this directory and everything inside it
                int deletedCount = DeleteHelper.DeleteRecursively(targetSlotIndex, stream, reader, writer, _MetadataEntrySize);

                // Update the global file/dir counter
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(currentCount - deletedCount);
            }
        }
    }
}
