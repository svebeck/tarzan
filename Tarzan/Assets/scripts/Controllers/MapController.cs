using UnityEngine;
using System.Collections;

public class MapController : MonoBehaviour {
    public static MapController instance;

    [Header("Mesh Types")]
    public MeshGenerator solids;
    public MeshGenerator water;
    public MeshGenerator lava;

    [Header("Mesh Chunk Size")]
    public int chunkSize = 16;
    public float squareSize = 2;
    public int fluidChunkSize = 64;

    GameObject player;

    private int width;
    private int height;
    private MapGenerator map;
    private int[,] solidMap;
    private int[,] fluidMap;

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
        player.GetComponent<Rigidbody2D>().isKinematic = true;
    }

    public IEnumerator Init(MapGenerator map)
    {
        this.map = map;
        width = map.width;
        height = map.height;
        solidMap = map.solidMap;
        fluidMap = map.fluidMap;

        DigController.instance.Init();

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
                if (material > 0 && map.materialBehaviour[material-1] == 0)
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
}
