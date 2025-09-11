using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using Photon.Pun;


public class Generate : MonoBehaviour 
{
    [SerializeField]
    public Terrain Terrain;
    public static float[,] GenerationNoiseMap(int mapWidth, int mapHeight, float scale)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        if (scale <= 0)
        {
            scale = 0.0001f;
        }


        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float sampleX = x / scale;
                float sampleY = y / scale;
                float perlinValue = Mathf.PerlinNoise(sampleX, sampleY);
                noiseMap[x, y] = perlinValue;
            }
        }

        return noiseMap;
    }

    public static void PlaceObject(GameObject prefab, Vector3 position)
    {
        if (prefab == null)
        {
            Debug.LogError("Prefab is null!");
            return;  // Exit early if prefab is not assigned
        }

        float terrainHeight = Terrain.activeTerrain.SampleHeight(position);
        Vector3 adjustedPosition = new Vector3(position.x, terrainHeight, position.z);

        Debug.Log($"Placing object at position: {adjustedPosition}");
        UnityEngine.Object.Instantiate(prefab, adjustedPosition, Quaternion.identity);
    }


}
