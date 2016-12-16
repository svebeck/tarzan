using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class MapGenerator : MonoBehaviour {
    public static MapGenerator instance;

    [Header("Mesh Types")]
    public MeshGenerator solids;
    public MeshGenerator water;
    public MeshGenerator lava;

    [Header("Map Settings")]
    public int height = 128;
    public int width = 128;

    public int chunkSize = 16;
    public float squareSize = 1;

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
    public int mineralDeposits = 5;
    public List<int> maxMineralSizes;

    [Header("Fluid Distribution")]
    public int waterPockets = 4;
    public int lavaPockets = 3;
    public int fluidChunkSize = 64;

    public int [,] solidMap;
    public int [,] fluidMap;

    Dictionary<string, int> checkedUpdateMeshCoords = new Dictionary<string, int>();

    System.Random pseudoRandom;

    GameObject player;

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
        player = GameObject.FindGameObjectWithTag("Player");

        StartCoroutine(GenerateMap());
	}

    void Update() 
    {
        checkedUpdateMeshCoords.Clear();
    }

    public void Refresh()
    {
        Application.LoadLevel(Application.loadedLevelName);
    }

    IEnumerator GenerateMap()
    {
        StopCoroutine("UpdateFluids");
        StopCoroutine("UpdateByPlayer");

        solidMap = new int[width, height];
        fluidMap = new int[width, height];

        transform.position = new Vector3(0, -height*0.5f * squareSize);

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


        player.GetComponent<Rigidbody2D>().isKinematic = true;

        DigController.instance.Init(this);


        Vector3 pos = player.transform.position;
        Coord coord = WorldPointToCoord(pos);
        playerCoord = coord;

        UpdateMeshGenerator(coord, chunkSize, solids, solidMap, false);

        player.transform.Translate(Vector3.up*5);

        yield return new WaitForSeconds(0.5f);

        player.GetComponent<Rigidbody2D>().isKinematic = false;

        StartCoroutine(UpdateDynamic());
        StartCoroutine(UpdateByPlayer());
    }

    public void UpdateMeshGenerator(Coord coord, int chunkSize, MeshGenerator meshGenerator, int[,] map, bool forceClear = false)
    {
        useRandomSeed = false;
        int chunkX = coord.tileX  - (coord.tileX % chunkSize);
        int chunkY = coord.tileY  - (coord.tileY % chunkSize);

        //print("chunkX: " + chunkX + " chunkY: " + chunkY);
        int diffX = coord.tileX - playerCoord.tileX;
        int diffY = coord.tileY - playerCoord.tileY;

        //print("diffX: " + diffX + " diffY: " + diffY);

        if (diffX*diffX+diffY*diffY > chunkSize*chunkSize*4*4)
            return;

        for (int i = chunkX-chunkSize*4; i <= chunkX+chunkSize*4; i += chunkSize )
        {
            for (int j = chunkY-chunkSize*4; j <= chunkY+chunkSize*4; j += chunkSize )
            {
                if (i < 0 || i > width-chunkSize || j < 0 || j > height-chunkSize)
                    continue;

                meshGenerator.GenerateMesh(map, i, j, chunkSize, squareSize, forceClear);
            }
        }

        useRandomSeed = true;
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

    public Coord FindNearestEmpty(int x, int y, int radius)
    {
        if (!IsInsideMap(x, y))
            throw new UnityException("Can't find nearest outside of map.");
        
        if (solidMap[x,y] == 0)
            return new Coord(x, y);

        Coord coord = new Coord(x, y);

        Queue<Coord> tileQueue = new Queue<Coord>();
        tileQueue.Enqueue(coord);


        while(tileQueue.Count > 0)
        {
            Coord tile = tileQueue.Dequeue();

            for (int xx = tile.tileX-1; x <= tile.tileX+1; x++)
            {
                for (int yy = tile.tileY-1; y <= tile.tileY+1; y++)
                {
                    if (IsInsideMap(xx, yy))
                    {
                        if (solidMap[xx, yy] == 1)
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
        }

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

    Vector3 SwitchUp(Vector3 pos)
    {
        float yy = pos.y;
        pos.y = pos.z;
        pos.z = yy;

        return pos;
    }

    public Coord WorldPointToCoordClamped(Vector3 worldPos)
    {
        Coord coord = WorldPointToCoord(worldPos);

        coord.tileX = Mathf.Clamp(coord.tileX, 0, width);
        coord.tileY = Mathf.Clamp(coord.tileY, 0, height);

        return coord;
    }

    public Coord WorldPointToCoord(Vector3 worldPos)
    {
        worldPos = SwitchUp(worldPos);
        return new Coord(Mathf.RoundToInt(width/2 - .5f + worldPos.x/squareSize), (int)( height - .5f + worldPos.z/squareSize));
    }

    public Vector3 CoordToWorldPoint(Coord tile)
    {
        Vector3 pos = new Vector3(-width+.5f + tile.tileX*squareSize, 2, -height*2+.5f+tile.tileY*squareSize);
        return SwitchUp(pos);
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
        for (int i = 0; i < mineralDeposits; i++)
        {
            int x = pseudoRandom.Next(0, width);
            int y = pseudoRandom.Next(0, height);
            int material = pseudoRandom.Next(2, solids.materials.Length+1);

            int maxSize = maxMineralSizes[material-1];

            if (maxSize < 2)
                return;

            DrawCircle(solidMap, new Coord(x,y), pseudoRandom.Next(2,maxSize), material, false, 1);
        }
    }

    void RandomFillFluidMaterial()
    {
        for (int i = 0; i < waterPockets; i++)
        {
            int x = pseudoRandom.Next(0, width);
            int y = pseudoRandom.Next(0, height);

            DrawCircle(fluidMap, new Coord(x,y), pseudoRandom.Next(5,15), 1, false, 0);
        }

        for (int i = 0; i < lavaPockets; i++)
        {
            int x = pseudoRandom.Next(0, width);
            int y = pseudoRandom.Next(0, height);

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


    float[,] fluidMassMap;
    float[,] newfluidMassMap;

    //Water properties
    [Header("Fluid Simulation")]
    public float maxMass = 1.0f; //The normal, un-pressurized mass of a full water cell
    public float maxCompress = 0.02f; //How much excess water a cell can store, compared to the cell above it
    public float minMass = 0.0001f;  //Ignore cells that are almost dry
    public float maxSpeed = 1f;
    public float minFlow = 0.0001f;

    IEnumerator UpdateDynamic()
    {
        fluidMassMap = new float[width, height];
        newfluidMassMap = new float[width, height];

        //initial fluid flooding
        for (int y = height-1; y > -1; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (fluidMap[x,y] > 0)
                {
                    fluidMassMap[x,y] = 1f;
                    newfluidMassMap[x,y] = 1f;
                }
            }
        }

        for(;;)
        {
            yield return new WaitForSeconds(0.1f);

            yield return new WaitForFixedUpdate();

            UpdateFluids();
            UpdateDynamicSolids();
            DoChemistry();
        }
    }

    void UpdateFluids()
    {
        for (int x = playerCoord.tileX - 30; x < playerCoord.tileX + 30; x++)
        {
            for (int y = playerCoord.tileY - 30; y < playerCoord.tileY + 30; y++)
            {
                if (x <= 0 || x >= width-1 || y <= 0 || y >= height-1)
                    continue;

                int material = fluidMap[x,y];
                if (material == 0)
                    continue;

                float remainingMass = fluidMassMap[x,y];

                if (remainingMass <= 0)
                    continue;
                
                if (solidMap[x,y] != 0)
                {
                    newfluidMassMap[x,y] = 0;

                    if (fluidMap[x,y-1] == material)
                        newfluidMassMap[x,y-1] = remainingMass*0.25f;
                    if (fluidMap[x,y+1] == material)
                        newfluidMassMap[x,y+1] = remainingMass*0.25f;
                    if (fluidMap[x-1,y] == material)
                        newfluidMassMap[x-1,y] = remainingMass*0.25f;
                    if (fluidMap[x+1,y] == material)
                        newfluidMassMap[x+1,y] = remainingMass*0.25f;

                    continue;
                }

                /*float fluidBelow = fluidMap[x,y-1];
                float fluidAbove = fluidMap[x,y+1];
                float fluidRight = fluidMap[x-1,y];
                float fluidLeft = fluidMap[x+1,y];
                */
                int solidBelow = solidMap[x,y-1];
                int solidAbove = solidMap[x,y+1];
                int solidLeft = solidMap[x-1,y];
                int solidRight = solidMap[x+1,y];


                if (solidBelow == 0)// && (fluidBelow == 0 || fluidBelow == material))
                {
                    float destMass = fluidMassMap[x,y-1];
                    float moveMass = GetStableFluidMass(remainingMass + destMass) - destMass;
                    moveMass = moveMass > minFlow ? moveMass*0.5f : moveMass;
                    moveMass = Mathf.Clamp(moveMass, 0, Mathf.Min(maxSpeed, remainingMass));

                    newfluidMassMap[x,y-1] += moveMass;
                    newfluidMassMap[x,y] -= moveMass;
                    remainingMass -= moveMass;

                    fluidMap[x,y-1] = material;
                }

                if (remainingMass <= 0)
                {
                    continue;
                }

                if (solidRight == 0)// && (fluidRight == 0 || fluidRight == material))
                {
                    float destMass = fluidMassMap[x+1,y];
                    float moveMass = (fluidMassMap[x,y]-destMass)*0.5f;
                    moveMass = moveMass > minFlow ? moveMass*0.5f : moveMass;
                    moveMass = Mathf.Clamp(moveMass, 0, remainingMass);

                    newfluidMassMap[x+1,y] += moveMass;
                    newfluidMassMap[x,y] -= moveMass;
                    remainingMass -= moveMass;

                    fluidMap[x+1,y] = material;
                }

                if (remainingMass <= 0)
                {
                    continue;
                }

                if (solidLeft == 0)// && (fluidLeft == 0 || fluidLeft == material))
                {
                    float destMass = fluidMassMap[x-1,y];
                    float moveMass = (fluidMassMap[x,y]-destMass)*0.5f;
                    moveMass = moveMass > minFlow ? moveMass*0.5f : moveMass;
                    moveMass = Mathf.Clamp(moveMass, 0, remainingMass);

                    newfluidMassMap[x-1,y] += moveMass;
                    newfluidMassMap[x,y] -= moveMass;
                    remainingMass -= moveMass;

                    fluidMap[x-1,y] = material;
                }

                if (remainingMass <= 0)
                {
                    continue;
                }

                if (solidAbove == 0)// && (fluidAbove == 0 || fluidAbove == material))
                {
                    float destMass = fluidMassMap[x,y+1];
                    float moveMass = remainingMass - GetStableFluidMass(remainingMass + destMass);
                    moveMass = moveMass > minFlow ? moveMass*0.5f : moveMass;
                    moveMass = Mathf.Clamp(moveMass, 0, Mathf.Min(maxSpeed, remainingMass));
                    //print("moveMAss up: " + moveMass);
                    newfluidMassMap[x,y+1] += moveMass;
                    newfluidMassMap[x,y] -= moveMass;
                    remainingMass -= moveMass;

                    fluidMap[x,y+1] = material;
                }

                if (remainingMass > 0)
                    fluidMap[x,y] = material;

            }
        }

        for (int x = playerCoord.tileX - 31; x < playerCoord.tileX + 31; x++)
        {
            for (int y = playerCoord.tileY - 31; y < playerCoord.tileY + 31; y++)
            {
                if (x <= 0 || x >= width-1 || y <= 0 || y >= height-1)
                    continue;

                fluidMassMap[x,y] = newfluidMassMap[x,y];

                if (fluidMassMap[x,y] < minMass)
                {
                    fluidMap[x,y] = 0;
                }
            }
        }
    }

    float GetStableFluidMass(float totalMass)
    { 
        if ( totalMass <= 1f )
        { 
            return 1f; 
        } 
        else if(totalMass < 2f*maxMass + maxCompress )
        { 
            return (maxMass*maxMass + totalMass*maxCompress)/(maxMass + maxCompress); 
        } 
        else 
        { 
            return (totalMass + maxCompress)/2f; 
        } 
    }

    public void UpdateDynamicSolids()
    {
        for (int x = playerCoord.tileX - 30; x < playerCoord.tileX + 30; x++)
        {
            for (int y = playerCoord.tileY - 30; y < playerCoord.tileY + 30; y++)
            {
                if (x <= 0 || x >= width-1 || y <= 0 || y >= height-1)
                    continue;
                
                int material = solidMap[x,y];
                if (material != 2)
                    continue;
                    
                if (solidMap[x,y-1] != 0)
                    continue;
                

                if ((x == playerCoord.tileX && y-1 == playerCoord.tileY) ||
                    (x-1 == playerCoord.tileX && y-1 == playerCoord.tileY) ||
                    (x+2 == playerCoord.tileX && y-1 == playerCoord.tileY) ||
                    (x-1 == playerCoord.tileX && y-1 == playerCoord.tileY) ||
                    (x+2 == playerCoord.tileX && y-1 == playerCoord.tileY)||
                    (x == playerCoord.tileX && y-2 == playerCoord.tileY))
                {
                    //player is below, do something
                    continue;
                }


                solidMap[x,y] = 0;
                solidMap[x,y-1] = material;
            }
        }
    }

    void DoChemistry()
    {
        Coord coord = new Coord(0,0);

        for (int x = playerCoord.tileX - 30; x < playerCoord.tileX + 30; x++)
        {
            for (int y = playerCoord.tileY - 30; y < playerCoord.tileY + 30; y++)
            {
                if (x <= 0 || x >= width-1 || y <= 0 || y >= height-1)
                    continue;

                int material = fluidMap[x,y];

                //if (material == 0)
                //    continue;

                int bottomMaterial = fluidMap[x,y+1];
                int rightMaterial = fluidMap[x+1,y];

                if (material == 2 && bottomMaterial == 1)
                {
                    fluidMap[x,y] = 0;
                    //fluidMap[x,y+1] = 0;
                    solidMap[x,y] = 4; // solid lava
                }
                else if (material == 1 && bottomMaterial == 2)
                {
                    //fluidMap[x,y] = 0;
                    fluidMap[x,y+1] = 0;
                    solidMap[x,y+1] = 4; // solid lava
                }
                else if (material == 2 && rightMaterial == 1)
                {
                    fluidMap[x,y] = 0;
                    //fluidMap[x+1,y] = 0;
                    solidMap[x,y] = 4; // solid lava
                }
                else if (material == 1 && rightMaterial == 2)
                {
                    //fluidMap[x,y] = 0;
                    fluidMap[x+1,y] = 0;
                    solidMap[x+1,y] = 4; // solid lava
                }
            }
        }

        if (coord.tileX != 0 || coord.tileY != 0)
            UpdateMeshGenerator(coord, chunkSize, solids, solidMap);
    }

    Coord playerCoord;
    IEnumerator UpdateByPlayer()
    {
        for (;;)
        {
            yield return new WaitForFixedUpdate();

            if (player == null)
                yield break;
            
            Vector3 pos = player.transform.position;
            Coord coord = WorldPointToCoord(pos);
                
            if (coord.tileX != playerCoord.tileX || coord.tileY != playerCoord.tileY)
            {
                UpdateMeshGenerator(coord, chunkSize, solids, solidMap, false);
            }

            UpdateMeshGenerator(coord, fluidChunkSize, water, fluidMap, false);
            UpdateMeshGenerator(coord, fluidChunkSize, lava, fluidMap, false);



            playerCoord = coord;
        }
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