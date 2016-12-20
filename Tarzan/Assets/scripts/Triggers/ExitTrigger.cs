using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;
using TMPro;

public class ExitTrigger : MonoBehaviour 
{
    public int nextLevel;

    bool triggered = false;

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (triggered)
            return;

        triggered = true;

        App.instance.NextLevel(nextLevel);
    }

}

