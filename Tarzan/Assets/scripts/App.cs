using UnityEngine;
using System.Collections;
using UnityStandardAssets._2D;
using UnityEngine.SceneManagement;

public class App : MonoBehaviour 
{
    public static App instance;

    public GameObject playerPrefab;
    private GameObject player;

    private bool inited = false;

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

    IEnumerator Init() 
    {
        Camera.main.GetComponent<Camera2DFollow>().target = PlayerSpawn.instance.spawnPoint;

        ViewController viewController = ViewController.instance;
        viewController.Init();
        viewController.ChangeView(viewController.viewLoading);

        SpawnPlayer();

        MapGenerator map = MapGenerator.instance;
        yield return StartCoroutine(map.Generate());

        MapController mapController = MapController.instance;
        mapController.Init(map);

        viewController.ChangeView(viewController.viewPlay);

	}

    void SpawnPlayer()
    {
        if (player != null)
        {
            Destroy(player);
        }

        player = Instantiate(playerPrefab);
        player.transform.position = PlayerSpawn.instance.spawnPoint.position;

        Camera.main.GetComponent<Camera2DFollow>().target = player.transform;
    }

    void Update()
    {

        if (!inited)
        {
            StartCoroutine(Init());

            inited = true;
        }
    }

    public GameObject GetPlayer()
    {
        return player;
    }

    public void Refresh()
    {
        SpawnPlayer();
    }

    public void NextLevel(int level)
    {
        SceneManager.LoadScene("Level"+level);
    }
}
