using UnityEngine;
using System.Collections;

public class SpawnOnce : MonoBehaviour {
    public GameObject enemyPrefab;

    public void Spawn()
    {
        EnemyController.instance.SpawnAtPosition(this.transform.position, enemyPrefab);
    }
}
