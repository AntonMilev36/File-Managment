using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem.Persistence
{
    public static class Initializer
    {
        public static void InitializeContainer(string path, int MetadataEntrySize)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0);

                byte[] reservedMetadata = new byte[Constants.MaxFiles * MetadataEntrySize];
                writer.Write(reservedMetadata);

                byte[] reserveData = new byte[Constants.MaxFiles * Constants.BlockSize];
                writer.Write(reserveData);
            }
        }
    }
}
