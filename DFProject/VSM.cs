using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RocchioProject
{
    class VSM
    {
        // The key to the weights is an index starting from 1. The index is mapped
        // in another dictionary to a termID. That mapping can be used to find termID
        // for the given index in the VSM
        SortedList<int, float> weights;

        public VSM ()
        {
            this.weights = new SortedList<int, float>();
        }

        public void addWeight(int index, float weight)
        {
            this.weights[index] = weight;
        }

        public void normalizeTF()
        { 
            float sumWeights = 0;
            // Sum all the weights in the VSM
            foreach (int key in this.weights.Keys)
                sumWeights += this.weights[key];
            // Normalize each weight by dividing by the sum of the weights
            IList<int> keyList = this.weights.Keys;
            SortedList<int, float> newWeights = new SortedList<int, float>();
            foreach (int key in keyList)
                newWeights[key] = this.weights[key] / sumWeights;
            this.weights = newWeights;
        }

        public void multiplyIDF(Dictionary<uint, double> indexToIDF)
        {
            IList<int> keyList = this.weights.Keys;
            SortedList<int, float> newWeights = new SortedList<int, float>();
            foreach (int key in keyList)
                newWeights[key] = this.weights[key] * (float)indexToIDF[(uint)key];
            this.weights = newWeights;
        }

        public double cosine(VSM vsm)
        {
            double cosineSimilarity = 0;
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        // termList is a list of integers from the termIDIndex (ranges from 1 to number of terms)
        public double cosine(VSM vsm, List<int> termList)
        {
            double cosineSimilarity = 0;
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms if the termID is part of the docList given
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (termList.Contains(key) && vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                if (termList.Contains(key))
                    magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                if (termList.Contains(key))
                    magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        public double cosine(VSM vsm, int[] termList)
        {
            double cosineSimilarity = 0;
            //Array.Sort<int>(docList);
            // Calculate cosine similarity between this and the given vsm object

            // Calculate dot product of the two vsms if the termID is part of the docList given
            double dotProduct = 0;
            foreach (int key in this.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0 && vsm.weights.ContainsKey(key))
                    dotProduct += this.weights[key] * vsm.weights[key];

            // Calculate magnitudes of the two vsms
            double magOfThis = 0, magOfVsm = 0;
            foreach (int key in this.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0)
                    magOfThis += this.weights[key] * this.weights[key];
            magOfThis = Math.Sqrt(magOfThis);

            foreach (int key in vsm.weights.Keys)
                if (Array.BinarySearch<int>(termList, key) >= 0)
                    magOfVsm += vsm.weights[key] * vsm.weights[key];
            magOfVsm = Math.Sqrt(magOfVsm);

            cosineSimilarity = dotProduct / (magOfThis * magOfVsm);

            return cosineSimilarity;
        }

        // Finds the centroid of a list of VSMs based on the termID index given in the docList. All
        // other terms are ignored
        public static VSM findCentroid(List<VSM> listOfVSMs, List<int> termList)
        {
            VSM centroid = new VSM();

            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged

            // Along with the above condition, add the key and find avg iff the key is part
            // of the docList
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (termList.Contains(key))
                        if (!centroid.weights.ContainsKey(key))
                            centroid.weights[key] = vsm.weights[key];
                        else
                            centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key] / listOfVSMs.Count;
            return newCentroid;
        }

        public static VSM findCentroid(List<VSM> listOfVSMs, int[] termList)
        {
            VSM centroid = new VSM();
            
            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged

            // Along with the above condition, add the key and find avg iff the key is part
            // of the docList
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (Array.BinarySearch<int>(termList, key) >= 0)
                        if (!centroid.weights.ContainsKey(key))
                            centroid.weights[key] = vsm.weights[key];
                        else
                            centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key] / listOfVSMs.Count;
            return newCentroid;
        }
        public static VSM findCentroid(List<VSM> listOfVSMs)
        {
            VSM centroid = new VSM();
            
            // for each VSM in the list, iterate on the key. If the key is not present in 
            // the centroid, add the key, value to centroid. If the key is already present
            // then add the value to the existing value in the centroid. Finally, iterate
            // over all values in the centroid and divide by the number of VSMs that are
            // being averaged
            foreach (VSM vsm in listOfVSMs)
                foreach (int key in vsm.weights.Keys)
                    // If the centroid doesn't contain the key, then add the key, value
                    if (!centroid.weights.ContainsKey(key))
                        centroid.weights[key] = vsm.weights[key];
                    else
                        centroid.weights[key] += vsm.weights[key];

            VSM newCentroid = new VSM();
            foreach (int key in centroid.weights.Keys)
                newCentroid.weights[key] = centroid.weights[key]/listOfVSMs.Count;
            return newCentroid;
        }
    }
}
