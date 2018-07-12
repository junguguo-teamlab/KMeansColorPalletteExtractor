using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KMeansCluster;

//[Serializable]
public class MyData : IKMeansData
{
    public int length;
    public float[] values;
    public int ClusterIndex;
    public static int ClusterCount; // obselete
    //public static MyData[] RawData;
    public static Dictionary<int,List<MyData>> SortedData;


    public MyData(float[] values)
    {
        this.length = values.Length;
        //this.values = (float[])values.Clone();//**maybe wrong? shalow copy?

        this.values = new float[values.Length]; // proposed result
        Array.Copy(values, this.values, values.Length);

        SortedData = new Dictionary<int, List<MyData>>();
//RawData = new List<MyData>();
    }

    public MyData(int length)
    {
        this.length = length;
        values = new float[length];
        SortedData = new Dictionary<int, List<MyData>>();
 //       RawData = new List<MyData>();
    }

    public void SetCluster(int ClusterIndex)
    {
        this.ClusterIndex = ClusterIndex;
    }

    public void SetClusterCount(int count)
    {
        MyData.ClusterCount = count;
    }

    public static void SortData(MyData data)
    {
        if (!SortedData.ContainsKey(data.ClusterIndex))
            SortedData.Add(data.ClusterIndex,new List<MyData>());

        SortedData[data.ClusterIndex].Add(data);
    }

    public static void Print()
    {
        foreach (var dataList in SortedData)
        {
            Debug.Log("============");
            foreach (var data in dataList.Value)
            {
                string values = "";
                foreach (var value in data.values)
                {
                    values+=(value+" ,");
                }
                Debug.Log(values+"\n");
            }

        }
    }
//    public void InitData(int length) //dont use this, it's only for KMeansCluster to use
//    {
//        this.length = length;
//        values = new float[length];
//    }

    public int Length()
    {
        return length;
    }

    public float Value(int index)
    {
        return values[index];
    }

    public void SetValue(int index, float value)
    {
        values[index] = value;
    }

    public float Distance(IKMeansData data)
    {

        float sumSquaredDiffs = 0.0f;
        for (int j = 0; j < values.Length; ++j)
            sumSquaredDiffs += Mathf.Pow((values[j] - data.Value(j)), 2);
        return Mathf.Sqrt(sumSquaredDiffs);

    }

}

public class TestKmeans : MonoBehaviour
{
    public MyData[] MyDatas;
    public Texture2D sourceTex;

    public int ColorPalleteSize = 5;
    public Vector2 ReSize = new Vector2(128,128);
    // Use this for initialization
    void Start () {

        Texture2D tex = Resize(sourceTex, (int)ReSize.x, (int)ReSize.y);
        Color[] pix = tex.GetPixels(0, 0, tex.width, tex.height);

        MyDatas = new MyData[pix.Length];
        for (int i = 0; i < pix.Length; i++)
            MyDatas[i] = new MyData(new float[] { pix[i].r, pix[i].g, pix[i].b });

        MyData[] means = new MyData[ColorPalleteSize];
	    for (int i = 0; i < ColorPalleteSize; i++)
	    {
            means[i] = new MyData(3);
	    }


	    int[] clustering = KMeansCluster.KMeansCluster.Cluster(MyDatas, means , ColorPalleteSize ); // this is it

	    foreach (var data in MyDatas)
	    {
	        MyData.SortData(data);
        }

        MyData.Print();




        Color[] newPix = new Color[pix.Length];
        int ii = 0;
        foreach (var tuple in MyData.SortedData)
        {
            foreach (var data in tuple.Value)
            {
                newPix[ii].r = data.values[0];
                newPix[ii].g = data.values[1];
                newPix[ii].b = data.values[2];
                ii++;
            }
        }
        tex.SetPixels(newPix);
        tex.Apply();
        GetComponent<Renderer>().material.mainTexture = tex;



    }

    public static Texture2D Resize(Texture2D source, int newWidth, int newHeight)
    {
        source.filterMode = FilterMode.Point;
        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
        rt.filterMode = FilterMode.Point;
        RenderTexture.active = rt;
        Graphics.Blit(source, rt);
        Texture2D nTex = new Texture2D(newWidth, newHeight);
        nTex.ReadPixels(new Rect(0, 0, newWidth, newWidth), 0, 0);
        nTex.Apply();
        RenderTexture.active = null;
        return nTex;
    }

}
