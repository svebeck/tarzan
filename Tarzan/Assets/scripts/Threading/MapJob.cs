using UnityEngine;

public class MapJob : ThreadedJob
{
    public int smoothLevels;
    public int[,] map;

    private int width;
    private int height;

    protected override void ThreadFunction()
    {
        width = map.GetLength(0);
        height = map.GetLength(1);

        for (int i = 0; i < smoothLevels; i++)
        {
            SmoothMap();
        }
    }
    protected override void OnFinished()
    {
        Debug.Log("Smoothing finished!");
    }


    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int surroundingWallCount = GetSurroundingWallCount(x,y); 
                if (surroundingWallCount > 4)
                {
                    map[x,y] = 1;
                }
                else if (surroundingWallCount < 4)
                {
                    map[x,y] = 0;
                }
            }
        }
    }


    int GetSurroundingWallCount(int x, int y)
    {
        int wallCount = 0;
        for (int neighborX = x-1; neighborX <= x+1; neighborX++)
        {
            for (int neighborY = y-1; neighborY <= y+1; neighborY++)
            {
                if (IsInsideMap(neighborX, neighborY))
                {
                    if (neighborX != x || neighborY != y)
                    {
                        int count = map[neighborX, neighborY];
                        wallCount += count >= 1 ? 1 : 0;
                    }
                }
                else
                {
                    wallCount += 1;
                }
            }    
        }

        return wallCount;
    }


    public bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

}