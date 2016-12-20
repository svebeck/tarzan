using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class RefreshLevel : MonoBehaviour {

	
    public void Refresh()
    {
        App.instance.Refresh();
    }
}
