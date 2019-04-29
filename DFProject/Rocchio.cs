using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class Rocchio
    {
        // The relVSMs and nonRelVSMs list contains a aggregate list of lawyer classified VSMs
        // as relevant and non-relevant as of the end of a particular iteration. A negative value for
        // ranking in the below sorted Lists imply that the ranking has not been computed yet.
        Dictionary<VSM, float> rocchioClassifiedRelVSMRankings = new Dictionary<VSM, float>();
        Dictionary<VSM, float> rocchioClassifiedNonRelVSMRankings = new Dictionary<VSM, float>();

        List<VSM> lawyerClassifiedRelVSMs = new List<VSM>();
        List<VSM> lawyerClassifiedNonRelVSMs = new List<VSM>();

        // Current of set of VSMs that are yet available to be classified
        Dictionary<int, VSM> yetToBeClassifiedDocIDToVSMs = new Dictionary<int, VSM>();
        Dictionary<VSM, int> yetToBeClassifiedVSMsToDocIDs = new Dictionary<VSM, int>();

        // The master set of VSMs 
        Dictionary<int,VSM> masterDocIDToVSMs = new Dictionary<int, VSM>();
        Dictionary<VSM, int> masterVSMsToDocIDs = new Dictionary<VSM, int>();

        // Centroids
        VSM relCentroid, nonRelCentroid;

        // Create the new Rocchio object and create the master VSM/DocID structures for the current 
        // iteration. Once the Rocchio object goes through iterations, it is advisable not to reuse it.
        // Better reinstantiate and start subsequent simulations. Otherwise, the master lists may be 
        // corrupted and not the right data set would be used for learning purpose.

        public Rocchio(RelevancyDataSet relDataSet, TFIDF tfidf)
        {
            foreach (int key in relDataSet.relevancy.Keys)
                if (tfidf.contains(key))
                { 
                    masterDocIDToVSMs.Add(key, tfidf.getVSMforDocID(key));
                    masterVSMsToDocIDs.Add(tfidf.getVSMforDocID(key), key);
                    yetToBeClassifiedDocIDToVSMs.Add(key, tfidf.getVSMforDocID(key));
                    yetToBeClassifiedVSMsToDocIDs.Add(tfidf.getVSMforDocID(key), key);
                }
        }

        // If enought documents cant be found, empty slots will be prefilled with -1
        public int[] getDocListToBeClassified(int numDocsPerIteration)
        {
            // Calculate the number of relevant and non relevant documents to be chosen
            int numRelDocs = (int)(0.8 * numDocsPerIteration);
            int numNonRelDocs = numDocsPerIteration - numRelDocs;

            // Arrange the relVSM and nonRelVSM dictionaries in descending order

            // Get temp iterators on the rel and non rel VSMs based on their rankings in 
            // descending order
            // This is very slow. Need to change the algorithm and see
            var tempRelVSMs = this.rocchioClassifiedRelVSMRankings.OrderByDescending(x => x.Value);
            var tempNonRelVSMs = this.rocchioClassifiedNonRelVSMRankings.OrderByDescending(x => x.Value);

            int[] docListToBeClassified = new int[numRelDocs + numNonRelDocs];
            Console.WriteLine("Number of documents classified as relevant are {0}", this.rocchioClassifiedRelVSMRankings.Count);
            Console.WriteLine("Number of documents classified as non-relevant are {0}", this.rocchioClassifiedNonRelVSMRankings.Count);
            for (int k = 0; k < docListToBeClassified.Length; k++) docListToBeClassified[k] = -1;
            int i = 0, j = 0;
            while (i < numRelDocs && j < this.rocchioClassifiedRelVSMRankings.Count)
            {
                // If yet to be classified VSMs contains the VSM then add the docID, increment i and j
                // else increment only j. If j reaches the end break out of the while loop and continue
                // adding nonRel documents
                if (this.yetToBeClassifiedVSMsToDocIDs.ContainsKey(tempRelVSMs.ElementAt(j).Key))
                {
                    docListToBeClassified[i] = this.yetToBeClassifiedVSMsToDocIDs[tempRelVSMs.ElementAt(j).Key];
                    i++;
                }
                j++;
                Console.Write("Value of i is {0}, Value of j is {1} for relevant", i, j);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            Console.WriteLine();
            j = 0;
            while (i < numRelDocs + numNonRelDocs && j < this.rocchioClassifiedNonRelVSMRankings.Count)
            {
                if (this.yetToBeClassifiedVSMsToDocIDs.ContainsKey(tempNonRelVSMs.ElementAt(j).Key))
                {
                    docListToBeClassified[i] = this.yetToBeClassifiedVSMsToDocIDs[tempNonRelVSMs.ElementAt(j).Key];
                    i++;
                }
                j++;
                Console.Write("Value of i is {0}, Value of j is {1} for non-relevant", i, j);
                Console.SetCursorPosition(0, Console.CursorTop);
            }
            Console.WriteLine();
            return docListToBeClassified;
        }

        // relType can be "REL" or "NONREL"
        // Precision is calculated on the REL or NONREL set that was classified in the current iteration
        // the docList contains the documents for which relevancyFeedback is given by the lawyer.
        // It will check whether feedback matches the rocchio based classification and finally calculate
        // the precision and return

        // Returns precision as a percentage
        public float calculatePrecision(string relType)
        {
            float precision = 0;
            if (relType.Equals("REL"))
            {
                // For each lawyer classified Relevant VSM check the rocchio classification. If they
                // match, up the count. Finally, divide by the total number of lawyer classified relevant
                // documents up to this iteration
                foreach (VSM vsm in this.lawyerClassifiedRelVSMs)
                    if (this.rocchioClassifiedRelVSMRankings.ContainsKey(vsm))
                        precision++;
                precision = precision / this.lawyerClassifiedRelVSMs.Count;
            }
            else
            {
                // For each lawyer classified Non-relevant VSM check the rocchio classification. If they
                // match, up the count. Finally, divide by the total number of lawyer classified non-relevant
                // documents up to this iteration
                foreach (VSM vsm in this.lawyerClassifiedNonRelVSMs)
                    if (this.rocchioClassifiedNonRelVSMRankings.ContainsKey(vsm))
                        precision++;
                precision = precision / this.lawyerClassifiedNonRelVSMs.Count;
            }
            precision = precision * 100;
            return precision;
        }
        

        public void addLatestLawyerClassifiedDocIDs(int[] docIDList, int[] relevancy)
        {
            for (int i = 0; i < docIDList.Length; i++)
            {
                // Add the VSM for the correspoding docID to the correct VSM list
                //Console.WriteLine("Current docID to add to the Lawyer classification is {0}", docIDList[i]);
                if (relevancy[i] == 0)
                    this.lawyerClassifiedNonRelVSMs.Add(this.masterDocIDToVSMs[docIDList[i]]);
                else
                    this.lawyerClassifiedRelVSMs.Add(this.masterDocIDToVSMs[docIDList[i]]);

                // Remove the VSM from the yetToBeClassifed lists
                this.yetToBeClassifiedDocIDToVSMs.Remove(docIDList[i]);
                this.yetToBeClassifiedVSMsToDocIDs.Remove(this.masterDocIDToVSMs[docIDList[i]]);
            }
        }

        public void calculateCentroids(int[] termList)
        {
            this.relCentroid = VSM.findCentroid(this.lawyerClassifiedRelVSMs, termList);
            this.nonRelCentroid = VSM.findCentroid(this.lawyerClassifiedNonRelVSMs, termList);
        }

        public void classify(int[] termList)
        {
            object _lock = new object();
            // Delete the existing classifications
            this.rocchioClassifiedNonRelVSMRankings = new Dictionary<VSM, float>();
            this.rocchioClassifiedRelVSMRankings = new Dictionary<VSM, float>();

            // for each VSM in the master list check whether it is similar to relCentroid or nonRelCentroid
            // and add the VSM and its ranking accordingly
            int i = 0;
            //foreach (VSM vsm in this.masterVSMsToDocIDs.Keys)
            Parallel.ForEach(this.masterVSMsToDocIDs.Keys, vsm => 
            {
                float relSimilarity = (float)vsm.cosine(relCentroid, termList);
                float nonRelSimilarity = (float)vsm.cosine(nonRelCentroid, termList);

                // If it is more relevant than nonRelevant, add the VSM and relSimilarity to the relevant
                // list
                lock (_lock)
                {
                    if (relSimilarity > nonRelSimilarity) this.rocchioClassifiedRelVSMRankings.Add(vsm, relSimilarity);
                    else this.rocchioClassifiedNonRelVSMRankings.Add(vsm, nonRelSimilarity);
                }
                Console.Write("Finsished classifying {0}th VSM", i);
                Console.SetCursorPosition(0, Console.CursorTop);
                i++;
            });
            Console.WriteLine();
        }

        public int[] getRandomDocList(int numDocsPerIteration)
        {
            List<int> docList = new List<int>();
            Random rand = new Random();
            int[] docIDList = this.masterDocIDToVSMs.Keys.ToArray<int>();
            int count = docIDList.Length + 1;
            for (int i = count - numDocsPerIteration; i < count; i++)
            {
                int item = rand.Next(i + 1);
                if (docList.Contains(docIDList[item])) docList.Add(docIDList[i]);
                else docList.Add(docIDList[item]);
            }
            return docList.ToArray();
        }

        // If numOfDocIDs = -1, return all docIDs. Otherwise, other send top 'n' ranked docIDs /
        // based on rocchio classification
        public int[] getRocchioClassifedDocIDs(string type, int numOfDocIDs)
        {
            List<int> docIDs = new List<int>();
            if (numOfDocIDs < 0)
            {
                if (type.Equals("REL"))
                    foreach (VSM key in this.rocchioClassifiedRelVSMRankings.Keys)
                        docIDs.Add(this.masterVSMsToDocIDs[key]);
                if (type.Equals("NONREL"))
                    foreach (VSM key in this.rocchioClassifiedNonRelVSMRankings.Keys)
                        docIDs.Add(this.masterVSMsToDocIDs[key]);
            }
            else
            {
                if (rocchioClassifiedRelVSMRankings.Count > 0 && rocchioClassifiedNonRelVSMRankings.Count > 0)
                {
                    // Get temp iterators on the rel and non rel VSMs based on their rankings in 
                    // descending order
                    // This is very slow. Need to change the algorithm and see

                    List<float> rankingsList = new List<float>();

                    var tempRelVSMs = this.rocchioClassifiedRelVSMRankings.OrderByDescending(x => x.Value);
                    int relVSMCount = this.rocchioClassifiedRelVSMRankings.Count;
                    var tempNonRelVSMs = this.rocchioClassifiedNonRelVSMRankings.OrderByDescending(x => x.Value);
                    int nonRelVSMCount = this.rocchioClassifiedNonRelVSMRankings.Count;
                    if (type.Equals("REL"))
                        for (int i = 0; i < Math.Min(numOfDocIDs,relVSMCount); i++) {
                            docIDs.Add(this.masterVSMsToDocIDs[tempRelVSMs.ElementAt(i).Key]);
                            rankingsList.Add(tempRelVSMs.ElementAt(i).Value);
                        }
                    if (type.Equals("NONREL"))
                        for (int i = 0; i < Math.Min(numOfDocIDs, nonRelVSMCount); i++) {
                            docIDs.Add(this.masterVSMsToDocIDs[tempNonRelVSMs.ElementAt(i).Key]);
                            rankingsList.Add(tempNonRelVSMs.ElementAt(i).Value);
                        }

                    // Create min, max, average and variance on the rankings for the selected list
                    float[] rankingArray = rankingsList.ToArray<float>();
                    float min = rankingArray.Min();
                    float max = rankingArray.Max();
                    float avg = rankingArray.Average();
                    float stdDeviation = this.standardDeviation(rankingArray);
                    Console.WriteLine("Min {0} Max {1} Avg {2} StdDeviation {3}", min, max, avg, stdDeviation);
                }
            }
            return docIDs.ToArray<int>();
        }

        private float standardDeviation(float[] source)
        {
            int n = 0;
            float mean = 0;
            float M2 = 0;

            foreach (float x in source)
            {
                n = n + 1;
                float delta = x - mean;
                mean = mean + delta / n;
                M2 += delta * (x - mean);
            }
            return (float)Math.Sqrt(M2 / (n - 1));
        }
    }
}
