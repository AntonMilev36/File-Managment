using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem
{
    internal static class Constants
    {
        internal const string ContainerFileName = "fs_container.bin";
        internal const int MaxFileNameLength = 32;
        internal const int SizeLength = 8;
        internal const int OffsetLength = 8;
        internal const int TypeLength = 1;
        internal const int ParentIdLength = 4;
        internal const int MaxFiles = 100;
        internal const int BlockSize = 512;
        internal const int MetadataStart = 4;
        internal const int RootDirectory = -1;
    }
}
