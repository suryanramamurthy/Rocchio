using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class TFIDF
    {
        // IDF, TermID dictionaries based on index starting from 1
        Dictionary<uint, ulong> indexToTermID = new Dictionary<uint, ulong>();
        Dictionary<ulong, uint> termIDToIndex = new Dictionary<ulong, uint>();
        public Dictionary<uint, double> indexToIDF = new Dictionary<uint, double>();
        
        // Bindary files containing the dictionries
        BinaryReader indexToTIDReader, TIDToIndexReader, indexToIDFReader;

        // Dictionary of VSMs with docID as key
        //Dictionary<int, VSM> tdMatrix = new Dictionary<int, VSM>();
        SortedList<int, VSM> tdMatrix = new SortedList<int, VSM>();

        ulong termIDCntwithDF1 = 0;

        TFReader tfReader;

        public TFIDF(SortedList<string, string> settings)
        {
            // Read the index to termID and termID to index dictionary files
            indexToIDFReader = new BinaryReader(File.OpenRead(settings["IDFSTOREBASENAME"] + "IndexToIDF.bin"));
            indexToTIDReader = new BinaryReader(File.OpenRead(settings["IDFSTOREBASENAME"] + "IndexToTID.bin"));
            TIDToIndexReader = new BinaryReader(File.OpenRead(settings["IDFSTOREBASENAME"] + "TIDToIndex.bin"));
            readIndxTermIDDictionaries();
            indexToIDFReader.Close();
            indexToTIDReader.Close();
            TIDToIndexReader.Close();

            // initialize a tfReader object
            this.tfReader = new TFReader(settings["TFFILEDIR"], settings["TFFILEEXT"]);
        }

        public bool contains(int docID)
        {
            if (this.tdMatrix.ContainsKey(docID)) return true;
            else return false;
        }
        public int getDocCount()
        {
            return this.tdMatrix.Count;
        }

        public int getCorpusDocCount()
        {
            int docCount = 0;
            TF tf;
            int prevDocID = 0, currDocID = 0;

            while ((tf = this.tfReader.readNext()) != null)
            {
                currDocID = tf.docID;
                if (prevDocID != currDocID)
                {
                    docCount++;
                    prevDocID = currDocID;
                }
            }
            return docCount;
        }

        public void createRawTF()
        {
            TF tf;
            while ((tf = this.tfReader.readNext()) != null)
            {
                // Since the df file has only terms with df > 1 and tf files still
                // contains those term IDs, they will not have an index in the termIDToIndex
                // dictionary. Therefore, catch those exceptions and ignore them.
                try
                {
                    // Copy the raw tf count as weights into the TDMatrix.
                    // Later based on the type of weight, the raw tf will be converted and the
                    // appropriate IDF value will be multiplied along with it.

                    // If the TD matrix does not contain the index then create a new VSM and add to
                    // the TD matrix. 
                    if (!this.tdMatrix.ContainsKey(tf.docID)) this.tdMatrix[tf.docID] = new VSM();
                    // Add the tf value to the VSM present at that index
                    this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], tf.tf);
                }
                catch (KeyNotFoundException)
                {
                    termIDCntwithDF1++;
                }
            }
        }

        public void createBinaryTF()
        {
            TF tf;
            while ((tf = this.tfReader.readNext()) != null)
            {
                // Since the df file has only terms with df > 1 and tf files still
                // contains those term IDs, they will not have an index in the termIDToIndex
                // dictionary. Therefore, catch those exceptions and ignore them.
                try
                {
                    // Copy the raw tf count as weights into the TDMatrix.
                    // Later based on the type of weight, the raw tf will be converted and the
                    // appropriate IDF value will be multiplied along with it.

                    // If the TD matrix does not contain the index then create a new VSM and add to
                    // the TD matrix. 
                    if (!this.tdMatrix.ContainsKey(tf.docID)) this.tdMatrix[tf.docID] = new VSM();
                    // Add the tf value to the VSM present at that index
                    if (tf.tf > 0) this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], 1);
                    else this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], 0);
                }
                catch (KeyNotFoundException)
                {
                    termIDCntwithDF1++;
                }
            }
        }
        public void createTDMatrix(string weightType)
        {
            if (weightType.Equals("RAW_IDF")) this.createRawTF();
            if (weightType.Equals("NOR_IDF"))
            {
                this.createRawTF();
                Console.WriteLine("Normalizing the TF values and multiplying by IDF values");
                this.normalizeTFValues();
            }
            if (weightType.Equals("BIN_IDF"))
            {
                Console.WriteLine("Finding Binary TF values and multiplying by IDF values");
                this.createBinaryTF();
            }
            // Finally multiply by IDF
            this.multiplyByIDF();
            Console.WriteLine("Finished TFIDF computation");
        }

        private void createRawTFForGivenDocList(IList<int> docList)
        {
            TF tf;

            while ((tf = this.tfReader.readNext()) != null)
            {
                // If the docID in tf matches any of the IDs in the doc List then do the following
                // else ignore the data and read next.
                if (docList.Contains(tf.docID))
                {
                    // Since the df file has only terms with df > 1 and tf files still
                    // contains those term IDs, they will not have an index in the termIDToIndex
                    // dictionary. Therefore, catch those exceptions and ignore them.
                    try
                    {
                        // Copy the raw tf count as weights into the TDMatrix.
                        // Later based on the type of weight, the raw tf will be converted and the
                        // appropriate IDF value will be multiplied along with it.

                        // If the TD matrix does not contain the index then create a new VSM and add to
                        // the TD matrix. 
                        if (!this.tdMatrix.ContainsKey(tf.docID)) this.tdMatrix[tf.docID] = new VSM();
                        // Add the tf value to the VSM present at that index
                        this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], tf.tf);
                    }
                    catch (KeyNotFoundException)
                    {
                        termIDCntwithDF1++;
                    }
                }
            }
        }

        private void createBinaryTFForGivenDocList(IList<int> docList)
        {
            TF tf;

            while ((tf = this.tfReader.readNext()) != null)
            {
                // If the docID in tf matches any of the IDs in the doc List then do the following
                // else ignore the data and read next.
                if (docList.Contains(tf.docID))
                {
                    // Since the df file has only terms with df > 1 and tf files still
                    // contains those term IDs, they will not have an index in the termIDToIndex
                    // dictionary. Therefore, catch those exceptions and ignore them.
                    try
                    {
                        // Copy the raw tf count as weights into the TDMatrix.
                        // Later based on the type of weight, the raw tf will be converted and the
                        // appropriate IDF value will be multiplied along with it.

                        // If the TD matrix does not contain the index then create a new VSM and add to
                        // the TD matrix. 
                        if (!this.tdMatrix.ContainsKey(tf.docID)) this.tdMatrix[tf.docID] = new VSM();
                        // Add the tf value to the VSM present at that index
                        if (tf.tf > 0)
                            this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], 1);
                        else this.tdMatrix[tf.docID].addWeight((int)termIDToIndex[tf.termID], 0);
                    }
                    catch (KeyNotFoundException)
                    {
                        termIDCntwithDF1++;
                    }
                }
            }
        }

        public void createTDMatrixForGivenDocList(string weightType, IList<int> docList)
        {
            if (weightType.Equals("RAW_IDF")) this.createRawTFForGivenDocList(docList);
            if (weightType.Equals("NOR_IDF"))
            {
                this.createRawTFForGivenDocList(docList);
                Console.WriteLine("Normalizing the TF values and multiplying by IDF values");
                this.normalizeTFValues();
            }
            if (weightType.Equals("BIN_IDF"))
            {
                Console.WriteLine("Finding Binary TF values and multiplying by IDF values");
                this.createBinaryTFForGivenDocList(docList);
            }
            // Finally multiply by IDF
            this.multiplyByIDF();
            Console.WriteLine("Finished TFIDF computation");
        }

        private void normalizeTFValues()
        {
            foreach (int doc in this.tdMatrix.Keys)
                this.tdMatrix[doc].normalizeTF();
        }

        private void multiplyByIDF()
        {
            foreach (int doc in this.tdMatrix.Keys)
                this.tdMatrix[doc].multiplyIDF(this.indexToIDF);
        }

        private void readIndxTermIDDictionaries()
        {
            // Read index To IDF dictonary <uint, double>
            while (indexToIDFReader.BaseStream.Position != indexToIDFReader.BaseStream.Length)
            {
                uint index = this.indexToIDFReader.ReadUInt32();
                double IDF = this.indexToIDFReader.ReadDouble();
                indexToIDF[index] = IDF;
            }

            // Read index to TID dictionary <uint, ulong>
            while (indexToTIDReader.BaseStream.Position != indexToTIDReader.BaseStream.Length)
            {
                uint index = this.indexToTIDReader.ReadUInt32();
                ulong TID = this.indexToTIDReader.ReadUInt64();
                indexToTermID[index] = TID;
            }

            // Read TID to Index dictionary <ulong, uint>
            while (TIDToIndexReader.BaseStream.Position != TIDToIndexReader.BaseStream.Length)
            {
                ulong TID = this.TIDToIndexReader.ReadUInt64();
                uint index = this.TIDToIndexReader.ReadUInt32();
                termIDToIndex[TID] = index;
            }
        }

        public VSM getVSMforDocID(int docID)
        {
            return this.tdMatrix[docID];
        }
    }
}
