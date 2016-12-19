using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;

public class ViewStandard : View {

    [Serializable]
    public class OnEnter: UnityEvent { }

    public OnEnter onEnter;

    // Use this for initialization
    void Start () {
    }

    // Update is called once per frame
    void Update () {

    }

    public override void Enter()
    {
        onEnter.Invoke();
    }

    public void Render()
    {

    }

    public override void Exit()
    {
    }

    public override void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }
}
