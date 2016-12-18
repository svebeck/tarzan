using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour {
    public static EnemyController instance;

    public GameObject player;


    public int minSpawnDepth = -50;
    public int minEnemies = 0;
    public int maxEnemies = 20;
    public float minSpawnDistance = 100f;
    public float maxSpawnDistance = 120f;
    public float killDistance = 140f;
    public float spawnInterwall = 0.2f;

    public List<GameObject> enemyPrefabs;
    public List<float> rarity;

    List<GameObject> enemies = new List<GameObject>();
    MapController mapController;
    MapGenerator mapGenerator;

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

        if (enemyPrefabs.Count != rarity.Count)
        {
            throw new UnityException("Enemy prefabs and rarity must be of equal length.");
        }
    }

    void Start()
    {
        mapController = MapController.instance;
        mapGenerator = MapGenerator.instance;

        StartCoroutine(UpdateSpawn());
    }


    void Update()
    {
        RemoveInactiveEnemies();
    }

    IEnumerator UpdateSpawn()
    {
        for (;;)
        {
            yield return new WaitForSecondsRealtime(0.2f);

            if (player.transform.position.y < minSpawnDepth && enemies.Count < maxEnemies)
            {
                SpawnAround(player);
            }
        }
    }

    public void SpawnAround(GameObject player)
    {
        Vector3 playerPosition = player.transform.position;

        Vector3 distance = new Vector3((1-2*Random.value), (1-2*Random.value), 0);
        distance.Normalize();
        distance = distance * minSpawnDistance + distance * (maxSpawnDistance-minSpawnDistance);
            

        Vector3 spawnPosition = playerPosition + distance;

        Coord coord = mapController.WorldPointToCoordClamped(spawnPosition);

        coord = mapGenerator.FindNearestEmpty(coord.tileX, coord.tileY, 5);

        if (coord.tileX == -1 || coord.tileY == -1)
            return;

        SpawnAtCoord(coord);
    }

    public void SpawnAtCoord(Coord coord)
    {
        float totalRarity = 0f;

        rarity.Sort(delegate(float a, float b) {
            return a < b ? 1 : -1;
        });

        foreach (float rar in rarity)
        {
            totalRarity += rar;
        }

        float value = Random.value;

        GameObject enemy = null;

        for (int i = 0; i < rarity.Count; i++)
        {
            if (value < rarity[i]/totalRarity)
            {
                enemy = enemyPrefabs[i];
            }
        }

        if (enemy == null)
        {
            throw new UnityException("Rarity function is not really working :-/ no enemy selected");
        }

        Vector3 worldPos = mapController.CoordToWorldPoint(coord);
        GameObject zombieGo = Instantiate(enemy);
        zombieGo.transform.position = worldPos; 

        AIZombie zombie = zombieGo.GetComponent<AIZombie>();
        zombie.target = player;

        enemies.Add(zombieGo);

        int x = coord.tileX;
        int y = coord.tileY;

        /*
        mapGenerator.DrawDot(x-1, y, 0);
        mapGenerator.DrawDot(x+1, y, 0);
        mapGenerator.DrawDot(x, y, 0);
        mapGenerator.DrawDot(x, y+2, 1);
        mapGenerator.DrawDot(x, y-1, 1);
        mapGenerator.DrawDot(x-1, y-1, 1);
        mapGenerator.DrawDot(x+1, y-1, 1);*/
    }

    public void RemoveInactiveEnemies()
    {
        int l = enemies.Count;
        for( int i = l-1; i >= 0; i--)
        {
            GameObject enemy = enemies[i];

            if (!enemy.activeSelf)
            {
                enemies.Remove(enemy);
                Destroy(enemy);
            }
        }
    }

}
