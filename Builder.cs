using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace FileManagment
{
    public enum FsObjectType : byte { File = 0, Directory = 1 }

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
        private const int DataStartOffset = 4 + (MaxFiles * MetadataEntrySize);

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

                // Reserve the metadata area by filling it with zeros
                byte[] reservedSpace = new byte[DataStartOffset - 4];
                writer.Write(reservedSpace);
            }
        }

        public void WriteFile(string sourceFilePath, string targetName)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Source file not found on disk.");

            FileInfo sourceInfo = new FileInfo(sourceFilePath);
            long fileSize = sourceInfo.Length;

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();
                if (count >= MaxFiles) throw new Exception("Container is full (Max 100 files).");

                stream.Seek(0, SeekOrigin.End);

                long dataOffset = Math.Max(stream.Position, DataStartOffset); // prevent from having content in the metadata
                stream.Seek(dataOffset, SeekOrigin.Begin);

                using (var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    // Make a write into smaler peaces
                    byte[] buffer = new byte[4096];
                    int bytesRead;
                    while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                // Write the metadata
                stream.Seek(4 + (count * MetadataEntrySize), SeekOrigin.Begin);

                byte[] nameBytes = new byte[MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(targetName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, MaxFileNameLength)); // Trunkate the name, if too long

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

        public IEnumerable<MetadataRecord> ListCurrentDirectory()
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (stream.Length < 4) yield break;
                int count = reader.ReadInt32();

                for (int i = 0; i < count; i++)
                {
                    byte[] nameBytes = reader.ReadBytes(MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                    long size = reader.ReadInt64();
                    long offset = reader.ReadInt64();
                    FsObjectType type = (FsObjectType)reader.ReadByte();

                    yield return new MetadataRecord { Name = name, Size = size, Offset = offset, Type = type };
                }
            }
        }
    }
}