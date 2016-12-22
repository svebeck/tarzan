using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickupController : MonoBehaviour {
    public static PickupController instance;

    public int healthPickups = 0;
    public GameObject healthPrefab;
    int spawnedHealthPickups = 0;

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
    }

    void Start()
    {
        mapController = MapController.instance;
        mapGenerator = MapGenerator.instance;
    }

    void Update()
    {
        SpawnHealthPickups();
    }

    void SpawnHealthPickups()
    {
        if (spawnedHealthPickups == healthPickups)
            return;

        if (mapGenerator.solidMap == null)
            return;
        
        int startY = (mapGenerator.height / healthPickups) * (spawnedHealthPickups);
        int endY = (mapGenerator.height / healthPickups) * (spawnedHealthPickups+1);

        Coord coord = mapGenerator.FindNearestEmpty(Random.Range(10, mapGenerator.width-10), Random.Range(startY, endY), 10);

        if (coord.tileX == -1 && coord.tileY == -1)
            return;

        Vector3 worldPos = mapController.CoordToWorldPoint(coord);

        GameObject go = Instantiate(healthPrefab);

        go.transform.position = worldPos;

        spawnedHealthPickups++;
    }

}
