using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DepthScript : MonoBehaviour {

    private TextMeshProUGUI text;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    void Update()
    {
        GameObject player = App.instance.GetPlayer();

        if (player == null)
            return;

        text.text = Mathf.Floor(player.transform.position.y) + " m";
    }
}
