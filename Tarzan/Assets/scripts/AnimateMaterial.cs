using UnityEngine;
using System.Collections;

public class AnimateMaterial : MonoBehaviour {

    public Material material;
    public float speedX;
    public float speedY;

    Vector2 offset;

	void Start () 
    {
	
	}
	
	void Update () 
    {
        offset.x += speedX*Time.deltaTime;
        offset.y += speedY*Time.deltaTime;

        material.mainTextureOffset = offset;
	}
}
