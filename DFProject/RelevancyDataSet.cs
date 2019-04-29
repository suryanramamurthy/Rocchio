using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class RelevancyDataSet
    {
        public SortedList<int, int> relevancy = new SortedList<int, int>();
        public List<int> relDocList = new List<int>();
        public List<int> nonRelDocList = new List<int>();
        public RelevancyDataSet(string csvFile)
        {
            StreamReader relevancyFile = new StreamReader(File.OpenRead(csvFile));
            string line;
            while((line = relevancyFile.ReadLine()) != null)
            {
                string[] tokens = line.Split(',');
                relevancy[int.Parse(tokens[0].Trim())] = int.Parse(tokens[1].Trim());
            }
            foreach (int key in relevancy.Keys)
                if (relevancy[key] == 1) relDocList.Add(key);
                else nonRelDocList.Add(key);
        }

        public IList<int> getDocIDs()
        {
            return this.relevancy.Keys;
        }

        public Boolean isRelevant(int docID)
        {
            if (this.relevancy[docID] == 1) return true;
            else return false;
        }

        public int[] getRelevancyFeedback(int[] docArray)
        {
            int[] relevancyArray = new int[docArray.Length];
            for (int i = 0; i < relevancyArray.Length; i++)
                relevancyArray[i] = this.relevancy[docArray[i]];

            return relevancyArray;
        }

        public float recallPercentage(string type, int[] rocchioClassifedDocID, bool topN)
        {
            float recall = 0;

            if (type.Equals("REL"))
            {
                for (int i = 0; i < rocchioClassifedDocID.Length; i++)
                    if (this.relevancy.ContainsKey(rocchioClassifedDocID[i]))
                        if (this.relevancy[rocchioClassifedDocID[i]] == 1) recall++;
                if (topN) recall = recall / rocchioClassifedDocID.Length;
                else recall = recall / this.relDocList.Count;
            }
            if (type.Equals("NONREL"))
            {
                for (int i = 0; i < rocchioClassifedDocID.Length; i++)
                    if (this.relevancy.ContainsKey(rocchioClassifedDocID[i]))
                        if (this.relevancy[rocchioClassifedDocID[i]] == 0) recall++;
                if (topN) recall = recall / rocchioClassifedDocID.Length;
                else recall = recall / this.nonRelDocList.Count;
            }
            return recall * 100;
        }
    }
}
