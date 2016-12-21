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

    bool triggered = false;

    Collider2D target;

    void Start()
    {
        text.gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if (collider.tag != "Player")
            return;
        
        if (triggered)
            return;

        text.gameObject.SetActive(true);
        //text.transform.SetParent(collider.transform);

        target = collider;

        triggered = true;

        onEnter.Invoke();

        StartCoroutine(Animate());
    }

    void Update()
    {
        if (!triggered)
            return;

        if (target == null)
            return;

        text.transform.position = target.transform.position;
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

        for (int i = 0; i < 100; i++)
        {
            yield return new WaitForEndOfFrame();
            text.alpha -= 0.01f;
        }

        Destroy(this);
    }

}
