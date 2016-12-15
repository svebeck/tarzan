using UnityEngine;
using System.Collections;

public class LightController : MonoBehaviour {

    public Transform target;
    public Light sun;
    public Camera camera;

    public Color sunLight;
    public Color caveLight;

    public Color daySkyColor;
    public Color caveSkyColor;

    public float sunIntensityStart = 1.6f;
    public float sunIntensityDeep = 0.3f;

    void Update()
    {
        float depthFactor = -target.position.y / 50f;

        sun.intensity = Mathf.Lerp(sunIntensityStart, sunIntensityDeep, depthFactor);
        sun.color = Color.Lerp(sunLight, caveLight, depthFactor);
        camera.backgroundColor = Color.Lerp(daySkyColor, caveSkyColor, depthFactor);
    }
}
