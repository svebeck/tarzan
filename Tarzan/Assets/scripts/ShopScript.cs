using UnityEngine;
using System.Collections;

public class ShopScript : MonoBehaviour {

    ViewController viewController;

    void Start()
    {
        viewController = ViewController.instance;
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        viewController.ChangeView(viewController.viewShop);
    }

    void OnTriggerExit2D(Collider2D collider)
    {
        viewController.ChangeView(viewController.viewPlay);
    }
}
