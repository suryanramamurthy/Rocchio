using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class DFReader
    {
        private BinaryReader dfReader;
        public DFReader(string fileName)
        {
            dfReader = new BinaryReader(File.OpenRead(fileName));
        }  

        public DF readNext()
        {
            DF df = new DF();
            try
            {
                df.termID = this.dfReader.ReadUInt64();
                df.df = this.dfReader.ReadInt32();
                df.totalTF = this.dfReader.ReadInt32();
                return df;
            } catch (EndOfStreamException)
            {
                return null;
            }
        }
    }
}
