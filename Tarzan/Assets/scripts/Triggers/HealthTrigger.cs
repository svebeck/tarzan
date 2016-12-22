using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;
using TMPro;

public class HealthTrigger : MonoBehaviour 
{

    bool pickedUp = false;


    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag != "Player")
            return;

        if (pickedUp)
            return;

        pickedUp = true;

        HealthController healthController = collider.gameObject.GetComponent<HealthController>();
        healthController.GiveHealth(3f);

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

        //added delay for doing text effect
        yield return new WaitForSeconds(5f);

        Destroy(this);
    }

}

