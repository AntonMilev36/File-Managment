using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagment.FileSystem.Structure;

namespace FileManagment.FileSystem.Managers
{
    public class DirectoryManager : BaseManager
    {
        public DirectoryManager(
            string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset) 
            : base(containerPath, folderId, MetadataEntrySize, DataStartOffset)
        {
        }
        public void MakeDirectory(string dirName)
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();
                if (count >= Constants.MaxFiles)
                    throw new Exception("Container is full.");

                foreach (var record in ListCurrentDirectory())
                {
                    if (record.Name == dirName)
                        throw new Exception("Directory with this name already exists in this directory.");
                }

                int slotIndex = FindFreeSlot(reader, stream);

                // write Directory Metadata
                stream.Seek(Constants.MetadataStart + slotIndex * _MetadataEntrySize, SeekOrigin.Begin);

                byte[] nameBytes = new byte[Constants.MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(dirName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, Constants.MaxFileNameLength));

                writer.Write(nameBytes);
                writer.Write((long)0);
                writer.Write((long)-1);
                writer.Write((long)0);
                writer.Write((byte)Metadata.FsObjectType.Directory);
                writer.Write(CurrentFolderID);
                writer.Write((int)-1);

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
                var children = ListCurrentDirectory();
                bool found = false;

                foreach (var child in children)
                {
                    if (child.Name == dirName && child.Type == Metadata.FsObjectType.Directory)
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
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int currentCount = reader.ReadInt32();
                int targetSlotIndex = Constants.RootDirectory;

                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    stream.Seek(Constants.MetadataStart + i * _MetadataEntrySize, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                    stream.Seek(
                        Constants.SizeLength 
                        + Constants.OffsetLength 
                        + Constants.CheckSumLenght, 
                        SeekOrigin.Current
                        );
                    Metadata.FsObjectType type = (Metadata.FsObjectType)reader.ReadByte();
                    int parentId = reader.ReadInt32();

                    // Ensures only directories in the current dir will be deleted
                    if (type == Metadata.FsObjectType.Directory && parentId == CurrentFolderID && name == dirName)
                    {
                        targetSlotIndex = i;
                        break;
                    }
                }

                if (targetSlotIndex == Constants.RootDirectory)
                    throw new Exception($"Directory '{dirName}' not found in current location.");

                int deletedCount = DeleteRecursively(targetSlotIndex, _MetadataEntrySize, stream, reader, writer);

                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(currentCount - deletedCount);
            }
        }

        private int DeleteRecursively(int slotIndex, int MetadataEntrySize, Stream stream, BinaryReader reader, BinaryWriter writer)
        {
            int deletedCount = 1;

            // Mark the delted folder as free
            stream.Seek(
                Constants.MetadataStart
                + (slotIndex * MetadataEntrySize)
                + Constants.MaxFileNameLength
                + Constants.SizeLength
                + Constants.OffsetLength
                + Constants.CheckSumLenght,
                SeekOrigin.Begin
                );
            writer.Write((byte)Metadata.FsObjectType.Free);

            for (int i = 0; i < Constants.MaxFiles; i++)
            {
                stream.Seek(
                    Constants.MetadataStart 
                    + (i * MetadataEntrySize)
                    + Constants.MaxFileNameLength
                    + Constants.SizeLength
                    + Constants.OffsetLength
                    + Constants.CheckSumLenght,
                    SeekOrigin.Begin
                    );

                Metadata.FsObjectType type = (Metadata.FsObjectType)reader.ReadByte();
                int parentId = reader.ReadInt32();

                if (type != Metadata.FsObjectType.Free && parentId == slotIndex)
                {
                    if (type == Metadata.FsObjectType.Directory)
                    {
                        deletedCount += DeleteRecursively(i, MetadataEntrySize, stream, reader, writer);
                    }
                    else
                    {
                        int slotToDelete = i;
                        while (slotToDelete != -1)
                        {
                            stream.Seek(Constants.MetadataStart + (slotToDelete * MetadataEntrySize) + (MetadataEntrySize - 4), SeekOrigin.Begin);
                            int next = reader.ReadInt32();

                            stream.Seek(Constants.MetadataStart + (slotToDelete * MetadataEntrySize) 
                                + Constants.MaxFileNameLength 
                                + Constants.SizeLength 
                                + Constants.OffsetLength 
                                + Constants.CheckSumLenght, 
                                SeekOrigin.Begin);

                            writer.Write((byte)Metadata.FsObjectType.Free);

                            deletedCount++;

                            if (next == slotToDelete) break;
                            slotToDelete = next;
                        }
                    }
                }
            }

            return deletedCount;
        }
    }
}
