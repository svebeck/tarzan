using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.SceneManagement;

public class LevelName : MonoBehaviour 
{

	void Start () 
    {
        GetComponent<TextMeshProUGUI>().text = SceneManager.GetActiveScene().name;
	}
}
