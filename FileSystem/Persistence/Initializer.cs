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
                // 1. Write the initial file count (0)
                writer.Write(0);

                // 2. Initialize all Metadata slots correctly (at the start)
                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    // Write Name (32 bytes), Size(8), Offset(8), CheckSum(8), Type(1), Parent(4)
                    writer.Write(new byte[MetadataEntrySize - 4]);
                    // Write NextSlotId as -1 (4 bytes)
                    writer.Write((int)-1);
                }

                // 3. Reserve the Data area (initialize with 0s)
                byte[] reserveData = new byte[Constants.BlockSize];
                for (int i = 0; i < Constants.MaxFiles; i++)
                {
                    writer.Write(reserveData);
                }
            }
        }
    }
}
