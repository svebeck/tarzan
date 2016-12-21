using UnityEngine;
using System.Collections;
using System;
using UnityEngine.Events;

public class ViewDead : ViewStandard 
{
    void Start () 
    {
        
    }

    void Update () 
    {
        if (Input.GetButtonDown("Submit"))
        {
            App.instance.Refresh();
        }
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }
}
