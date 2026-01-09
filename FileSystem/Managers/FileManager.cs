using System;
using System.IO;
using System.Text;
using FileManagment.FileSystem.Structure;
using FileManagment.Utils;

namespace FileManagment.FileSystem.Managers
{
    public class FileManager : BaseManager
    {
        public FileManager(string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset)
            : base(containerPath, folderId, MetadataEntrySize, DataStartOffset) { }

        public void WriteFile(string sourceFilePath, string targetName)
        {
            if (!File.Exists(sourceFilePath)) throw new FileNotFoundException("Source file not found.");

            foreach (var record in ListCurrentDirectory())
                if (record.Name == targetName) throw new Exception("File already exists.");

            long[] freqs = new long[256];
            long originalSize = 0;
            long checkSum = 0;
            byte[] buffer = new byte[Constants.BufferSize];

            using (FileStream fs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
            {
                originalSize = fs.Length;
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, Constants.BufferSize)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        freqs[buffer[i]]++;
                        checkSum = Hash.CalculateChecksumIncremental(checkSum, buffer[i]);
                    }
                }
            }

            HuffmanHelper helper = new HuffmanHelper();
            var root = helper.BuildTree(freqs);
            string[] codeTable = new string[256];
            helper.GenerateCodes(root, "", codeTable);

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                stream.Seek(0, SeekOrigin.Begin);
                int globalCount = reader.ReadInt32();
                int headSlotIndex = FindFreeSlot(reader, stream);

                // --- FIX: MARK HEAD AS OCCUPIED IMMEDIATELY ---
                // We skip Name (32), Size (8), Offset (8), CheckSum (8) to reach Type (1)
                stream.Seek(Constants.MetadataStart + (headSlotIndex * _MetadataEntrySize) + 56, SeekOrigin.Begin);
                writer.Write((byte)Metadata.FsObjectType.File);
                writer.Flush(); // Force write to disk so FindFreeSlot won't pick it again

                int currentSlot = headSlotIndex;
                int slotsUsed = 1;
                int? firstNextSlot = null;

                long currentBlockEnd = _DataStartOffset + (long)currentSlot * Constants.BlockSize + Constants.BlockSize;
                stream.Seek(_DataStartOffset + (long)currentSlot * Constants.BlockSize, SeekOrigin.Begin);

                Action<byte> writeByteWithJump = (b) =>
                {
                    if (stream.Position >= currentBlockEnd)
                    {
                        int nextSlot = FindFreeSlot(reader, stream);
                        if (firstNextSlot == null) firstNextSlot = nextSlot;
                        slotsUsed++;

                        // Link current block to the next one
                        long nextSlotPos = Constants.MetadataStart + (currentSlot * _MetadataEntrySize) + (_MetadataEntrySize - 4);
                        stream.Seek(nextSlotPos, SeekOrigin.Begin);
                        writer.Write(nextSlot);

                        // Initialize the new FilePart metadata
                        stream.Seek(Constants.MetadataStart + nextSlot * _MetadataEntrySize, SeekOrigin.Begin);
                        writer.Write(new byte[Constants.MaxFileNameLength]);
                        writer.Write((long)0);
                        writer.Write(_DataStartOffset + (long)nextSlot * Constants.BlockSize);
                        writer.Write((long)0);
                        writer.Write((byte)Metadata.FsObjectType.FilePart);
                        writer.Write(CurrentFolderID);
                        writer.Write((int)-1);
                        writer.Flush(); // Ensure this slot is now marked as "FilePart"

                        currentSlot = nextSlot;
                        stream.Seek(_DataStartOffset + (long)currentSlot * Constants.BlockSize, SeekOrigin.Begin);
                        currentBlockEnd = stream.Position + Constants.BlockSize;
                    }
                    writer.Write(b);
                };

                for (int i = 0; i < 256; i++)
                {
                    byte[] fBytes = BitConverter.GetBytes(freqs[i]);
                    foreach (byte fb in fBytes) writeByteWithJump(fb);
                }

                byte currentByte = 0;
                int bits = 0;
                using (FileStream fs = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read))
                {
                    int bRead;
                    while ((bRead = fs.Read(buffer, 0, Constants.BufferSize)) > 0)
                    {
                        for (int i = 0; i < bRead; i++)
                        {
                            foreach (char bit in codeTable[buffer[i]])
                            {
                                currentByte = (byte)((currentByte << 1) | (bit == '1' ? 1 : 0));
                                if (++bits == 8)
                                {
                                    writeByteWithJump(currentByte);
                                    currentByte = 0; bits = 0;
                                }
                            }
                        }
                    }
                }
                if (bits > 0) writeByteWithJump((byte)(currentByte << (8 - bits)));

                // Finalize the head metadata
                stream.Seek(Constants.MetadataStart + headSlotIndex * _MetadataEntrySize, SeekOrigin.Begin);
                byte[] nameBytes = new byte[Constants.MaxFileNameLength];
                Encoding.UTF8.GetBytes(targetName).CopyTo(nameBytes, 0);
                writer.Write(nameBytes);
                writer.Write(originalSize);
                writer.Write(_DataStartOffset + (long)headSlotIndex * Constants.BlockSize);
                writer.Write(checkSum);
                writer.Write((byte)Metadata.FsObjectType.File);
                writer.Write(CurrentFolderID);
                writer.Write(firstNextSlot ?? -1);

                stream.Seek(0, SeekOrigin.Begin);
                writer.Write(globalCount + slotsUsed);
            }
        }

        public void ReadFile(string sourceName, string destinationPath)
        {
            Metadata.MetadataRecord? head = null;
            foreach (var r in ListCurrentDirectory()) if (r.Name == sourceName) { head = r; break; }
            if (head == null) throw new Exception("File not found.");

            using (var container = new FileStream(_containerPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = new BinaryReader(container))
            using (var output = new FileStream(destinationPath, FileMode.Create))
            {
                Metadata.MetadataRecord currentTarget = head.Value;
                long currentBlockEnd = currentTarget.Offset + Constants.BlockSize;
                container.Seek(currentTarget.Offset, SeekOrigin.Begin);

                Func<byte> readByteWithJump = () =>
                {
                    if (container.Position >= currentBlockEnd)
                    {
                        if (currentTarget.NextSlotId == -1) throw new Exception("Chain ended prematurely.");
                        currentTarget = GetRecordById(currentTarget.NextSlotId);
                        container.Seek(currentTarget.Offset, SeekOrigin.Begin);
                        currentBlockEnd = container.Position + Constants.BlockSize;
                    }
                    return reader.ReadByte();
                };

                long[] freqs = new long[256];
                for (int i = 0; i < 256; i++)
                {
                    byte[] fBytes = new byte[8];
                    for (int j = 0; j < 8; j++) fBytes[j] = readByteWithJump();
                    freqs[i] = BitConverter.ToInt64(fBytes, 0);
                }

                HuffmanHelper helper = new HuffmanHelper();
                var root = helper.BuildTree(freqs);
                var node = root;
                long decodedCount = 0;
                long checkSum = 0;

                while (decodedCount < head.Value.Size)
                {
                    byte b = readByteWithJump();
                    for (int i = 7; i >= 0 && decodedCount < head.Value.Size; i--)
                    {
                        node = ((b >> i) & 1) == 0 ? node.Left : node.Right;
                        if (node.IsLeaf)
                        {
                            output.WriteByte(node.Symbol);
                            checkSum = Hash.CalculateChecksumIncremental(checkSum, node.Symbol);
                            decodedCount++;
                            node = root;
                        }
                    }
                }

                if (checkSum != head.Value.CheckSum)
                    throw new Exception($"Checksum mismatch! Expected {head.Value.CheckSum}, got {checkSum}");
            }
        }

        public void RemoveFile(string fileName)
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
            using (var reader = new BinaryReader(stream))
            using (var writer = new BinaryWriter(stream))
            {
                bool found = false;
                int totalSlotsFreed = 0;

                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    long slotStart = Constants.MetadataStart + (long)i * _MetadataEntrySize;

                    stream.Seek(Constants.MetadataStart + i * _MetadataEntrySize, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');

                    stream.Seek(slotStart 
                        + Constants.MaxFileNameLength 
                        + Constants.SizeLength 
                        + Constants.OffsetLength 
                        + Constants.CheckSumLenght, 
                        SeekOrigin.Begin);

                    byte type = reader.ReadByte();
                    int parentId = reader.ReadInt32();

                    if (name == fileName && type == (byte)Metadata.FsObjectType.File && parentId == CurrentFolderID)
                    {
                        found = true;
                        int slotToDelete = i;
                        int safetyCounter = 0;

                        while (slotToDelete != -1 && safetyCounter < Constants.MaxFiles)
                        {
                            stream.Seek(Constants.MetadataStart + (slotToDelete * _MetadataEntrySize) + (_MetadataEntrySize - 4), SeekOrigin.Begin);
                            int next = reader.ReadInt32();

                            stream.Seek(Constants.MetadataStart 
                                + (slotToDelete * _MetadataEntrySize)
                                + Constants.MaxFileNameLength
                                + Constants.SizeLength
                                + Constants.OffsetLength
                                + Constants.CheckSumLenght, 
                                SeekOrigin.Begin);

                            writer.Write((byte)Metadata.FsObjectType.Free);

                            stream.Seek(Constants.MetadataStart + (slotToDelete * _MetadataEntrySize), SeekOrigin.Begin);
                            writer.Write(new byte[Constants.MaxFileNameLength]);

                            totalSlotsFreed++;

                            if (next == slotToDelete) break;

                            slotToDelete = next;
                            safetyCounter++;
                        }
                        break;
                    }
                }

                if (found)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                    int currentCount = reader.ReadInt32();
                    stream.Seek(0, SeekOrigin.Begin);
                    writer.Write(Math.Max(0, currentCount - totalSlotsFreed));

                    stream.Flush(true);
                }
                else
                {
                    throw new Exception($"File '{fileName}' not found.");
                }
            }
        }
    }
}
