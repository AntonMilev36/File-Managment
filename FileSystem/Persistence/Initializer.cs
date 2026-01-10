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

                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    writer.Write(new byte[MetadataEntrySize - 4]);
                    writer.Write((int)-1);
                }

                byte[] reserveData = new byte[Constants.BlockSize];
                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    writer.Write(reserveData);
                }
            }
        }
    }
}
