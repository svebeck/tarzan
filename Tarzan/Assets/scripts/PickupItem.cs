using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;

public class PickupItem : MonoBehaviour 
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

        StartCoroutine(ScaleDown());
    }

    IEnumerator ScaleDown()
    {
        float scale = 1f;

        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForEndOfFrame();

            scale -= 0.25f;
            transform.localScale = new Vector3(scale, scale, scale);
        }

        Destroy(this);
    }

}
