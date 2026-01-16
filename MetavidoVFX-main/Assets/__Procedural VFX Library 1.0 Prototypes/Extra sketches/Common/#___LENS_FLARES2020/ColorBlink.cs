using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//using UnityEngine;

public class ColorBlink : MonoBehaviour
{
    // Fade the color from red to green
    // back and forth over the defined duration

    //Color colorStart = Color.red;
    //Color colorEnd = Color.black;
    //float duration = 1.0f;
    Renderer rend;


    //float floor = 0.03f;
    //float ceiling = 15.0f;
    

    void Start()
    {
        rend = GetComponent<Renderer>();
    }


    public Color startEmissionColor = new Color(0.1F, 0.1F, 0.0F, 0F);
    public Color newEmissionColor =new Color(1.1F, 0.1F, 0.0F, 1F);
    public float frequency = 5f;
    public float amplitude = 10f;
    public float glowstrength=1.0f;


    void Update()
    {
        //float emission = floor + Mathf.PingPong(Time.time, ceiling - floor);


        //float lerp = Mathf.PingPong(Time.time, duration) / duration;
       // rend.material.color = Color.Lerp(colorStart, colorEnd, lerp);
       // rend.material.SetColor("_EmissionColor", Color.Lerp(colorStart, colorEnd, lerp));
        //rend.material.SetColor("_EmissionColor", Color.Lerp(colorStart, colorEnd, lerp));
        //rend.material. = Color.Lerp(colorStart, colorEnd, lerp);


      //  Color baseColor = Color.yellow; //Replace this with whatever you want for your base color at emission level '1'

       // Color finalColor = colorStart * Mathf.LinearToGammaSpace(emission);

        //rend.material.SetColor("_EmissionColor", finalColor);

        float glow = ((1.5f + Mathf.Cos(Time.time * frequency)) * amplitude)* glowstrength;
        rend.material.SetColor("_EmissionColor", newEmissionColor * glow);
       // DynamicGI.UpdateMaterials(rend);

    }
}