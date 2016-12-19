using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AnimateScreen))]
public abstract class View : MonoBehaviour {

    public abstract void Enter();
    public abstract void Exit();
    public abstract void SetActive(bool value);
}
