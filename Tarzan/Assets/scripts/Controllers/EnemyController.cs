using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemyController : MonoBehaviour {
    public static EnemyController instance;

    private GameObject target;

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

            if (target == null)
            {
                target = App.instance.GetPlayer();
                yield return true;
            }
            else if (target.transform.position.y < minSpawnDepth && enemies.Count < maxEnemies)
            {
                SpawnRandomEnemyAround(target);
            }
        }
    }

    public void SpawnRandomEnemyAround(GameObject target)
    {
        Vector3 playerPosition = target.transform.position;

        Vector3 distance = new Vector3((1-2*Random.value), (1-2*Random.value), 0);
        distance.Normalize();
        distance = distance * minSpawnDistance + distance * (maxSpawnDistance-minSpawnDistance);
            

        Vector3 spawnPosition = playerPosition + distance;

        Coord coord = mapController.WorldPointToCoordClamped(spawnPosition);

        coord = mapGenerator.FindNearestEmpty(coord.tileX, coord.tileY, 15);

        if (coord.tileX == -1 || coord.tileY == -1)
            return;

        Coord playerCoord = mapController.WorldPointToCoordClamped(playerPosition);

        float diff = (coord.tileX*coord.tileX+coord.tileY*coord.tileY) - (playerCoord.tileX*playerCoord.tileX+playerCoord.tileY*playerCoord.tileY);

        if (diff*diff < 5*5)
        {
            Debug.Log("Canceled spawn, too close!");
            return;
        }

        SpawnRandomEnemyAtCoord(coord);
    }

    public void SpawnRandomEnemyAtCoord(Coord coord)
    {
        float totalRarity = 0f;

        rarity.Sort(delegate(float a, float b) {
            return a < b ? -1 : 1;
        });

        foreach (float rar in rarity)
        {
            totalRarity += rar;
        }

        float value = Random.value;

        GameObject enemyPrefab = null;
        float r = 0f;
        for (int i = 0; i < rarity.Count; i++)
        {
            r += rarity[i];
            Debug.Log("Enemy rarity: " + r);
            if (value < r/totalRarity)
            {
                Debug.Log("Spawn: " + i);
                enemyPrefab = enemyPrefabs[i];
                break;
            }
        }

        if (enemyPrefab == null)
        {
            throw new UnityException("Rarity function is not really working :-/ no enemy selected: " + value + " : tr: " + totalRarity);
        }

        Vector3 worldPos = mapController.CoordToWorldPoint(coord);

        SpawnAtPosition(worldPos, enemyPrefab);
    }

    public void SpawnAtPosition(Vector3 pos, GameObject enemyPrefab)
    {

        GameObject zombieGo = Instantiate(enemyPrefab);
        zombieGo.transform.position = pos; 

        AIZombie zombie = zombieGo.GetComponent<AIZombie>();
        zombie.target = target;

        enemies.Add(zombieGo);
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

    public void SetTarget(GameObject target)
    {
        this.target = target;
    }

}
