using UnityEngine;
using System.Collections;

public class LightController : MonoBehaviour 
{

    public Color sunLight;
    public Color caveLight;

    public Color daySkyColor;
    public Color caveSkyColor;

    public float sunIntensityStart = 1.6f;
    public float sunIntensityDeep = 0.3f;

    Light sun;
    Camera camera;

    void Start()
    {
        sun = GetComponent<Light>();
        camera = Camera.main;

    }

    void Update()
    {
        GameObject target = App.instance.GetPlayer();

        if (target == null)
            return;
        
        float depthFactor = -target.transform.position.y / 50f;

        sun.intensity = Mathf.Lerp(sunIntensityStart, sunIntensityDeep, depthFactor);
        sun.color = Color.Lerp(sunLight, caveLight, depthFactor);
        camera.backgroundColor = Color.Lerp(daySkyColor, caveSkyColor, depthFactor);
    }
}
