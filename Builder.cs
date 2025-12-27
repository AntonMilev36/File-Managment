using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FileManagment
{
    public enum FsObjectType : byte {Free = 0, File = 1, Directory = 2 }

    public struct MetadataRecord
    {
        public string Name;
        public long Size;
        public long Offset;
        public FsObjectType Type;
    }

    public class Builder
    {
        private string _containerPath;
        private const int MaxFileNameLength = 32;
        private const int MetadataEntrySize = MaxFileNameLength + 8 + 8 + 1; // 49 bytes

        // Reserving space for 100 metadata entries to prevent overlap with data
        private const int MaxFiles = 100;
        private const int BlockSize = 512;
        private const int MetadataStart = 4;
        private const int DataStartOffset = MetadataStart + (MaxFiles * MetadataEntrySize);

        public Builder(string containerPath)
        {
            _containerPath = containerPath;
            if (!File.Exists(_containerPath)) InitializeContainer();
        }

        private void InitializeContainer()
        {
            using (var stream = new FileStream(_containerPath, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0);

                byte[] reservedMetadata = new byte[MaxFiles * MetadataEntrySize];
                writer.Write(reservedMetadata);

                byte[] reserveData = new byte[MaxFiles * BlockSize];
                writer.Write(reserveData);
            }
        }

        public void WriteFile(string sourceFilePath, string targetName)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Source file not found on disk.");

            FileInfo sourceInfo = new FileInfo(sourceFilePath);
            long fileSize = sourceInfo.Length;
            if (fileSize > BlockSize)
                throw new Exception($"File too large. Maximum size is {BlockSize} bytes.");

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();
                if (count >= MaxFiles) throw new Exception($"Container is full (Max {MaxFiles} files).");

                // Find the first metadata slot that is marked as 'Free'
                int slotIndex = -1;
                for (int i = 0; i < MaxFiles; i++)
                {
                    stream.Seek(MetadataStart + (i * MetadataEntrySize) + MaxFileNameLength + 8 + 8, SeekOrigin.Begin);
                    if ((FsObjectType)reader.ReadByte() == FsObjectType.Free)
                    {
                        slotIndex = i;
                        break;
                    }
                }

                if (slotIndex == -1) 
                    throw new Exception($"Inconsistency: Count < {MaxFiles} but no free slot found.");

                // Write file content to its reserved block
                long dataOffset = DataStartOffset + (slotIndex * BlockSize);
                stream.Seek(dataOffset, SeekOrigin.Begin);
                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    byte[] buffer = new byte[BlockSize];
                    int bytesRead = sourceStream.Read(buffer, 0, BlockSize);
                    writer.Write(buffer, 0, bytesRead);
                }

                // Write the metadata
                stream.Seek(MetadataStart + (slotIndex * MetadataEntrySize), SeekOrigin.Begin);
                byte[] nameBytes = new byte[MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(targetName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, MaxFileNameLength));

                writer.Write(nameBytes);
                writer.Write(fileSize);
                writer.Write(dataOffset);
                writer.Write((byte)FsObjectType.File);

                // Updating the number of writes
                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(count + 1);
            }
        }

        public void ReadFile(string sourceName, string destinationPath)
        {
            MetadataRecord? target = null;
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

                for (int i = 0; i < MaxFiles; i++)
                {
                    stream.Seek(MetadataStart + (i * MetadataEntrySize), SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                    if (name == fileName)
                    {
                        // Check if the slot is already free
                        long statusPos = MetadataStart + (i * MetadataEntrySize) + MaxFileNameLength + 8 + 8;
                        stream.Seek(statusPos, SeekOrigin.Begin);
                        if ((FsObjectType)reader.ReadByte() == FsObjectType.Free) 
                            continue;

                        // 3. Mark as Free (Logical deletion)
                        stream.Seek(statusPos, SeekOrigin.Begin);
                        writer.Write((byte)FsObjectType.Free);

                        // 4. Decrement the global counter
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

        public IEnumerable<MetadataRecord> ListCurrentDirectory()
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (stream.Length < 4) yield break;
                int count = reader.ReadInt32();
                int found = 0;

                for (int i = 0; i < count && found < count; i++)
                {
                    byte[] nameBytes = reader.ReadBytes(MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                    long size = reader.ReadInt64();
                    long offset = reader.ReadInt64();
                    FsObjectType type = (FsObjectType)reader.ReadByte();

                    if (type != FsObjectType.Free)
                    {
                        found++;
                        yield return new MetadataRecord { Name = name, Size = size, Offset = offset, Type = type };
                    }
                }
            }
        }
    }
}