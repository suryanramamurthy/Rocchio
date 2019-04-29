using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    // This class implements IDF for each term by calculating ln(N/df[term])
    class IDF
    {
        Dictionary<uint, ulong> indexToTermID = new Dictionary<uint, ulong>();
        Dictionary<uint, double> indexToIDF = new Dictionary<uint, double>();
        int[] dfStats = new int[100];

        //Open the binary file in the given location and create the dictionaries
        public IDF(string fileName, int corpusDocCount)
        {
            DF df;
            uint counter = 1;

            // Open the df file for reading
            DFReader dfReader = new DFReader(fileName);

            // Read each term id entry and create three dictionaries
            // Initially the indexToIDF dictionary will contain the DF.
            // Once the loop is finished the actual IDF wll be computed and replaced in the dictionary
            while ((df = dfReader.readNext()) != null)
            {
                indexToTermID[counter] = df.termID;
                indexToIDF[counter] = df.df;
                counter++;
            }

            int N = corpusDocCount;

            for (int i = 0; i < dfStats.Length; i++) dfStats[i] = 0;
                        
            for (uint i = 1; i <= indexToIDF.Count; i++)
            {
                int offset = (int)Math.Floor(indexToIDF[i] * 100 / N);
                dfStats[offset]++;
            }

            for (uint i = 1; i <= indexToIDF.Count; i++)
            {
                // Compute using the natural log
                double idf = Math.Log(N / indexToIDF[i]);   
                indexToIDF[i] = idf;
            }
        }

        public void writeIDFToFile(string baseName)
        {
            string termIDToIndexFName = baseName + "TIDToIndex.bin";
            string indexToTermIDFName = baseName + "IndexToTID.bin";
            string indexToIDFFName = baseName + "IndexToIDF.bin";

            if (File.Exists(termIDToIndexFName)) File.Delete(termIDToIndexFName);
            if (File.Exists(indexToTermIDFName)) File.Delete(indexToTermIDFName);
            if (File.Exists(indexToIDFFName)) File.Delete(indexToIDFFName);

            BinaryWriter TIDToIndex = new BinaryWriter(File.OpenWrite(termIDToIndexFName));
            BinaryWriter IndexToTID = new BinaryWriter(File.OpenWrite(indexToTermIDFName));
            BinaryWriter IndexToIDF = new BinaryWriter(File.OpenWrite(indexToIDFFName));

            for (uint i = 1; i <= this.indexToIDF.Count; i++)
            {
                // Write *TIDToIndex.bin. ulong,uint tuples
                TIDToIndex.Write(this.indexToTermID[i]);
                TIDToIndex.Write(i);

                // Write *IndexToTID.bin values. uint, ulong tuples
                IndexToTID.Write(i);
                IndexToTID.Write(this.indexToTermID[i]);

                // Write *IndexToIDF.bin values. uint, double tuples
                IndexToIDF.Write(i);
                IndexToIDF.Write(this.indexToIDF[i]);
            }

            TIDToIndex.Flush();
            TIDToIndex.Close();
            IndexToTID.Flush();
            IndexToTID.Close();
            IndexToIDF.Flush();
            IndexToIDF.Close();
        }

        public void printStats()
        {
            for (int i = 0; i < dfStats.Length; i++)
                Console.WriteLine("{0} % = {1} count", i, dfStats[i]);
        }
    }
}
