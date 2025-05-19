using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{
    /// <summary>
    /// 使用多层次的柏林噪声生成二维噪声图。
    /// </summary>
    /// <param name="mapWidth">噪声图的宽度。</param>
    /// <param name="mapHeight">噪声图的高度。</param>
    /// <param name="scale">噪声图的缩放比例。较大的值会生成更缩小的噪声。</param>
    /// <param name="octaves">要组合的柏林噪声层数。 (大于1） </param>
    /// <param name="persistance">控制每个后续层的振幅衰减（值在0到1之间）。</param>
    /// <param name="lacunarity">控制每个后续层的频率增加（值大于1）。</param>
    /// <returns>一个二维浮点数组，表示生成的噪声图，归一化到范围[0, 1]。</returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, float scale, int octaves, float persistance,
        float lacunarity)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];

        //防止除以0，除以负数
        if (scale <= 0)
        {
            scale = 0.0001f;
        }

        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                //使用unity的柏林函数
                float amplitude = 1; //振幅
                float frequency = 1; //频率
                float noiseHeight = 0; //高度，即最终该点的颜色值，将每一度的振幅相加来获得
                //分octaves次级
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = x / scale * frequency;
                    float sampleY = y / scale * frequency; //用频率影响采样点间隔

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                    noiseHeight += perlinValue * amplitude; //振幅影响

                    amplitude *= persistance; //更换新的频率和振幅
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxNoiseHeight)
                {
                    maxNoiseHeight = noiseHeight;
                }
                else if (noiseHeight < minNoiseHeight)
                {
                    minNoiseHeight = noiseHeight;
                }

                noiseMap[x, y] = noiseHeight;
            }
        }

        //把噪声的范围从[minNoiseHeight,maxNoiseHeight]归一化到[0,1]
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }

        return noiseMap;
    }
}