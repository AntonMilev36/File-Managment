using System.Text;
using FileManagment.FileSystem.Structure;

namespace FileManagment.FileSystem.Managers
{
    public abstract class BaseManager
    {
        protected string _containerPath;
        protected int _MetadataEntrySize;
        protected int _DataStartOffset;
        public int CurrentFolderID;

        protected BaseManager(string containerPath, int folderId, int MetadataEntrySize, int DataStartOffset)
        {
            _containerPath = containerPath;
            CurrentFolderID = folderId;
            _MetadataEntrySize = MetadataEntrySize;
            _DataStartOffset = DataStartOffset;
        }

        protected int FindFreeSlot(BinaryReader reader, Stream stream)
        {
            for (int i = 0; i < Constants.MaxFiles; i++)
            {
                stream.Seek(
                    Constants.MetadataStart 
                    + (i * _MetadataEntrySize) 
                    + Constants.MaxFileNameLength 
                    + Constants.SizeLength 
                    + Constants.OffsetLength, SeekOrigin.Begin);

                if ((Metadata.FsObjectType)reader.ReadByte() == Metadata.FsObjectType.Free)
                {
                    return i;
                }
            }
            throw new Exception($"Inconsistency: Count < {Constants.MaxFiles} but no free slot found.");
        }

        protected Metadata.MetadataRecord GetRecordById(int id)
        {
            if (id == Constants.RootDirectory)
                throw new Exception("Root has no metadata record.");

            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                // Seek directly to the slot
                stream.Seek(Constants.MetadataStart + id * _MetadataEntrySize, SeekOrigin.Begin);

                // Read the data (Same logic as your ls loop)
                byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                long size = reader.ReadInt64();
                long offset = reader.ReadInt64();
                Metadata.FsObjectType type = (Metadata.FsObjectType)reader.ReadByte();
                int parentId = reader.ReadInt32();

                return new Metadata.MetadataRecord
                {
                    Id = id,
                    Name = name,
                    Size = size,
                    Offset = offset,
                    Type = type,
                    ParentId = parentId
                };
            }
        }

        public IEnumerable<Metadata.MetadataRecord> ListCurrentDirectory()
        {
            using (var stream = new FileStream(_containerPath, FileMode.Open, FileAccess.Read))
            using (var reader = new BinaryReader(stream))
            {
                if (stream.Length < 4) yield break;
                int count = reader.ReadInt32();
                int found = 0;

                for (int i = 0; i < Constants.MaxFiles && found < count; i++)
                {
                    stream.Seek(Constants.MetadataStart + i * _MetadataEntrySize, SeekOrigin.Begin);
                    byte[] nameBytes = reader.ReadBytes(Constants.MaxFileNameLength);
                    string name = Encoding.UTF8.GetString(nameBytes).TrimEnd('\0');
                    long size = reader.ReadInt64();
                    long offset = reader.ReadInt64();
                    Metadata.FsObjectType type = (Metadata.FsObjectType)reader.ReadByte();
                    int parentId = reader.ReadInt32();

                    if (type != Metadata.FsObjectType.Free)
                    {
                        found++;
                        if (parentId == CurrentFolderID)
                        {
                            yield return new Metadata.MetadataRecord
                            {
                                Id = i,
                                Name = name,
                                Size = size,
                                Offset = offset,
                                Type = type,
                                ParentId = parentId
                            };
                        }
                    }
                }
            }
        }
    }
}