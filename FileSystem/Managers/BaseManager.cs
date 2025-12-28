using System.Text;

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

                if ((Structure.FsObjectType)reader.ReadByte() == Structure.FsObjectType.Free)
                {
                    return i;
                }
            }
            throw new Exception($"Inconsistency: Count < {Constants.MaxFiles} but no free slot found.");
        }

        public IEnumerable<Structure.MetadataRecord> ListCurrentDirectory()
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
                    Structure.FsObjectType type = (Structure.FsObjectType)reader.ReadByte();
                    int parentId = reader.ReadInt32();

                    if (type != Structure.FsObjectType.Free)
                    {
                        found++;
                        if (parentId == CurrentFolderID)
                        {
                            yield return new Structure.MetadataRecord
                            {
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