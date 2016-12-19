using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine;

public enum Direction
{
    UP,
    DOWN,
    RIGHT,
    LEFT,
    NONE
}

public enum Position
{
    TOP,
    BOTTOM,
    RIGHT,
    LEFT,
    CENTER,
    CURRENT
}

public class AnimateScreen : MonoBehaviour {

    public float duration = 0.5f;

    RectTransform parentRect;
    RectTransform rectTransform;
    float timer = 0;
    Vector2 target;

    Vector2 startPosition;
    Vector2 currentPosition;
    Vector2 goalPosition;

    bool hasStartPosition = false;


    void Start()
    {
        
    }

    public void Center(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.NONE, active);
    }

    public void AnimateDown(bool active)
    {
        BeginAnimate(Position.CURRENT, Direction.DOWN, active);
    }

    public void AnimateUp(bool active)
    {
        BeginAnimate(Position.CURRENT, Direction.UP, active);
    }

    public void AnimateRight(bool active)
    {
        BeginAnimate(Position.CURRENT, Direction.RIGHT, active);
    }

    public void AnimateLeft(bool active)
    {
        BeginAnimate(Position.CURRENT, Direction.LEFT, active);
    }

    public void AnimateDownFromCenter(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.DOWN, active);
    }

    public void AnimateUpFromCenter(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.UP, active);
    }

    public void AnimateRightFromCenter(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.RIGHT, active);
    }

    public void AnimateLeftFromCenter(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.LEFT, active);
    }

    public void AnimateDownFromTop(bool active)
    {
        BeginAnimate(Position.CENTER, Direction.DOWN, active);
    }

    public void AnimateRightFromLeft(bool active)
    {
        BeginAnimate(Position.LEFT, Direction.RIGHT, active);
    }

    public void AnimateLeftFromRight(bool active)
    {
        BeginAnimate(Position.RIGHT, Direction.LEFT, active);
    }

    public void AnimateUpFromBottom(bool active)
    {
        BeginAnimate(Position.BOTTOM, Direction.UP, active);
    }
	
    public void BeginAnimate(Position from, Direction to, bool active) 
    {
        gameObject.SetActive(true);

        parentRect = transform.parent.GetComponent<RectTransform>();
        rectTransform = GetComponent<RectTransform>();

        if (!hasStartPosition)
        {
            hasStartPosition = true;
            startPosition = rectTransform.anchoredPosition;
        }

        if (from == Position.CURRENT)
            currentPosition = rectTransform.anchoredPosition;
        else if (from == Position.CENTER)
        {
            currentPosition = new Vector2();
        }
        else if (from == Position.TOP)
        {
            currentPosition = startPosition;
            currentPosition.y += parentRect.sizeDelta.y;
        }
        else if (from == Position.BOTTOM)
        {
            currentPosition = startPosition;
            currentPosition.y -= parentRect.sizeDelta.y;
        }
        else if (from == Position.LEFT)
        {
            currentPosition = startPosition;
            currentPosition.x -= parentRect.sizeDelta.x;
        }
        else if (from == Position.RIGHT)
        {
            currentPosition = startPosition;
            currentPosition.x += parentRect.sizeDelta.x;
        }

        rectTransform.anchoredPosition = currentPosition;

        goalPosition.x = currentPosition.x;
        goalPosition.y = currentPosition.y;

        if (to == Direction.UP)
            goalPosition.y += parentRect.sizeDelta.y;
        else if (to == Direction.DOWN)
            goalPosition.y -= parentRect.sizeDelta.y;
        else if (to == Direction.RIGHT)
            goalPosition.x += parentRect.sizeDelta.x;
        else if (to == Direction.LEFT)
            goalPosition.x -= parentRect.sizeDelta.x;

        StopAllCoroutines();
        StartCoroutine(Animate(active));
	}

    IEnumerator Animate(bool active)
    {
        //GameController.instance.raycaster.enabled = false;

        float time = 0f;
        while (time < duration)
        {
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, goalPosition, time/duration);

            yield return new WaitForEndOfFrame();

            time += Time.deltaTime;
        }
        //GameController.instance.raycaster.enabled = true;
        gameObject.SetActive(active);
    }
}
