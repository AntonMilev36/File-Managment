using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileManagment.FileSystem.Structure;
using FileManagment.Utils;

namespace FileManagment.FileSystem.Managers
{
    public class FileManager : BaseManager
    {
        public FileManager(
            string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset) 
            : base(containerPath, folderId, MetadataEntrySize, DataStartOffset)
        {
        }
        public void WriteFile(string sourceFilePath, string targetName)
        {
            if (!File.Exists(sourceFilePath))
                throw new FileNotFoundException("Source file not found on disk.");

            byte[] originalData = File.ReadAllBytes(sourceFilePath);
            long originalSize = originalData.Length;
            long checkSum = Hash.CalculateCheckSum(originalData);

            // How often every byte is seen in the file
            long[] freqs = new long[256];
            foreach (byte b in originalData)
                freqs[b]++;

            HuffmanHelper helper = new HuffmanHelper();
            var root = helper.BuildTree(freqs);
            string[] codeTable = new string[256];
            helper.GenerateCodes(root, "", codeTable);

            List<byte> compressed = new List<byte>();
            byte currentByte = 0;
            int bits = 0;
            foreach (byte b in originalData)
            {
                foreach (char bit in codeTable[b])
                {
                    currentByte = (byte)((currentByte << 1) | (bit == '1' ? 1 : 0));
                    if (++bits == 8)
                    {
                        compressed.Add(currentByte); currentByte = 0; bits = 0;
                    }
                }
            }
            if (bits > 0)
                compressed.Add((byte)(currentByte << (8 - bits)));

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                int count = reader.ReadInt32();

                if (count >= Constants.MaxFiles)
                    throw new Exception("Container full.");

                int slotIndex = FindFreeSlot(reader, stream);
                long dataOffset = _DataStartOffset + slotIndex * Constants.BlockSize;

                stream.Seek(dataOffset, SeekOrigin.Begin);

                foreach (long f in freqs)
                    writer.Write(f);

                writer.Write(compressed.ToArray());

                stream.Flush(true);

                // Write the metadata
                stream.Seek(Constants.MetadataStart + slotIndex * _MetadataEntrySize, SeekOrigin.Begin);
                byte[] nameBytes = new byte[Constants.MaxFileNameLength];
                byte[] sourceNameBytes = Encoding.UTF8.GetBytes(targetName);
                Array.Copy(sourceNameBytes, nameBytes, Math.Min(sourceNameBytes.Length, Constants.MaxFileNameLength));

                writer.Write(nameBytes);
                writer.Write(originalSize); // Store original size for decompression
                writer.Write(dataOffset);
                writer.Write(checkSum);

                // Prevent from writing not fully created files
                long typePosition = stream.Position;
                writer.Write((byte)Metadata.FsObjectType.Free);
                writer.Write(CurrentFolderID);

                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(count + 1);

                stream.Seek(typePosition, SeekOrigin.Begin);
                writer.Write((byte)Metadata.FsObjectType.File);

                stream.Flush(true);
            }
        }

        public void ReadFile(string sourceName, string destinationPath)
        {
            Metadata.MetadataRecord? target = null;
            foreach (var record in ListCurrentDirectory().ToList())
            {
                if (record.Name == sourceName)
                {
                    target = record;
                    break;
                }
            }

            if (target == null)
                throw new Exception("File not found.");

            string tempPath = destinationPath + ".tmp";
            bool success = false;
            long currentCheckSum = 0;

            try
            {
                using (var containerStream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite))
                using (var reader = new BinaryReader(containerStream))
                using (var writer = new BinaryWriter(containerStream))
                {
                    using (var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write))
                    {
                        containerStream.Seek(target.Value.Offset, SeekOrigin.Begin);

                        // Read frequency table
                        long[] freqs = new long[256];
                        for (int i = 0; i < 256; i++)
                            freqs[i] = reader.ReadInt64();

                        // Rebuild the tree
                        HuffmanHelper helper = new HuffmanHelper();
                        var root = helper.BuildTree(freqs);
                        var current = root;

                        // Decompress bits
                        long decodedBytes = 0;

                        while (decodedBytes < target.Value.Size)
                        {
                            byte b = reader.ReadByte();
                            for (int i = 7; i >= 0 && decodedBytes < target.Value.Size; i--)
                            {
                                int bit = (b >> i) & 1;
                                current = (bit == 0) ? current.Left : current.Right;

                                if (current.IsLeaf)
                                {
                                    byte symbol = current.Symbol;

                                    tempStream.WriteByte(symbol);

                                    // Update checksum for every single byte decompressed
                                    currentCheckSum = Hash.CalculateChecksumIncremental(currentCheckSum, symbol);

                                    decodedBytes++;
                                    current = root;
                                }
                            }
                        }
                    }
                    if (currentCheckSum != target.Value.CheckSum)
                    {
                        containerStream.Seek(
                            Constants.MetadataStart
                            + (target.Value.Id * _MetadataEntrySize)
                            + Constants.MaxFileNameLength
                            + Constants.SizeLength
                            + Constants.OffsetLength
                            + Constants.CheckSumLenght,
                            SeekOrigin.Begin
                            );

                        writer.Write((byte)Metadata.FsObjectType.Free); // Delete file when corrupted 

                        containerStream.Seek(0, SeekOrigin.Begin);
                        int currentCount = reader.ReadInt32();
                        containerStream.Seek(0, SeekOrigin.Begin);
                        writer.Write(currentCount - 1);

                        throw new Exception("File corrupted (checksum mismatch).");
                    }
                    else
                        success = true;
                }

                if (success)
                    // Prevent from overwriting existing files
                    File.Move(tempPath, destinationPath);
            }
            finally
            {
                if (!success && File.Exists(tempPath))
                {
                    File.Delete(tempPath);
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
                        long statusPos = (
                            Constants.MetadataStart 
                            + (i * _MetadataEntrySize) 
                            + Constants.MaxFileNameLength 
                            + Constants.SizeLength 
                            + Constants.OffsetLength
                            + Constants.CheckSumLenght
                            );
                        stream.Seek(statusPos, SeekOrigin.Begin);
                        if ((Metadata.FsObjectType)reader.ReadByte() == Metadata.FsObjectType.Free)
                            continue;

                        stream.Seek(statusPos, SeekOrigin.Begin);
                        writer.Write((byte)Metadata.FsObjectType.Free);

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
