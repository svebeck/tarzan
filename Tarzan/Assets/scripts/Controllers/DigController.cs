using UnityEngine;
using System.Collections;

public class DigController : MonoBehaviour {
    public static DigController instance;

    public int digRadius = 0;
    public int bombRadius = 3;

    public int digDamage = 1;
    public int bombDamage = 3;

    public GameObject bombPrefab;
    public GameObject digPrefab;

    bool dirty = false;

    GameObject player;
    int [,] diggedPlaces;

    MapGenerator map;

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
        player = GameObject.FindGameObjectWithTag("Player");

	}

    public void Init(MapGenerator map)
    {
        this.map = map;

        diggedPlaces = new int[map.width, map.height];
    }

	void Update () 
    {
        if (map == null)
            return;

        if (player == null)
            return;

        if (Input.GetKeyUp(KeyCode.E))
        {
            Vector3 pos = player.transform.position;
            Vector2 direction = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized;
            Dig(pos, direction, player);
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            Vector3 pos = player.transform.position;
            Vector2 direction = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized;
            MakeMaterial(pos, direction);
        }

        if (Input.GetKeyUp(KeyCode.Q))
        {
            Vector2 direction = new Vector2 (Input.GetAxisRaw ("Horizontal"), Input.GetAxisRaw ("Vertical")).normalized*0.1f;
            Vector3 pos = player.transform.position;    

            pos.x += direction.x;

            StartCoroutine(Bomb(pos, direction));       
        }
	}

    Vector3 digPosition;
    public void Dig(Vector3 pos, Vector2 direction, GameObject digger)
    {
        digPosition.x = pos.x+direction.x;
        digPosition.y = pos.y+direction.y;
        Coord coord = map.WorldPointToCoord(pos);

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

        Debug.Log("Dig coords: " + coord.ToString());

        map.DrawCircle(diggedPlaces, coord, digRadius, digDamage, true);

        if (direction.y > 0 && direction.x > 0)
        {
            map.DrawCircle(diggedPlaces, new Coord(coord.tileX-1, coord.tileY), digRadius, digDamage, true);
        }

        if (direction.y > 0 && direction.x < 0)
        {
            map.DrawCircle(diggedPlaces, new Coord(coord.tileX+1, coord.tileY), digRadius, digDamage, true);
        }

        if (direction.y < 0 && direction.x != 0)
        {
            map.DrawCircle(diggedPlaces, new Coord(coord.tileX, coord.tileY+1), digRadius, digDamage, true);
        }

        dirty = false;

        int width = map.width;
        int height = map.height;

        for (int x = coord.tileX-10; x < coord.tileX+10; x++)
        {
            for (int y = coord.tileY-10; y < coord.tileY+10; y++)
            {
                if (x < 0 || x > width-1 || y < 0 || y > height-1)
                    continue;
                
                if (diggedPlaces[x,y] >= map.solidMap[x, y] && map.solidMap[x,y] != 0)
                {
                    map.solidMap[x, y] = 0;
                    diggedPlaces[x,y] = 0;
                    dirty = true;

                    GameObject go = Instantiate(digPrefab);
                    Vector3 worldPos = map.CoordToWorldPoint(new Coord(x,y));
                    go.transform.position = worldPos; 
                }
            }
        }

        if (!dirty)
            return;

        map.UpdateDynamicSolids();

        dirty = false;

        map.UpdateMeshGenerator(coord, map.chunkSize, map.solids, map.solidMap);
    }

    Vector3 bombPosition;
    public IEnumerator Bomb(Vector3 pos, Vector2 force)
    {

        bombPosition = pos;

        GameObject go = Instantiate(bombPrefab);

        go.transform.position = pos;
        go.GetComponent<Rigidbody2D>().AddForce(force);

        yield return new WaitForSeconds(1.9f);

        pos = go.transform.position;
        Coord coord = map.WorldPointToCoord(pos);

        Collider2D[] colliders = Physics2D.OverlapCircleAll(go.transform.position, bombRadius);
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


        map.DrawCircle(diggedPlaces, coord, bombRadius, bombDamage, true);

        dirty = false;

        int width = map.width;
        int height = map.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (diggedPlaces[x,y] >= map.solidMap[x, y] && map.solidMap[x, y] != 0)
                {
                    map.solidMap[x, y] = 0;
                    dirty = true;

                    go = Instantiate(digPrefab);
                    Vector3 worldPos = map.CoordToWorldPoint(new Coord(x,y));
                    go.transform.position = worldPos; 
                }
            }
        }

        if (!dirty)
            yield break;

        dirty = false;

        map.UpdateMeshGenerator(coord, map.chunkSize, map.solids, map.solidMap);
    }


    public void MakeMaterial(Vector3 pos, Vector3 direction)
    {
        Coord coord = map.WorldPointToCoord(pos);

        coord.tileX += (int)direction.x;
        coord.tileY += (int)direction.y;

        map.DrawCircle(map.solidMap, coord, 0, 1);

        map.UpdateMeshGenerator(coord, map.chunkSize, map.solids, map.solidMap);
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(digPosition, new Vector3(.5f,.5f,.5f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(bombPosition, 1f);
    }
}
