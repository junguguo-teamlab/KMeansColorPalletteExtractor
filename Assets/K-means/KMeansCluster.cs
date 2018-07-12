using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

// K-means clustering demo. ('Lloyd's algorithm')
// Coded using static methods. Normal error-checking removed for clarity.
// This code can be used in at least two ways. You can do a copy-paste and then insert the code into some system that uses clustering.
// Or you can wrap the code up in a Class Library. The single public method is Cluster().

namespace KMeansCluster
{
    public interface IKMeansData
    {
        int Length();
        float Value(int index);
        void SetValue(int index,float value);
        float Distance(IKMeansData data);
        void SetCluster(int ClusterIndex);
        void SetClusterCount(int count);
    }

    public class KMeansCluster
    {
//        void Start()
//        {
//
//            // real data likely to come from a text file or SQL
//            double[][] rawData = new double[20][];
//            rawData[0] = new double[] { 65.0, 220.0 };
//            rawData[1] = new double[] { 73.0, 160.0 };
//            rawData[2] = new double[] { 59.0, 110.0 };
//            rawData[3] = new double[] { 61.0, 120.0 };
//            rawData[4] = new double[] { 75.0, 150.0 };
//            rawData[5] = new double[] { 67.0, 240.0 };
//            rawData[6] = new double[] { 68.0, 230.0 };
//            rawData[7] = new double[] { 70.0, 220.0 };
//            rawData[8] = new double[] { 62.0, 130.0 };
//            rawData[9] = new double[] { 66.0, 210.0 };
//            rawData[10] = new double[] { 77.0, 190.0 };
//            rawData[11] = new double[] { 75.0, 180.0 };
//            rawData[12] = new double[] { 74.0, 170.0 };
//            rawData[13] = new double[] { 70.0, 210.0 };
//            rawData[14] = new double[] { 61.0, 110.0 };
//            rawData[15] = new double[] { 58.0, 100.0 };
//            rawData[16] = new double[] { 66.0, 230.0 };
//            rawData[17] = new double[] { 59.0, 120.0 };
//            rawData[18] = new double[] { 68.0, 210.0 };
//            rawData[19] = new double[] { 61.0, 130.0 };
//
//            Debug.Log("Raw unclustered data:\n");
//            Debug.Log("    Height Weight");
//            Debug.Log("-------------------");
//           // ShowData(rawData, 1, true, true);
//
//            int numClusters = 3;
//            Debug.Log("\nSetting numClusters to " + numClusters);
//
//            //int[] clustering = Cluster(rawData, numClusters); // this is it
//
//            Debug.Log("\nK-means clustering complete\n");
//
//            Debug.Log("Final clustering in internal form:\n");
//           // ShowVector(clustering, true);
//
//            Debug.Log("Raw data by cluster:\n");
//           // ShowClustered(rawData, clustering, numClusters, 1);
//
//            Debug.Log("\nEnd k-means clustering demo\n");
//
//        } // Main

        // ============================================================================

        public static int[] Cluster(IKMeansData[] data, IKMeansData[] means, int numClusters)
        {
            // k-means clustering
            // index of return is tuple ID, cell is cluster ID
            // ex: [2 1 0 0 2 2] means tuple 0 is cluster 2, tuple 1 is cluster 1, tuple 2 is cluster 0, tuple 3 is cluster 0, etc.
            // an alternative clustering DS to save space is to use the .NET BitArray class
            
            //IKMeansData[] data = Normalized(rawData); // so large values don't dominate

            bool changed = true; // was there a change in at least one cluster assignment?
            bool success = true; // were all means able to be computed? (no zero-count clusters)

            // init clustering[] to get things started
            // an alternative is to initialize means to randomly selected tuples
            // then the processing loop is
            // loop
            //    update clustering
            //    update means
            // end loop
            int[] clustering = InitClustering(data.Length, numClusters, 0); // semi-random initialization
            //IKMeansData[] means = Allocate(numClusters, data[0].Length()); // small convenience 

            int maxCount = data.Length * 10; // sanity check
            int ct = 0;
            while (changed == true && success == true && ct < maxCount)
            {
                ++ct; // k-means typically converges very quickly
                success = UpdateMeans(data, clustering, means); // compute new cluster means if possible. no effect if fail
                changed = UpdateClustering(data, clustering, means); // (re)assign tuples to clusters. no effect if fail
            }
            // consider adding means[][] as an out parameter - the final means could be computed
            // the final means are useful in some scenarios (e.g., discretization and RBF centroids)
            // and even though you can compute final means from final clustering, in some cases it
            // makes sense to return the means (at the expense of some method signature uglinesss)
            //
            // another alternative is to return, as an out parameter, some measure of cluster goodness
            // such as the average distance between cluster means, or the average distance between tuples in 
            // a cluster, or a weighted combination of both
            return clustering;
        }

        private static IKMeansData[] Normalized(IKMeansData[] rawData)
        {
            // normalize raw data by computing (x - mean) / stddev
            // primary alternative is min-max:
            // v' = (v - min) / (max - min)

            // make a copy of input data
            IKMeansData[] result = (IKMeansData[])rawData.Clone(); //jungu: based on the result this seems like a shallow copy

//            for (int i = 0; i < rawData.Length; ++i)
//            {
//                result[i] = rawData[i].Clone();
//                //Array.Copy(rawData[i], result[i], rawData[i].Length);
//            }

            for (int j = 0; j < result[0].Length(); ++j) // each col
            {
                float colSum = 0.0f;
                for (int i = 0; i < result.Length; ++i)
                    colSum += result[i].Value(j);
                float mean = colSum / result.Length;
                float sum = 0.0f;
                for (int i = 0; i < result.Length; ++i)
                    sum += (result[i].Value(j) - mean) * (result[i].Value(j) - mean);
                float sd = sum / result.Length;
                for (int i = 0; i < result.Length; ++i)
                    result[i].SetValue(j, (result[i].Value(j) - mean) / sd);
            }
            return result;
        }

        private static int[] InitClustering(int numTuples, int numClusters, int randomSeed)
        {
            // init clustering semi-randomly (at least one tuple in each cluster)
            // consider alternatives, especially k-means++ initialization,
            // or instead of randomly assigning each tuple to a cluster, pick
            // numClusters of the tuples as initial centroids/means then use
            // those means to assign each tuple to an initial cluster.
            System.Random random = new System.Random(randomSeed);
            int[] clustering = new int[numTuples];
            for (int i = 0; i < numClusters; ++i) // make sure each cluster has at least one tuple
                clustering[i] = i;
            for (int i = numClusters; i < clustering.Length; ++i)
                clustering[i] = random.Next(0, numClusters); // other assignments random
            return clustering;
        }

        //obselte, dont use;
        private static IKMeansData[] Allocate(int numClusters, int numColumns)
        {
            // convenience matrix allocator for Cluster()
            IKMeansData[] result = new IKMeansData[numClusters];
//            for (int k = 0; k < numClusters; ++k)
//            {
//                result[k] = (IKMeansData) (new System.Object());
//                result[k].InitData(numColumns);
//            }

            return result;
        }

        private static bool UpdateMeans(IKMeansData[] data, int[] clustering, IKMeansData[] means)
        {
            // returns false if there is a cluster that has no tuples assigned to it
            // parameter means[][] is really a ref parameter

            // check existing cluster counts
            // can omit this check if InitClustering and UpdateClustering
            // both guarantee at least one tuple in each cluster (usually true)
            int numClusters = means.Length;
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to means[][]

            // update, zero-out means so it can be used as scratch matrix 
            //Debug.Log(means.Length);
            for (int k = 0; k < means.Length; ++k)
            {
                //Debug.Log(means[k].Length());
                for (int j = 0; j < means[k].Length(); ++j)
                    means[k].SetValue(j, 0.0f);
            }

            for (int i = 0; i < data.Length; ++i)
            {
                int cluster = clustering[i];
                for (int j = 0; j < data[i].Length(); ++j)
                    //means[cluster][j] += data[i][j]; // accumulate sum
                    means[cluster].SetValue(j, means[cluster].Value(j)+ data[i].Value(j));
            }

            for (int k = 0; k < means.Length; ++k)
                for (int j = 0; j < means[k].Length(); ++j)
                   // means[k][j] /= clusterCounts[k]; // danger of div by 0
                    means[k].SetValue(j, means[k].Value(j)/ clusterCounts[k]);
            return true;
        }

        private static bool UpdateClustering(IKMeansData[] datas, int[] clustering, IKMeansData[] means)
        {
            // (re)assign each tuple to a cluster (closest mean)
            // returns false if no tuple assignments change OR
            // if the reassignment would result in a clustering where
            // one or more clusters have no tuples.

            int numClusters = means.Length;
            bool changed = false;

            int[] newClustering = new int[clustering.Length]; // proposed result
            Array.Copy(clustering, newClustering, clustering.Length);

            float[] distances = new float[numClusters]; // distances from curr tuple to each mean

            for (int i = 0; i < datas.Length; ++i) // walk thru each tuple
            {
                for (int k = 0; k < numClusters; ++k)
                    distances[k] = datas[i].Distance(means[k]); //Distance(data[i], means[k]); // compute distances from curr tuple to all k means

                int newClusterID = MinIndex(distances); // find closest mean ID
                if (newClusterID != newClustering[i])
                {
                    changed = true;
                    newClustering[i] = newClusterID; // update
                }
            }

            if (changed == false)
                return false; // no change so bail and don't update clustering[][]

            // check proposed clustering[] cluster counts
            int[] clusterCounts = new int[numClusters];
            for (int i = 0; i < datas.Length; ++i)
            {
                int cluster = newClustering[i];
                ++clusterCounts[cluster];
            }

            for (int k = 0; k < numClusters; ++k)
                if (clusterCounts[k] == 0)
                    return false; // bad clustering. no change to clustering[][]

            for(int i =0;i<datas.Length;i++)
                datas[i].SetCluster(newClustering[i]);


            Array.Copy(newClustering, clustering, newClustering.Length); // update
            return true; // good clustering and at least one change
        }

        private static float Distance(float[] tuple, float[] mean)
        {
            // Euclidean distance between two vectors for UpdateClustering()
            // consider alternatives such as Manhattan distance
            float sumSquaredDiffs = 0.0f;
            for (int j = 0; j < tuple.Length; ++j)
                sumSquaredDiffs += Mathf.Pow((tuple[j] - mean[j]), 2);
            return Mathf.Sqrt(sumSquaredDiffs);
        }

        private static int MinIndex(float[] distances)
        {
            // index of smallest value in array
            // helper for UpdateClustering()
            int indexOfMin = 0;
            float smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < smallDist)
                {
                    smallDist = distances[k];
                    indexOfMin = k;
                }
            }
            return indexOfMin;
        }

        // ============================================================================

        // misc display helpers for demo

//        static void ShowData(double[][] data, int decimals, bool indices, bool newLine)
//        {
//            for (int i = 0; i < data.Length; ++i)
//            {
//                if (indices) Console.Write(i.ToString().PadLeft(3) + " ");
//                for (int j = 0; j < data[i].Length; ++j)
//                {
//                    if (data[i][j] >= 0.0) Console.Write(" ");
//                    Console.Write(data[i][j].ToString("F" + decimals) + " ");
//                }
//                Debug.Log("");
//            }
//            if (newLine) Debug.Log("");
//        } // ShowData

//        static void ShowVector(int[] vector, bool newLine)
//        {
//            for (int i = 0; i < vector.Length; ++i)
//                Console.Write(vector[i] + " ");
//            if (newLine) Debug.Log("\n");
//        }
        
//        static void ShowClustered(double[][] data, int[] clustering, int numClusters, int decimals)
//        {
//            for (int k = 0; k < numClusters; ++k)
//            {
//                Debug.Log("===================");
//                for (int i = 0; i < data.Length; ++i)
//                {
//                    int clusterID = clustering[i];
//                    if (clusterID != k) continue;
//                    Console.Write(i.ToString().PadLeft(3) + " ");
//                    for (int j = 0; j < data[i].Length; ++j)
//                    {
//                        if (data[i][j] >= 0.0) Console.Write(" ");
//                        Console.Write(data[i][j].ToString("F" + decimals) + " ");
//                    }
//                    Debug.Log("");
//                }
//                Debug.Log("===================");
//            } // k
//        }
    } // Program
} // ns
