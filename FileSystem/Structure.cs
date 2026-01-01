using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem
{
    public class Structure
    {
        public enum FsObjectType : byte { Free = 0, File = 1, Directory = 2 }

        public struct MetadataRecord
        {
            public int Id;
            public string Name;
            public long Size;
            public long Offset;
            public FsObjectType Type;
            public int ParentId;
        }
    }
}
