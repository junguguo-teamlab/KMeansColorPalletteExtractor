using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KMeansCluster;
using System.Linq;
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
    public enum TextureType { SOURCE, RESULT, AVERAGERESULT}
    public TextureType texType = TextureType.RESULT;

    public MyData[] MyDatas;
    public Texture2D sourceTex;
    public bool LogData = false;
    public int ColorPalleteSize = 5;
    public Vector2 SampleSize = new Vector2(128,128);

    public Color[] ResultMainColors;
    //---------
    private Texture2D resultTex, avrTex;
    private Renderer _renderer;

    void Start ()
    {
        _renderer = GetComponent<Renderer>();


        resultTex = Resize(sourceTex, (int)SampleSize.x, (int)SampleSize.y);
        Color[] pix = resultTex.GetPixels(0, 0, resultTex.width, resultTex.height);


        //---**jungu:the following needs fixing: ----- how to resize an object array?
        //----update: end up using a list and convert it to array
        List<MyData> dataList = new List<MyData>();
        for (int i = 0; i < pix.Length; i++)
        {
            if (pix[i].r * 0.3 + pix[i].g * 0.59 + pix[i].b * 0.11 > 0.3f) //drop dark colors
                dataList.Add( new MyData(new float[] { pix[i].r, pix[i].g, pix[i].b }) );
        }
        MyDatas = dataList.ToArray();


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

        if(LogData)
            MyData.Print();



        ResultMainColors = new Color[ColorPalleteSize];
        int mainColIndex = 0;

        Color[] newPix = new Color[pix.Length];
        int ii = 0;


        foreach (var colorList in MyData.SortedData)
        {
            float rSum = 0.0f; float gSum = 0.0f; float bSum = 0.0f;
            int count = 0;
            foreach (var color in colorList.Value)
            {
                newPix[ii].r = color.values[0]; rSum += newPix[ii].r;
                newPix[ii].g = color.values[1]; gSum += newPix[ii].g;
                newPix[ii].b = color.values[2]; bSum += newPix[ii].b;
                ii++;
                count++;
            }
            ResultMainColors[mainColIndex++] = new Color(rSum/count,gSum/count,bSum/count);
        }
        resultTex.SetPixels(newPix);
        resultTex.Apply();
        avrTex = ColorPalletteTex(ResultMainColors);
        ShowTexture();
    }

    void Update()
    {
        ShowTexture();
    }

    public void ShowTexture()
    {
        switch (texType)
        {
            case TextureType.RESULT:
                _renderer.material.mainTexture = resultTex;
                break;
            case TextureType.AVERAGERESULT:
                _renderer.material.mainTexture = avrTex;
                break;
            case TextureType.SOURCE:
                _renderer.material.mainTexture = sourceTex;
                break;
        }
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

    public static Texture2D ColorPalletteTex(Color[] cols)
    {
        Texture2D nTex = new Texture2D(1, cols.Length);
        nTex.SetPixels(0,0,1,cols.Length,cols);
        nTex.Apply();
        return nTex;
    }

}
