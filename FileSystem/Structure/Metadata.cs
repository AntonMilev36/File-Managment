using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem.Structure
{
    public class Metadata
    {
        public enum FsObjectType : byte { Free = 0, File = 1, Directory = 2, FilePart = 3 }

        public struct MetadataRecord
        {
            public int Id;
            public string Name;
            public long Size;
            public long Offset;
            public long CheckSum;
            public FsObjectType Type;
            public int ParentId;
            public int NextSlotId;
        }
    }
}
