using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileManagment.FileSystem.Structure
{
    public class HuffmanNode
    {
        public byte Symbol;
        public long Frequency;
        public HuffmanNode? Left;
        public HuffmanNode? Right;
        public bool IsLeaf => Left == null && Right == null;
    }
}
