using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem.Managers
{
    public class FileManager : BaseManager
    {
        public FileManager(string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset) : base(containerPath, folderId, MetadataEntrySize, DataStartOffset)
        {
        }
        public void WriteFile(string sourceFilePath, string targetName)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Source file not found on disk.");

            FileInfo sourceInfo = new FileInfo(sourceFilePath);
            long fileSize = sourceInfo.Length;
            if (fileSize > Constants.BlockSize)
                throw new Exception($"File too large. Maximum size is {Constants.BlockSize} bytes.");

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();
                if (count >= Constants.MaxFiles) throw new Exception($"Container is full (Max {Constants.MaxFiles} files).");

                int slotIndex = FindFreeSlot(reader, stream);

                // Write file content to its reserved block
                long dataOffset = _DataStartOffset + slotIndex * Constants.BlockSize;
                stream.Seek(dataOffset, SeekOrigin.Begin);
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[Constants.BlockSize];
                    int bytesRead = sourceStream.Read(buffer, 0, Constants.BlockSize);
                    writer.Write(buffer, 0, bytesRead);
                }

                // Write the metadata
                stream.Seek(Constants.MetadataStart + slotIndex * _MetadataEntrySize, SeekOrigin.Begin);
                byte[] nameBytes = new byte[Constants.MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(targetName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, Constants.MaxFileNameLength));

                writer.Write(nameBytes);
                writer.Write(fileSize);
                writer.Write(dataOffset);
                writer.Write((byte)Structure.FsObjectType.File);
                writer.Write(CurrentFolderID);

                // Updating the number of writes
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(count + 1);
            }
        }

        public void ReadFile(string sourceName, string destinationPath)
        {
            Structure.MetadataRecord? target = null;
            foreach (var record in ListCurrentDirectory())
            {
                if (record.Name == sourceName)
                {
                    target = record;
                    break;
                }
            }

            if (target == null)
                throw new Exception($"File '{sourceName}' not found in the container.");

            using (var containerStream = new FileStream(_containerPath, FileMode.Open, FileAccess.Read))
            using (var destinationStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write))
            {
                containerStream.Seek(target.Value.Offset, SeekOrigin.Begin);

                byte[] buffer = new byte[4096];
                long bytesRemaining = target.Value.Size;

                while (bytesRemaining > 0)
                {
                    int toRead = (int)Math.Min(buffer.Length, bytesRemaining);
                    int read = containerStream.Read(buffer, 0, toRead);
                    if (read == 0) break;

                    destinationStream.Write(buffer, 0, read);
                    bytesRemaining -= read;
                }
            }
        }

        public void RemoveFile(string fileName)
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int currentCount = reader.ReadInt32();
                bool found = false;

                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    stream.Seek(Constants.MetadataStart + i * _MetadataEntrySize, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                    if (name == fileName)
                    {
                        // Check if the slot is already free
                        long statusPos = Constants.MetadataStart + i * _MetadataEntrySize + Constants.MaxFileNameLength + 8 + 8;
                        stream.Seek(statusPos, SeekOrigin.Begin);
                        if ((Structure.FsObjectType)reader.ReadByte() == Structure.FsObjectType.Free)
                            continue;

                        stream.Seek(statusPos, SeekOrigin.Begin);
                        writer.Write((byte)Structure.FsObjectType.Free);

                        stream.Seek(0, SeekOrigin.Begin);
                        writer.Write(currentCount - 1);

                        found = true;
                        break;
                    }
                }

                if (!found)
                    throw new Exception($"File '{fileName}' not found in container.");
            }
        }
    }
}
