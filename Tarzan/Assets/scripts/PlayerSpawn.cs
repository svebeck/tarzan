using UnityEngine;
using System.Collections;

public class PlayerSpawn : MonoBehaviour {
    public static PlayerSpawn instance;

    public Transform spawnPoint;

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

}
