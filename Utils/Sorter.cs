using FileManagment.FileSystem.Structure;
using System;

namespace FileManagment.Utils
{
    public class Sorter
    {
        public void HuffmanSort(int count, HuffmanNode[] nodes)
        {
            for (int i = 0; i < count - 1; i++)
                for (int j = 0; j < count - i - 1; j++)
                    if (nodes[j].Frequency > nodes[j + 1].Frequency)
                    {
                        var temp = nodes[j];
                        nodes[j] = nodes[j + 1];
                        nodes[j + 1] = temp;
                    }
        }
    }
}
