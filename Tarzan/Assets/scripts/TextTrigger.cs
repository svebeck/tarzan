using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;
using TMPro;

public class TextTrigger : MonoBehaviour 
{
    public TextMeshPro text;

    [Serializable]
    public class OnEnter: UnityEvent { }

    public OnEnter onEnter;

    bool pickedUp = false;

    void Start()
    {
        text.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (pickedUp)
            return;

        text.gameObject.SetActive(true);

        pickedUp = true;

        onEnter.Invoke();

        StartCoroutine(Animate());
    }

    IEnumerator Animate()
    {
        float scale = 0f;
        text.transform.localScale = new Vector3();

        for (int i = 0; i < 4; i++)
        {
            yield return new WaitForEndOfFrame();

            scale += 0.25f;
            text.transform.localScale = new Vector3(scale, scale, scale);
        }

        for (int i = 0; i < 200; i++)
        {
            yield return new WaitForEndOfFrame();
            text.alpha -= 0.005f;
        }

        Destroy(this);
    }

}
