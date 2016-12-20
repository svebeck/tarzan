using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;
using TMPro;

public class Trigger : MonoBehaviour 
{
    [Serializable]
    public class OnEnter: UnityEvent { }

    public OnEnter onEnter;

    bool pickedUp = false;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (pickedUp)
            return;

        pickedUp = true;

        onEnter.Invoke();
    }

}
