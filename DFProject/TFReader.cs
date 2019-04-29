using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class TFReader
    {
        BinaryReader[] tfBinaryReader;
        int fileCounter = 0; // Current *.tf file being read
        int termCounter = 0; // Current term being read for a given docID
        bool readHead = true; // Read header before reading termid and tf if true
        int numTerms = 0; // number of terms for the given docID
        int docID = 0;
        public TFReader(string dir, string ext)
        {
            string[] tfFileNames = Directory.GetFiles(dir, "*." + ext);
            tfBinaryReader = new BinaryReader[tfFileNames.Length];
            for (int i = 0; i < tfFileNames.Length; i++)
            {
                tfBinaryReader[i] = new BinaryReader(File.OpenRead(tfFileNames[i]));
            }
        }

        public TF readNext()
        {
            TF tf = new TF();
            try
            {
                // If at beginning of file or new docid/numTerms place, read these two values
                // and reset readHead state.
                if (readHead)
                {
                    readHeader(this.tfBinaryReader[fileCounter]);
                    readHead = false;
                }
                // Read the termid and tf values and update the tf object.
                tf.termID = this.tfBinaryReader[fileCounter].ReadUInt64();
                tf.tf = this.tfBinaryReader[fileCounter].ReadInt32();
                tf.docID = this.docID;
                // Increment termcounter. If the termCounter has reached the number of terms for this docID
                // then set readHead state to True and reset termCounter
                termCounter++;
                if (termCounter == numTerms)
                {
                    readHead = true;
                    termCounter = 0;
                }
                // return the tf object
                return tf;
            }
            // If the EOF is reached, increment fileCounter to read the next *.tf file. Reset termCounter
            // If the fileCounter has reached the total number of files to read then return null indicating
            // that no more files to be read.
            catch (EndOfStreamException)
            {
                fileCounter++;
                Console.WriteLine("Reading file {0}", fileCounter);
                termCounter = 0;
                if (fileCounter == this.tfBinaryReader.Length) return null;
                else
                {
                    // else read the header, reset readHead state
                    readHeader(tfBinaryReader[fileCounter]);
                    readHead = false;
                    // Read the next termid and tf values
                    tf.termID = this.tfBinaryReader[fileCounter].ReadUInt64();
                    tf.tf = this.tfBinaryReader[fileCounter].ReadInt32();
                    // increment termcounter
                    termCounter++;
                    tf.docID = this.docID;
                    // return the tf object. The next readNext call will follow through the try phase
                    // until eof is reached.
                    return tf;
                }
            }
        }

        private void readHeader(BinaryReader reader)
        {
            this.docID = reader.ReadInt32();
            this.numTerms = reader.ReadInt32();
        }
    }
}
