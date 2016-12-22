using UnityEngine;
using System.Collections;
using UnityStandardAssets._2D;

public class DigController : MonoBehaviour {
    public static DigController instance;

    public int digRadius = 0;
    public int bombRadius = 3;

    public int digDamage = 1;
    public int bombDamage = 3;

    public float digCooldown = 0.1f;

    public GameObject bombPrefab;
    public GameObject digPrefab;

    bool dirty = false;

    bool onCooldown = false;

    GameObject player;
    int [,] diggedPlaces;

    MapGenerator mapGenerator;
    MapController mapController;

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

	void Start () {
        mapController = MapController.instance;
        mapGenerator = MapGenerator.instance;

	}

    public void Init()
    {
        diggedPlaces = new int[mapGenerator.width, mapGenerator.height];
    }

	void Update () 
    {
        player = App.instance.GetPlayer();

        if (mapGenerator == null)
            return;

        if (player == null)
            return;

        if (onCooldown)
            return;

        PlatformerCharacter2D character = player.GetComponent<PlatformerCharacter2D>();

        if (Input.GetKeyUp(KeyCode.E) && digDamage > 0)
        {
            Vector3 pos = player.transform.position;
            Vector2 direction = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized;
            Dig(pos, direction, player);
            StartCoroutine(Cooldown(digCooldown));
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            Vector3 pos = player.transform.position;
            Vector2 direction = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized;
            MakeMaterial(pos, direction);
            StartCoroutine(Cooldown(digCooldown));
        }

        if (Input.GetKeyUp(KeyCode.Q) && bombDamage > 0)
        {
            Vector2 direction = new Vector2 (character.GetFaceDirection(), Input.GetAxisRaw ("Vertical")).normalized;
            Vector3 pos = player.transform.position;    

            pos.x += direction.x;

            StartCoroutine(PlantBomb(pos, direction));  
            StartCoroutine(Cooldown(digCooldown));     
        }
	}

    IEnumerator Cooldown(float value)
    {
        onCooldown = true;
        yield return new WaitForSeconds(value);
        onCooldown = false;
    }

    Vector3 digPosition;
    public void Dig(Vector3 pos, Vector2 direction, GameObject digger)
    {
        digPosition.x = pos.x+direction.x;
        digPosition.y = pos.y+direction.y;
        Coord coord = mapController.WorldPointToCoord(pos);

        coord.tileX += Mathf.RoundToInt(direction.x);
        coord.tileY += Mathf.RoundToInt(direction.y);


        if (digger == player) // only do damage while digging as a player
        {
            Collider2D[] colliders = Physics2D.OverlapCircleAll(digPosition, 1);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i].gameObject != digger)
                {
                    HealthController healthController = colliders[i].gameObject.GetComponent<HealthController>();
                    if (healthController != null)
                    {
                        healthController.TakeDamage(digDamage);
                    }
                }
            }
        }

        mapGenerator.DrawCircle(diggedPlaces, coord, digRadius, digDamage, true);

        if (direction.y > 0 && direction.x > 0)
        {
            mapGenerator.DrawCircle(diggedPlaces, new Coord(coord.tileX-1, coord.tileY), digRadius, digDamage, true);
        }

        if (direction.y > 0 && direction.x < 0)
        {
            mapGenerator.DrawCircle(diggedPlaces, new Coord(coord.tileX+1, coord.tileY), digRadius, digDamage, true);
        }

        if (direction.y < 0 && direction.x != 0)
        {
            mapGenerator.DrawCircle(diggedPlaces, new Coord(coord.tileX, coord.tileY+1), digRadius, digDamage, true);
        }

        dirty = false;

        int width = mapGenerator.width;
        int height = mapGenerator.height;

        for (int x = coord.tileX-10; x < coord.tileX+10; x++)
        {
            for (int y = coord.tileY-10; y < coord.tileY+10; y++)
            {
                if (x < 0 || x > width-1 || y < 0 || y > height-1)
                    continue;

                if (mapGenerator.solidMap[x, y]-1 < 0)
                    continue;

                if (diggedPlaces[x,y] >= mapGenerator.materialHealth[mapGenerator.solidMap[x, y]-1])
                {
                    GameObject go = Instantiate(digPrefab);
                    Vector3 worldPos = mapController.CoordToWorldPoint(new Coord(x,y));
                    go.transform.position = worldPos; 

                    mapGenerator.solidMap[x, y] = 0;
                    diggedPlaces[x,y] = 0;
                    dirty = true;

                }
            }
        }

        if (!dirty)
        {
            return;
        }

        mapController.UpdateDynamicSolids();

        dirty = false;

        mapController.UpdateMeshGenerator(coord, mapController.chunkSize, mapController.solids, mapGenerator.solidMap);
    }

    Vector3 bombPosition;
    public IEnumerator PlantBomb(Vector3 pos, Vector2 force)
    {
        bombPosition = pos;

        GameObject go = Instantiate(bombPrefab);

        go.transform.position = pos;
        go.GetComponent<Rigidbody2D>().AddForce(force);

        yield return new WaitForSeconds(1.9f);

        ExplodeBomb(go.transform.position);
    }

    public void ExplodeBomb(Vector3 pos)
    {
        Coord coord = mapController.WorldPointToCoord(pos);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(pos, bombRadius);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i].gameObject != gameObject)
            {
                HealthController healthController = colliders[i].gameObject.GetComponent<HealthController>();
                if (healthController != null)
                {
                    healthController.TakeDamage(bombDamage);
                }
            }
        }

        mapGenerator.DrawCircle(diggedPlaces, coord, bombRadius, bombDamage, true);

        dirty = false;

        int width = mapGenerator.width;
        int height = mapGenerator.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapGenerator.solidMap[x, y]-1 < 0)
                    continue;

                if (diggedPlaces[x,y] >= mapGenerator.materialHealth[mapGenerator.solidMap[x, y]-1])
                {
                    mapGenerator.solidMap[x, y] = 0;
                    dirty = true;

                    GameObject go = Instantiate(digPrefab);
                    Vector3 worldPos = mapController.CoordToWorldPoint(new Coord(x,y));
                    go.transform.position = worldPos; 
                }
            }
        }

        if (!dirty)
            return;

        dirty = false;

        mapController.UpdateMeshGenerator(coord, mapController.chunkSize, mapController.solids, mapGenerator.solidMap);
    }


    public void MakeMaterial(Vector3 pos, Vector3 direction)
    {
        Coord coord = mapController.WorldPointToCoord(pos);

        coord.tileX += (int)direction.x;
        coord.tileY += (int)direction.y;

        mapGenerator.DrawCircle(mapGenerator.solidMap, coord, 0, 1);

        mapController.UpdateMeshGenerator(coord, mapController.chunkSize, mapController.solids, mapGenerator.solidMap);
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(digPosition, new Vector3(.5f,.5f,.5f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bombPosition, 1f);
    }

    public void SetDigDamage(int damage)
    {
        digDamage = damage;
    }


    public void SetBombDamage(int damage)
    {
        bombDamage = damage;
    }
}
