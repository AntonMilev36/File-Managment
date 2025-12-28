using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
