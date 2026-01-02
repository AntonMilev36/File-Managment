using FileManagment.Utils;
using FileManagment.FileSystem.Structure;

namespace FileManagment.FileSystem.Managers
{
    public class HuffmanHelper
    {
        public HuffmanNode? BuildTree(long[] frequencies)
        {
            // Check how many different bytes are in the file
            int count = 0;
            for (int i = 0; i < 256; i++) 
                if (frequencies[i] > 0) count++;

            if (count == 0) return null;

            HuffmanNode[] nodes = new HuffmanNode[count];
            int idx = 0;
            for (int i = 0; i < 256; i++)
                if (frequencies[i] > 0)
                    nodes[idx++] = new HuffmanNode { Symbol = (byte)i, Frequency = frequencies[i] };

            while (count > 1)
            {
                Sorter sorter = new Sorter();
                sorter.HuffmanSort(count, nodes);

                var parentNode = new HuffmanNode
                {
                    Frequency = nodes[0].Frequency + nodes[1].Frequency,
                    Left = nodes[0],
                    Right = nodes[1]
                };
                nodes[0] = parentNode;

                // Restructuring the arrey
                for (int i = 1; i < count - 1; i++) 
                    nodes[i] = nodes[i + 1];
                count--;
            }
            return nodes[0];
        }

        public void GenerateCodes(HuffmanNode? node, string code, string[] table)
        {
            if (node == null) 
                return;

            if (node.IsLeaf) 
                table[node.Symbol] = code;

            GenerateCodes(node.Left, code + "0", table);
            GenerateCodes(node.Right, code + "1", table);
        }
    }
}