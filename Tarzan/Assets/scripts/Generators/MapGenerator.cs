using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class MapGenerator : MonoBehaviour {
    public static MapGenerator instance;

    [Header("Map Settings")]
    public int height = 128;
    public int width = 128;

    [Range(0,100)]
    public int randomFillPercent;

    public string seed;
    public bool useRandomSeed;

    [Range(0,10)]
    public int smoothing;

    public bool processRegions = true;
    public bool processBorder = true;
    public int borderSize = 5;

    [Header("Cave Settings")]
    public int wallTreshholdSize = 50;
    public int roomTreshholdSize = 50;

    public bool connectAllRooms = true;
    public int passageRadius = 5;

    [Header("Minerals")]
    public List<int> mineralDeposits;
    public List<int> maxMineralSizes;
    public List<int> materialHealth;
    public List<int> materialMinDepth;
    public List<int> materialMaxDepth;
    public List<int> materialShape;
    public List<int> materialBehaviour;

    [Header("Fluid Distribution")]
    public int waterPockets = 4;
    public int lavaPockets = 3;
    public int minLavaDepth = 90;
    public int minWaterDepth = 5;

    public int[,] solidMap;
    public int[,] fluidMap;

    System.Random pseudoRandom;

    void Awake()
    {
        if (instance != null)
        {
            Destroy(this);
        }
        else
        {
            instance = this;
        }
    }

	void Start() 
    {
	}


    public void Refresh()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public IEnumerator Generate()
    {
        StopCoroutine("UpdateFluids");
        StopCoroutine("UpdateByPlayer");

        solidMap = new int[width, height];
        fluidMap = new int[width, height];

        transform.position = new Vector3(0, -height*0.5f * MapController.instance.squareSize);

        RandomFillMap();

        MapJob mapJob = new MapJob();
        mapJob.smoothLevels = smoothing;
        mapJob.map = solidMap;
        mapJob.Start();

        yield return StartCoroutine(mapJob.WaitFor());

        if (processRegions) ProcessRegions(); 
        if (processBorder) ProcessBorder();
        RandomFillSolidMaterial();
        RandomFillFluidMaterial();
    }

    void ProcessBorder()
    {
        for (int x = 0; x < solidMap.GetLength(0); x++)
        {
            for (int y = 0; y < solidMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width - borderSize && y >= borderSize && y < height - borderSize)
                {
                    solidMap[x,y] = solidMap[x, y];
                }
                else
                {
                    solidMap[x,y] = 0; // handle more materials, maybe choose border material
                }
            }
        }
    }

    void ProcessRegions()
    {
        List<List<Coord>> wallRegions = GetRegions(1);

        print("wallRegions.Count: " + wallRegions.Count);

        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < wallTreshholdSize)
            {
                foreach(Coord tile in wallRegion)
                {
                    solidMap[tile.tileX, tile.tileY] = 0;
                }
            }
        }


        List<List<Coord>> roomRegions = GetRegions(0);
        List<Room> survivingRooms = new List<Room>();

        print("roomRegions.Count: " + roomRegions.Count);

        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < roomTreshholdSize)
            {
                foreach(Coord tile in roomRegion)
                {
                    solidMap[tile.tileX, tile.tileY] = 1; //TODO: handle multiple materials
                }
            }
            else
            {
                survivingRooms.Add(new Room(roomRegion, solidMap));
            }

        }

        if (connectAllRooms)
        {
            survivingRooms.Sort();

            if (survivingRooms.Count == 0)
            {
                throw new Exception("No surviving rooms!");
            }
            
            survivingRooms[0].isMainRoom = true;
            survivingRooms[0].isAccessibleFromMainRoom = true;

            ConnectClosestRooms(survivingRooms);
        }
    }

    void ConnectClosestRooms(List<Room> survivingRooms, bool forceAccessibilityFromMainRoom = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();

        if (forceAccessibilityFromMainRoom)
        {
            foreach (Room room in survivingRooms)
            {
                if (room.isAccessibleFromMainRoom)
                {
                    roomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = survivingRooms;
            roomListB = survivingRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccessibilityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                    continue;
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB)) 
                {
                    continue;
                }

                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms = (int)(Mathf.Pow(tileA.tileX - tileB.tileX, 2) + Mathf.Pow(tileA.tileY - tileB.tileY, 2));

                        if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }

            if (possibleConnectionFound && !forceAccessibilityFromMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }

        if (possibleConnectionFound && forceAccessibilityFromMainRoom)
        {
            CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(survivingRooms, true);
        }

        if (!forceAccessibilityFromMainRoom)
        {
            ConnectClosestRooms(survivingRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord coord in line)
        {
            DrawCircle(coord, passageRadius, 0);
        }
    }

    public Coord FindNearestEmpty(int x, int y, int tries)
    {
        if (!IsInsideMap(x, y))
            throw new UnityException("Can't find nearest outside of map.");
        
        if (solidMap[x,y] == 0)
            return new Coord(x, y);

        Coord coord = new Coord(x, y);

        Queue<Coord> tileQueue = new Queue<Coord>();
        tileQueue.Enqueue(coord);


        while(tries > 0 && tileQueue.Count > 0)
        {
            Coord tile = tileQueue.Dequeue();

            for (int xx = tile.tileX-1; xx <= tile.tileX+1; xx++)
            {
                for (int yy = tile.tileY-1; yy <= tile.tileY+1; yy++)
                {
                    if (IsInsideMap(xx, yy))
                    {
                        if (solidMap[xx, yy] > 0)
                        {
                            tileQueue.Enqueue(new Coord(xx, yy));
                        }
                        else if (solidMap[xx, yy] == 0)
                        {
                            coord = new Coord(xx, yy);
                            tileQueue.Clear(); 
                        }
                    }
                }
            }

            tries--;
        }

        if (tries == 0)
            return new Coord(-1,-1);

        return coord;
        
    }

    public void DrawDot(int x, int y, int value)
    {
        if (IsInsideMap(x, y))
        {
            solidMap[x, y] = value;
        }
    }
        
    public void DrawCircle(Coord coord, int radius, int value)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x*x + y*y <= radius*radius)
                {
                    int drawX = coord.tileX + x;
                    int drawY = coord.tileY + y;

                    if (IsInsideMap(drawX, drawY))
                    {
                        solidMap[drawX, drawY] = value;
                    }
                }
            }
        }
    }

    public void DrawCircle(int[,] map, Coord coord, int radius, int value, bool additive = false, int targetValue = -1)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                if (x*x + y*y <= radius*radius)
                {
                    int drawX = coord.tileX + x;
                    int drawY = coord.tileY + y;

                    if (IsInsideMap(drawX, drawY) && (targetValue == -1 || map[drawX, drawY] == targetValue))
                    {
                        if (additive)
                           map[drawX, drawY] += value;
                        else
                           map[drawX, drawY] = value;
                    }
                }
            }
        }
    }

    public void DrawVein(int[,] map, Coord coord, int length, int value, int targetValue = -1)
    {
        int drawX = coord.tileX;
        int drawY = coord.tileY;

        while (coord.tileX == drawX && coord.tileY == drawY)
        {
            drawX = coord.tileX + pseudoRandom.Next(-1,1);
            drawY = coord.tileY + pseudoRandom.Next(-1,1);
        }

        if (IsInsideMap(drawX, drawY) && (targetValue == -1 || map[drawX, drawY] == targetValue))
        {
            solidMap[drawX, drawY] = value;
            DrawVein(map, new Coord(drawX, drawY), --length, value, targetValue);
        }
    }

    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();

        int x = from.tileX;
        int y = from.tileY;

        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy);
            shortest = Mathf.Abs(dx);

            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x,y));

            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }

            gradAccumulation += shortest;
            if (gradAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradAccumulation -= longest;
            }
        }

        return line;
    }


    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];
    
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x,y] == 0 && (solidMap[x,y] == tileType || (tileType >= 1 && solidMap[x,y] >= 1)))
                {
                    List<Coord> region = GetRegionTiles(x,y);
                    regions.Add(region);

                    foreach (Coord tile in region)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = solidMap[startX, startY];

        Queue<Coord> tileQueue = new Queue<Coord>();
        tileQueue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;

        while(tileQueue.Count > 0)
        {
            Coord tile = tileQueue.Dequeue();
            tiles.Add(tile);

            for (int x = tile.tileX-1; x <= tile.tileX+1; x++)
            {
                for (int y = tile.tileY-1; y <= tile.tileY+1; y++)
                {
                    if (IsInsideMap(x,y) && (y == tile.tileY || x == tile.tileX))
                    {
                        if (mapFlags[x,y] == 0 && (solidMap[x,y] == tileType || (tileType >= 1 && solidMap[x,y] >= 1)))
                        {
                            mapFlags[x,y] = 1;
                            tileQueue.Enqueue(new Coord(x,y));
                        }
                    }
                }
            }
        }

        return tiles;
    }
	
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = Time.realtimeSinceStartup.ToString();
        }

        pseudoRandom = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width-1 || y == 0 || y == width-1)
                    solidMap[x,y] = 1;
                else
                    solidMap[x,y] = (pseudoRandom.Next(0,100) < randomFillPercent) ? 1:0;
            }
        }
    }


    void RandomFillSolidMaterial()
    {
        for (int i = 0; i < mineralDeposits.Count; i++)
        {
            for (int j = 0; j < mineralDeposits[i]; j++)
            {
                int material = i+1;

                int minDepth = materialMinDepth[material-1];
                int maxDepth = materialMaxDepth[material-1];

                int x = pseudoRandom.Next(0, width);
                int y = 0;

                if (minDepth > 0 && maxDepth > 0)
                    y = pseudoRandom.Next(height-maxDepth, height-minDepth);
                else if (minDepth > 0)
                    y = pseudoRandom.Next(0, height-minDepth);
                else if (maxDepth > 0)
                    y = pseudoRandom.Next(height-maxDepth, height);
                else
                    y = pseudoRandom.Next(0, height);
                    


                int maxSize = maxMineralSizes[material-1];

                if (maxSize < 1)
                    continue;

                int shape = materialShape[material-1];

                if (shape == 0)
                    shape = pseudoRandom.Next(1,3);

                // Circle
                if (shape == 1)
                {
                    DrawCircle(solidMap, new Coord(x,y), pseudoRandom.Next(1, maxSize), material, false, 1);
                }

                // Random Cluster
                else if (shape == 2)
                {
                    int size = pseudoRandom.Next(maxSize*2, maxSize*2);
                    for (int k = 0; k < size; k++)
                    {
                        DrawCircle(solidMap, new Coord(x+pseudoRandom.Next(-maxSize,maxSize),y+pseudoRandom.Next(-maxSize,maxSize)), 0, material, false, 1);
                    }
                }

                // Vein
                else if (shape == 3)
                {
                    int size = pseudoRandom.Next(maxSize, maxSize*2);
                    DrawVein(solidMap, new Coord(x,y), size, material, 1);
                }
            }
        }
    }

    void RandomFillFluidMaterial()
    {
        for (int i = 0; i < waterPockets; i++)
        {
            int x = pseudoRandom.Next(0, width);
            int y = pseudoRandom.Next(0, height-minWaterDepth);

            DrawCircle(fluidMap, new Coord(x,y), pseudoRandom.Next(5,15), 1, false, 0);
        }

        for (int i = 0; i < lavaPockets; i++)
        {
            int x = pseudoRandom.Next(0, width);
            int y = pseudoRandom.Next(0, height- minLavaDepth);

            DrawCircle(fluidMap, new Coord(x,y), pseudoRandom.Next(5,10), 2, false, 0);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                fluidMap[x,y] = solidMap[x,y] > 0 ? 0 : fluidMap[x,y];
            }
        }
    }


    public bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }


    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;

        public int roomSize;
        public bool isAccessibleFromMainRoom;
        public bool isMainRoom;

        public Room()
        {
            
        }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = roomTiles.Count;
            connectedRooms = new List<Room>();

            edgeTiles = new List<Coord>();
            foreach(Coord tile in roomTiles)
            {
                for (int x = tile.tileX-1; x <= tile.tileX +1; x++)
                {
                    for (int y = tile.tileY-1; y <= tile.tileY +1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (y >= map.GetLength(1) || x >= map.GetLength(0))
                            {
                                continue;
                            }
                            if (map[x,y] >= 1) //TODO: handle with material > 1
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }

        public void SetAccessibleFromMainRoom()
        {
            if (!isAccessibleFromMainRoom)
            {
                isAccessibleFromMainRoom = true;
                foreach (Room room in connectedRooms)
                {
                    room.SetAccessibleFromMainRoom();
                }
            }
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMainRoom)
                roomB.SetAccessibleFromMainRoom();
            else if (roomB.isAccessibleFromMainRoom)
                roomA.SetAccessibleFromMainRoom();
            
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);
        }

        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }
    }
}


public struct Coord
{
    public int tileX;
    public int tileY;

    public Coord(int x, int y)
    {
        tileX = x;
        tileY = y;
    }

    public string ToString()
    {
        return "coord( " + tileX + ", " + tileY + " )"; 
    }
}