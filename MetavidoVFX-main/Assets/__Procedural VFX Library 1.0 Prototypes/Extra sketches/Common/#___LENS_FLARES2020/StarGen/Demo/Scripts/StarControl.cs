using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StarControl : MonoBehaviour {

    public GameObject[] stars;
    public Image color;

    public Slider flow;
    public Slider size;
    public Slider spots;
    [Header("-")]
    public Slider promAmount;
    public Slider promSize;
    [Header("-")]
    public Toggle flare;
    public Slider flareSize;
    public Slider flareContr;
    [Header("-")]
    public Toggle sprite;
    public Toggle simple;
    public Slider spriteScale;
    public Slider spriteDens;
    public Slider spriteWaves;
    public Slider spriteRipple;
    [Header("-")]
    public Toggle part;
    public Slider partDens;
    public Slider partSize;
    public Slider partSpread;
    public Slider partWaves;
    public Slider partWavesFreq;
    public Toggle trails;
    public Slider trailsRes;
    [Header("-")]
    public Slider coronaSpeed;
    public Slider coronaBrightness;

    public Slider R, G, B;

    public Slider X, Y, Z;

    bool colSet=false;

    public int picked=0;

    HU_Star starController;

    private void Start()
    {
        GetController();
    }

    void GetController()
    {
        starController = stars[picked].GetComponent<HU_Star>();
        flow.value = starController._flowSpeed;
        size.value = starController._size;
        spots.value = starController._spots;

        promAmount.value = starController.prominenceAmount;
        promSize.value = starController.prominenceSize;

        flare.isOn = starController._flare;
        flareSize.value = starController._flareSize;
        flareContr.value = starController._flareContrast;

        sprite.isOn = starController._coronaSprites;
        simple.isOn = starController._simpleCoronaSprites;
        spriteScale.value = starController._coronaSize;
        spriteDens.value = starController._coronaDensity;
        spriteWaves.value = starController._coronaWaves;
        spriteRipple.value = starController._coronaRipple;

        part.isOn = starController._coronaParticles;
        partDens.value = starController._particleDensity;
        partSpread.value = starController._particleSpread;
        partSize.value = starController._coronaParticlesSize;
        partWaves.value = starController._particleWavesAmplitude;
        partWavesFreq.value = starController._particleWavesFreq;
        trails.isOn = starController._coronaTrails;
        trailsRes.value = starController._trailsResolution;

        coronaBrightness.value = starController._coronaBrightness;
        coronaSpeed.value = starController._coronaSpeed;


        colSet = false;
        R.value = starController._starMaterial.GetColor("_Color").r;
        G.value = starController._starMaterial.GetColor("_Color").g;
        B.value = starController._starMaterial.GetColor("_Color").b;
        colSet = true;
        color.material.SetColor("_Color",new Color(R.value, G.value, B.value));
        X.value = stars[picked].transform.localScale.y;
        Y.value = stars[picked].transform.localScale.z;
        Z.value = stars[picked].transform.localScale.x;
    }


    public void OnPick(int i)
    {
        if (picked == i) return;
        StartCoroutine(MoveAway(picked));
        picked = i;
        StartCoroutine(MoveIn(picked));
    }
    IEnumerator MoveAway(int index)
    {
        float t = 0;
        while (t < 1)
        {
            stars[index].transform.Translate(-Camera.main.transform.right * Time.deltaTime*5f);
            t += Time.deltaTime*5f;
            yield return null;
        }
        stars[index].SetActive(false);
    }
    IEnumerator MoveIn(int index)
    {
        GetController();
        float t = 0;
        stars[index].SetActive(true);
        stars[index].transform.position = Camera.main.transform.right * 20;
        while (t < 1)
        {
            stars[index].transform.position = Vector3.Lerp(Camera.main.transform.right * 20, Vector3.zero, t);
            t += Time.deltaTime * 0.6f;
            yield return null;
        }
        stars[index].transform.position = Vector3.zero;
    }
    public void OnScale()
    {
        stars[picked].transform.localScale = new Vector3(Y.value, Z.value, X.value);
    }


	public void OnFlow(float t)
    {
        starController._flowSpeed = t;
    }
    public void OnSize(float t)
    {
        starController._size = t;
    }
    public void OnSpots(float t)
    {
        starController._spots = t;
    }

    public void OnCoronaSprites(bool t)
    {
        starController._coronaSprites = t;
    }
    public void OnSpritesSimple(bool t)
    {
        starController._simpleCoronaSprites = t;
    }
    public void OnCoronaSpriteSize(float t)
    {
        starController._coronaSize = t;
    }
    public void OnCoronaDensity(float t)
    {
        starController._coronaDensity =Mathf.RoundToInt(t*50);
    }
    public void OnCoronaWaves(float t)
    {
        starController._coronaWaves = t;
    }
    public void OnCoronaRipple(float t)
    {
        starController._coronaRipple = t;
    }

    public void OnProminence(float t)
    {
        starController.prominenceAmount = t;
    }
	public void OnProminenceSize(float t)
    {
        starController.prominenceSize = t;
    }

    public void OnFlare(bool t)
    {
        starController._flare = t;
    }
    public void OnFlareSize(float t)
    {
        starController._flareSize = t;
    }
    public void OnFlareContrast(float t)
    {
        starController._flareContrast=t;
    }

    public void OnPart(bool t)
    {
        starController._coronaParticles=t;
    }
    public void OnPartSize(float t)
    {
        starController._coronaParticlesSize=t;
    }
    public void OnPartDens(float t)
    {
        starController._particleDensity=t;
    }
    public void OnPartSpread(float t)
    {
        starController._particleSpread = t;
    }
    public void OnPartWaves(float t)
    {
        starController._particleWavesAmplitude = t;
    }
    public void OnPartWavesFreq(float t)
    {
        starController._particleWavesFreq = t;
    }
    public void OnTrails(bool t)
    {
        starController._coronaTrails=t;
    }
    public void OnTrailsRes(float t)
    {
        starController._trailsResolution = t;
    }
    public void OnCoronaBrightness(float t)
    {
        starController._coronaBrightness = t;
    }
    public void OnCornaSpeed(float t)
    {
        starController._coronaSpeed = t;
    }

    public void OnR(float t)
    {
        if (!colSet) return;
        Color col = starController._starMaterial.GetColor("_Color");
        col.r = t;
        starController._color = col;

    }
    public void OnG(float t)
    {
        if (!colSet) return;
        Color col = starController._starMaterial.GetColor("_Color");
        col.g = t;
        starController._color = col;
    }
    public void OnB(float t)
    {
        if (!colSet) return;
        Color col = starController._starMaterial.GetColor("_Color");
        col.b = t;
        starController._color = col;
    }
    void Update () {
		
	}
}
