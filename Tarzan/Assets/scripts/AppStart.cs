using UnityEngine;
using System.Collections;

public class AppStart : MonoBehaviour {

    private bool inited = false;

    IEnumerator Init() 
    {
        ViewController viewController = ViewController.instance;
        viewController.Init();
        viewController.ChangeView(viewController.viewLoading);

        MapGenerator map = MapGenerator.instance;
        yield return StartCoroutine(map.Generate());

        MapController mapController = MapController.instance;
        yield return StartCoroutine(mapController.Init(map));

        viewController.ChangeView(viewController.viewPlay);
	}


    void Update()
    {

        if (!inited)
        {
            StartCoroutine(Init());

            inited = true;
        }
    }
}
