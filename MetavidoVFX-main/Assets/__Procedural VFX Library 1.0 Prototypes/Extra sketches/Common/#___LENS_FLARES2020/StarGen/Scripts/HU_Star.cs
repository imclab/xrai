using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class HU_Star : MonoBehaviour
{
    
    public Shader starShader;
    public Shader coronaShader;
    public Shader simpleCoronaShader;
    public Shader jetShader;
    //public Shader accretionShader;
    bool forceUpdate;
     bool enab = false;
    [SerializeField]
    Material flareMaterial=null;
   // Material accretionMaterial;

    //[SerializeField]
    //bool accretionDisk;
    //public bool _accretionDisk
    //{
    //    get { return accretionDisk; }
    //    set
    //    {
    //        if (_accretionDisk == value && !forceUpdate) return;
    //        accretionDisk = value;
    //        if (_accretionDisk)
    //        {
    //            if (accretionShader == null) accretionShader = Shader.Find("Star/Accresion Disk");
    //            accretionMaterial = new Material(accretionShader);
    //            GameObject inst = new GameObject();
    //            inst.transform.parent = transform;
    //            inst.transform.localPosition = Vector3.zero;
    //            inst.name = "Accretion Disk";
    //            accretionComp = inst.AddComponent<AccretionDisk>();
    //            accretionComp.material = accretionMaterial;
    //            accretionComp.speed = accretionSpeed;
    //            accretionComp.size = accretionSize;
    //            accretionComp.color = accretionColor1;
    //            accretionComp.color2 = accretionColor2;
    //            accretionComp.twist = accretionTwist;
    //            accretionComp.segments = accretionSegments;
    //        }
    //        else
    //        {
    //            if (transform.Find("Accretion Disk") != null) DestroyImmediate(transform.Find("Accretion Disk").gameObject);
    //        }
    //    }
    //}
    //[SerializeField]
    //int accretionSegments;
    //public int _accretionSegments
    //{
    //    get { return accretionSegments; }
    //    set
    //    {
    //        if (accretionSegments == value && !forceUpdate) return;
    //        accretionSegments = value;
    //        if (accretionComp == null) return;
    //        accretionComp.segments = accretionSegments;
    //    }
    //}
    //[SerializeField]
    //Color accretionColor2;
    //public Color _accretionColor2
    //{
    //    get { return accretionColor2; }
    //    set
    //    {
    //        if (accretionColor2 == value && !forceUpdate) return;
    //        accretionColor2 = value;
    //        if (accretionComp == null) return;
    //        accretionComp.color2 = accretionColor2;
    //    }
    //}
    //[SerializeField]
    //Color accretionColor1;
    //public Color _accretionColor1
    //{
    //    get { return accretionColor1; }
    //    set
    //    {
    //        if (accretionColor1 == value && !forceUpdate) return;
    //        accretionColor1 = value;
    //        if (accretionComp == null) return;
    //        accretionColor1.a = accretionComp.color.a;
    //        accretionComp.color = accretionColor1;
    //    }
    //}
    //[SerializeField]
    //float accretionSpeed;
    //public float _accretionSpeed
    //{
    //    get { return accretionSpeed; }
    //    set
    //    {
    //        if (accretionSpeed == value && !forceUpdate) return;
    //        accretionSpeed = value;
    //        if (accretionComp == null) return;
    //        accretionComp.speed = accretionSpeed;
    //    }
    //}

    //[SerializeField]
    //float accretionSize;
    //public float _accretionSize
    //{
    //    get { return accretionSize; }
    //    set
    //    {
    //        if (accretionSize == value && !forceUpdate) return;
    //        accretionSize = value;
    //        if (accretionComp == null) return;
    //        accretionComp.size = accretionSize;
    //    }
    //}

    //[SerializeField]
    //float accretionTwist;
    //public float _accretionTwist
    //{
    //    get { return accretionTwist; }
    //    set
    //    {
    //        if (accretionTwist == value && !forceUpdate) return;
    //        accretionTwist = value;
    //        if (accretionComp == null) return;
    //        accretionComp.twist = accretionTwist;
    //    }
    //}
    //[SerializeField]
    //float accretionInnerRadus;
    //public float _accretionInnerRadius
    //{
    //    get { return accretionInnerRadus; }
    //    set
    //    {
    //        if (accretionInnerRadus == value && !forceUpdate) return;
    //        accretionInnerRadus = value;
    //        if (accretionComp == null) return;
    //        accretionComp.innerRadius = accretionInnerRadus;
    //    }
    //}

    [SerializeField]
    Material starMaterial;
    public Material _starMaterial
    {
        get
        {
            return starMaterial;
        }
        set
        {
            
            if (starMaterial == value&&!forceUpdate&&!forceUpdate&&!forceUpdate) return;
            starMaterial = value;
            starRend.sharedMaterial = starMaterial;

        }
    }
    [SerializeField]
    Material coronaMateral;
    public Material _coronaMateral
    {
        get
        {
            return coronaMateral;
        }
        set
        {
            if (coronaMateral == value&&!forceUpdate&&!forceUpdate) return;
            coronaMateral = value;
            GenerateCorona();
        }
    }
    public Material _jetMaterial;
    [SerializeField]
    bool jets;
    public bool _jets
    {
        get
        {
            return jets;
        }
        set
        {
            if (jets == value&&!forceUpdate) return;
            jets = value;
            if (jets) CreateJets();
            else
            {
                if(transform.Find("Jets")!=null)
                DestroyImmediate(transform.Find("Jets").gameObject);
                if(_jetMaterial!=null)
                DestroyImmediate(_jetMaterial);
            }
        }
    }
    [SerializeField]
    float jetHeight;
    public float _jetHeight
    {
        get
        {
            return jetHeight;
        }
        set
        {
            if (jetHeight == value && !forceUpdate) return;
            jetHeight = value;
            Transform parent = transform.Find("Jets");
            if (parent == null) return;
            _jetMaterial.SetTextureScale("_Main", jetMatScale);
            
            for(int i = 0; i < parent.childCount; i++)
            {
                parent.GetChild(i).localScale=new Vector3(jetWidth, jetHeight, 1);
            }
        }
    }
    [SerializeField]
    float jetSmear;
    public float _jetSmear
    {
        get
        {
            return jetSmear;
        }
        set
        {
            if (jetSmear == value && !forceUpdate) return;
            jetSmear = value;
            if (_jetMaterial != null) _jetMaterial.SetTextureScale("_Main", jetMatScale);
        }
    }
    Vector2 jetMatScale
    {
        get { return new Vector2(1, jetHeight/((jetSmear*2+1)* 2)); }
    }
    [SerializeField]
    float jetWidth;
    public float _jetWidth
    {
        get
        {
            return jetWidth;
        }
        set
        {
            if (jetWidth == value && !forceUpdate) return;
            jetWidth = value;
            Transform parent = transform.Find("Jets");
            if (parent == null) return;
            for(int i = 0; i < parent.childCount; i++)
            {
                parent.GetChild(i).localScale=new Vector3(jetWidth,jetHeight, 1);
            }
        }
    }
    [SerializeField]
    float jetSpread;
    public float _jetSpread
    {
        get
        {
            return jetSpread;
        }
        set
        {
            if (jetSpread == value && !forceUpdate) return;
            jetSpread = value;
            CreateJets();
        }
    }
    [SerializeField]
    float jetSpeed;
    public float _jetSpeed
    {
        get
        {
            return jetSpeed;
        }
        set
        {
            if (jetSpeed == value && !forceUpdate) return;
            jetSpeed = value;
            if (_jetMaterial == null) return;
            _jetMaterial.SetFloat("_Speed", jetSpeed);
        }
    }
    [SerializeField]
    int jetDensity;
    public int _jetDensity
    {
        get
        {
            return jetDensity;
        }
        set
        {
            if (jetDensity == value && !forceUpdate) return;
            jetDensity = value;
            _jetMaterial.SetColor("_Color", color * coronaBrightness / jetDensity * 1f);
            CreateJets();
        }
    }
    
    [SerializeField]
    float size;
    public float _size
    {
        get
        {
            return size;
        }
        set
        {
            if (size == value&&!forceUpdate&&!forceUpdate) return;
            size = value;
            starMaterial.SetFloat("_Size", size);
        }
    }

    [SerializeField]
    Color color;
    public Color _color
    {
        get
        {
            return color;
        }
        set
        {
            if (color == value&&!forceUpdate&&!forceUpdate) return;
            color = value;
            color.a = 1;
            starMaterial.SetColor("_Color", color);
            coronaMateral.SetColor("_Color", color * coronaBrightness);
            if (jets) _jetMaterial.SetColor("_Color", color * coronaBrightness/ jetDensity * 1f);
            if (flare)
            {
                if (flareMaterial == null) CreateFlare();
                flareMaterial.SetColor("_Color", color * coronaBrightness);
            }
            var main = coronaParticlesComp.main;
            var colorG = coronaParticlesComp.colorOverLifetime.color;
            //GradientColorKey[] key = new GradientColorKey[2];
            //key[0] = new GradientColorKey(color * coronaBrightness, 0);
            //key[1] = new GradientColorKey(color * coronaBrightness, 1);
            //colorG.gradient.SetKeys(key, colorG.gradient.alphaKeys);
            main.startColor = color * coronaBrightness/3;
        }

    }
    [SerializeField]
    bool singleColor;
    public bool _singleColor
    {
        get
        {
            return singleColor;
        }
        set
        {
            if (singleColor == value && !forceUpdate) return;
            singleColor = value;
            if (singleColor) starMaterial.SetInt("_SingleColor", 1);
            else starMaterial.SetInt("_SingleColor", 0);
        }
    }
    [SerializeField]
    Color color2;
    public Color _color2
    {
        get
        {
            return color2;
        }
        set
        {
            if (color2 == value && !forceUpdate) return;
            color2 = value;
            starMaterial.SetColor("_Color2", color2);
        }
    }
    [SerializeField]
    float rim;
    public float _rim
    {
        get
        {
            return rim;
        }
        set
        {
            if (rim == value&&!forceUpdate&&!forceUpdate) return;
            rim = value;
            starMaterial.SetFloat("_Rim", rim);
        }
    }
    [SerializeField]
    float flowSpeed;
    public float _flowSpeed
    {
        get
        {
            return flowSpeed;
        }
        set
        {
            if (flowSpeed == value&&!forceUpdate&&!forceUpdate) return;
            flowSpeed = value;
            starMaterial.SetFloat("_FlowSpeed", flowSpeed);
        }
    }
    [SerializeField]
    float spots;
    public float _spots
    {
        get
        {
            return spots;
        }
        set
        {
            if (spots == value&&!forceUpdate) return;
            spots = value;
            starMaterial.SetFloat("_Spots", spots);
        }
    }
    public Shader flareShader;
    [SerializeField]
    GameObject flareObj;
    [SerializeField]
    bool flare;
    public bool _flare
    {
        get
        {
            return flare;
        }
        set
        {
            if (flare == value&&!forceUpdate) return;
            flare = value;
            if (flare)
            {
                CreateFlare();
            }
            else
            {
                if (transform.Find("Flare") != null) ;
                DestroyImmediate(transform.Find("Flare").gameObject);
                DestroyImmediate(flareMaterial);
            }
        }
    }
    [SerializeField]
    float flareContrast;
    public float _flareContrast
    {
        get
        {
            return flareContrast;
        }
        set
        {
            if (flareContrast == value&&!forceUpdate) return;
            flareContrast = value;
            if (flare)
            {
                if (flareMaterial == null) CreateFlare();
                flareMaterial.SetFloat("_Contrast", flareContrast);
            }
        }
    }
    [SerializeField]
    float flareSize;
    public float _flareSize
    {
        get
        {
            return flareSize;
        }
        set
        {
            if (flareSize == value&&!forceUpdate) return;
            flareSize = value;
            if (flare)
            {
                if (flareMaterial == null) CreateFlare();
                flareObj.transform.localScale = Vector3.one * (1 + flareSize*3);
            }
        }
    }
    [SerializeField]
    bool coronaSprites;
    public bool _coronaSprites
    {
        get
        {
            return coronaSprites;
        }
        set
        {
            if (coronaSprites == value&&!forceUpdate) return;
            coronaSprites = value;
            GenerateCorona();
        }
    }
    [SerializeField]
    bool simpleCoronaSprites;
    public bool _simpleCoronaSprites
    {
        get
        {
            return simpleCoronaSprites;
        }
        set
        {
            if (simpleCoronaSprites == value&&!forceUpdate) return;
            simpleCoronaSprites = value;
            if (simpleCoronaSprites)
                coronaMateral.shader = simpleCoronaShader;
            else
                coronaMateral.shader = coronaShader;
            if (coronaSprites) GenerateCorona();
        }
    }
    [SerializeField]
    bool coronaParticles;
    public bool _coronaParticles
    {
        get
        {
            return coronaParticles;
        }
        set
        {
            if (coronaParticles == value&&!forceUpdate) return;
            coronaParticles = value;
            coronaParticlesComp.gameObject.SetActive(coronaParticles);


            //
        }
    }
    [SerializeField]
    bool coronaTrails;
    public bool _coronaTrails
    {
        get
        {
            return coronaTrails;
        }
        set
        {
            if (coronaTrails == value&&!forceUpdate) return;
            coronaTrails = value;
            var trails = coronaParticlesComp.trails;
            trails.enabled = coronaTrails;
            var main = coronaParticlesComp.main;
            var shape = coronaParticlesComp.shape;
            if (coronaTrails)
            {
                main.startSizeMultiplier = 0;
                shape.radius = 0.4f;
            }
            else
            {
                main.startSizeMultiplier = coronaParticlesSize;
                shape.radius = 0.5f;
            }
            //
        }
    }
    [SerializeField]
    float trailsResolution;
    public float _trailsResolution
    {
        get
        {
            return trailsResolution;
        }
        set
        {
            if (trailsResolution == value&&!forceUpdate) return;
            trailsResolution = value;
            var trails= coronaParticlesComp.trails;
            if (trailsResolution == 0) trails.minVertexDistance = 50;
            else
            trails.minVertexDistance = Mathf.Max((1- trailsResolution),0.01f);
        }
    }
    [SerializeField]
    float particlesDensity;
    public float _particleDensity
    {
        get
        {
            return particlesDensity;
        }
        set
        {
            if (particlesDensity == value&&!forceUpdate) return;
            particlesDensity = value;
            var emission = coronaParticlesComp.emission;
            emission.rateOverTime = 100 * particlesDensity;
//            emission.rateOverTimeMultiplier = 2 * particlesDensity;
        }
    }
    [SerializeField]
    float particleSpread;
    public float _particleSpread
    {
        get
        {
            return particleSpread;
        }
        set
        {
            if (particleSpread == value&&!forceUpdate) return;
            particleSpread = value;
            var main = coronaParticlesComp.main;
            main.startSpeed = particleSpread * 0.05f;
        }
    }
    [SerializeField]
    float particleWavesAmplitude;
    public float _particleWavesAmplitude
    {
        get
        {
            return particleWavesAmplitude;
        }
        set
        {
            if (particleWavesAmplitude == value&&!forceUpdate) return;
            particleWavesAmplitude = value;
            var noise = coronaParticlesComp.noise;
            noise.strength = particleWavesAmplitude * 0.15f;
            if (particleWavesAmplitude == 0) noise.enabled = false;
            else noise.enabled = true;
        }
    }
    [SerializeField]
    float particleWavesFreq;
    public float _particleWavesFreq
    {
        get
        {
            return particleWavesFreq;
        }
        set
        {
            if (particleWavesFreq == value&&!forceUpdate) return;
            particleWavesFreq = value;
            var noise = coronaParticlesComp.noise;
            noise.frequency = particleWavesFreq * 5;
        }
    }

    [SerializeField]
    float coronaParticlesSize;
    public float _coronaParticlesSize
    {
        get
        {
            return coronaParticlesSize;
        }
        set
        {
            if (coronaParticlesSize == value&&!forceUpdate) return;
            coronaParticlesSize = value;
            var trails = coronaParticlesComp.trails;
            trails.widthOverTrailMultiplier = coronaParticlesSize;
            if (coronaTrails) return;
            var main = coronaParticlesComp.main;
            main.startSizeMultiplier = coronaParticlesSize;
        }
    }
    [SerializeField]
    int coronaDensity;
    public int _coronaDensity
    {
        get
        {
            return coronaDensity;
        }
        set
        {
            if (coronaDensity == value&&!forceUpdate) return;
            coronaDensity = value;
            GenerateCorona();
        }
    }
    [SerializeField]
    float coronaBrightness;
    public float _coronaBrightness
    {
        get
        {
            return coronaBrightness;
        }
        set
        {
            if (coronaBrightness == value&&!forceUpdate) return;
            coronaBrightness = value;
            coronaMateral.SetColor("_Color", color * coronaBrightness);
            var main = coronaParticlesComp.main;
            main.startColor = color * coronaBrightness/3;
            if(flare)
            flareMaterial.SetColor("_Color", color * coronaBrightness);
            if(jets)
                _jetMaterial.SetColor("_Color", color * coronaBrightness / jetDensity * 1f);
        }
    }
    [SerializeField]
    float coronaSize;
    public float _coronaSize
    {
        get
        {
            return coronaSize;
        }
        set
        {
            if (coronaSize == value&&!forceUpdate) return;
            coronaSize = value;
            coronaParent.transform.localScale = Vector3.one * coronaSize;
        }
    }
    [SerializeField]
    float coronaWaves;
    public float _coronaWaves
    {
        get
        {
            return coronaWaves;
        }
        set
        {
            if (coronaWaves == value&&!forceUpdate) return;
            coronaWaves = value;
            coronaMateral.SetFloat("_Waves", coronaWaves);
        }
    }
    [SerializeField]
    float coronaRipple;
    public float _coronaRipple
    {
        get
        {
            return coronaRipple;
        }
        set
        {
            if (coronaRipple == value&&!forceUpdate) return;
            coronaRipple = value;
            coronaMateral.SetFloat("_Ripple", coronaRipple);
        }
    }
    [SerializeField]
    float coronaSpeed;
    public float _coronaSpeed
    {
        get
        {
            return coronaSpeed;
        }
        set
        {
            if (coronaSpeed == value&&!forceUpdate) return;
            coronaSpeed = value;
            coronaMateral.SetFloat("_Speed", coronaSpeed);
            var main = coronaParticlesComp.main;
            main.simulationSpeed = 1.5f * coronaSpeed;
        }
    }
    


    public float prominenceAmount;
    public float prominenceSize;


    public GameObject coronaParticlesPrefab;
    public ParticleSystem coronaParticlesComp;

    public GameObject prominence;

//    private AccretionDisk accretionComp;

    CoronaSegment[] coronaSegments;


    Transform coronaParent;
    Transform prominenceParent;

    Renderer starRend;

    //cam = GetComponent<Camera>();


    void CreateJets()
    {

        GameObject parent;
        if (transform.Find("Jets") != null)
        {
            DestroyImmediate(transform.Find("Jets").gameObject);
        }

        parent = new GameObject("Jets");
        parent.transform.parent = transform;
        parent.transform.localPosition = Vector3.zero;
        parent.transform.localScale = Vector3.one;

        if (_jetMaterial != null)
        {
            DestroyImmediate(_jetMaterial);
        }
        if (jetShader == null) jetShader = Shader.Find("Star/Jet");
        _jetMaterial = new Material(jetShader);
        _jetMaterial.SetFloat("_Speed", _jetSpeed);
        _jetMaterial.SetColor("_Color", color*coronaBrightness/ jetDensity * 1f);
        _jetMaterial.SetTextureScale("_Main", jetMatScale);
        GameObject jetTemp;
        for (int i = 0; i < _jetDensity; i++)
        {
            jetTemp = GameObject.CreatePrimitive(PrimitiveType.Quad);
            jetTemp.transform.parent = parent.transform;
            jetTemp.transform.position = Vector3.zero;
            jetTemp.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            jetTemp.GetComponent<Renderer>().sharedMaterial = _jetMaterial;
            jetTemp.transform.localPosition = Vector3.zero;
            jetTemp.transform.Rotate(Vector3.up, i * 360/_jetDensity,Space.Self);
            jetTemp.transform.Rotate(Vector3.right * Random.value * _jetSpread * 9,Space.Self);
            jetTemp.transform.localScale = new Vector3(jetWidth, jetHeight, 1);
        }
    }

    Material CreateFlare()
    {
        Transform flareT = transform.Find("Flare");
        if (flareT == null)
        {
            flareObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
            flareObj.name = "Flare";
            flareObj.transform.parent = transform;
            flareObj.transform.localPosition = Vector3.zero;
        }
        //                if (flareMaterial != null) DestroyImmediate(flareMaterial);

        if (flareMaterial == null)
        {
            flareMaterial = new Material(flareShader);
            flareMaterial.SetColor("_Color", color * coronaBrightness);
            flareMaterial.SetFloat("_Contrast", flareContrast);
        }
        flareObj.transform.localScale = Vector3.one * (1 + flareSize * 3);
        return flareObj.GetComponent<Renderer>().sharedMaterial = flareMaterial;
    }

    void OnEnable()

    {

        
        if (flare) CreateFlare();
        if (transform.Find("Corona") == null)
        {
            GameObject corona = new GameObject("Corona");
            corona.transform.parent = transform;
            corona.transform.localPosition = Vector3.zero;
        }
        coronaParent = gameObject.transform.Find("Corona");
        if (transform.Find("Prominence") == null)
        {
            GameObject prominence = new GameObject("Prominence");
            prominence.transform.parent = transform;
            prominence.transform.localPosition = Vector3.zero;
        }
        prominenceParent = transform.Find("Prominence");
        starRend = gameObject.GetComponent<Renderer>();
        GameObject coronaPartObj;
        if (transform.Find("ParticleCorona") == null)
        {
            coronaPartObj = Instantiate<GameObject>(coronaParticlesPrefab);
            coronaPartObj.name = "ParticleCorona";
            coronaPartObj.transform.parent = transform;
            coronaPartObj.transform.localPosition = Vector3.zero;
        }
        else
        coronaPartObj = transform.Find("ParticleCorona").gameObject;
        coronaParticlesComp = coronaPartObj.GetComponent<ParticleSystem>();
    }

    private void OnDisable()
    {
        if (transform.Find("Prominence") != null&& Application.isPlaying)
        Destroy(transform.Find("Prominence").gameObject);
    }
    private void OnDestroy()
    {
        DestroyImmediate(flareMaterial);
       // DestroyImmediate(accretionMaterial);
        DestroyImmediate(_jetMaterial);
    }
    /// <summary>
    /// Makes instances of materials at runtime. Use this before setting up values after instancig prefab
    /// </summary>
    public void MakeUniqueRunTime()
    {
        _starMaterial = new Material(_starMaterial);
        _coronaMateral = new Material(_coronaMateral);
        if (flare) flareMaterial = new Material(flareMaterial);
    }
    private void Update()
    {
        if (coronaMateral != null)
        {
            Vector3 up = transform.up;
            coronaMateral.SetVector("_ParentRotation", new Vector4(up.x, up.y, up.z, 0));
        }
        if (_jetMaterial != null) _jetMaterial.SetVector("_ParentRotation", transform.right);
    }

 


   public void OnBecameVisible()
    {
        enab = true;
    }

   public void OnBecameInvisible()
    {
        enab = false;
    }


    //Renderer rend = gameObject.renderer;
    private void FixedUpdate()
    {
        if (enabled == true)
        {
            if (Random.value * 10 < prominenceAmount * coronaSpeed) CreateProminence();
        }
     
      
      //  }
    }
    void CreateProminence()
    {

        Vector3 start = Random.onUnitSphere/2.03f;
        Vector3 end = Vector3.RotateTowards(start, Random.onUnitSphere/2.03f, Random.value*Mathf.PI*prominenceSize/2, 0);
        Vector3 p1 = Vector3.Lerp(start, end, 0.5f);
        int count = Random.Range(1, (int)(prominenceAmount * 16));
        for (int i = 0; i < count; i++)
        {
            GameObject prom = Instantiate(prominence);
            prom.name = "Prominence stream "+i.ToString();
            
            prom.transform.parent = prominenceParent;
            prom.transform.localPosition = Vector3.zero;
            prom.transform.localScale = new Vector3(Vector3.Distance(start, end), 0.0001f, Vector3.Distance(start, end) * prominenceSize * Random.Range(0.5f, 1.5f));
            prom.transform.localPosition = p1;
            prom.transform.localRotation = Quaternion.LookRotation(p1, Vector3.Cross(start - end, Vector3.Cross(start - end, p1)));
            prom.transform.RotateAround(prom.transform.position, prom.transform.right, Random.Range(-60f, 60f));
            StartCoroutine(PromCoroutine(prom));
        }
    }
    IEnumerator PromCoroutine(GameObject prom)
    {
        Material promM = prom.GetComponentInChildren<Renderer>().material;
        promM.SetColor("_Color", color * coronaBrightness);
        promM.SetFloat("_Speed", coronaSpeed);
        promM.SetFloat("_Width", Random.value * prominenceSize);
        promM.SetFloat("_Density", Random.value);
        float t = 0;
        float size = prom.transform.localScale.magnitude*10;
        while (t <= size)
        {
            promM.SetFloat("_T", t/size);
            promM.SetFloat("_Speed", coronaSpeed);
            promM.SetColor("_Color", color*coronaBrightness);
            t += Time.deltaTime * coronaSpeed/2f;
            yield return null;
        }
        Destroy(prom.gameObject);
    }

    Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * p0 +
            2f * oneMinusT * t * p1 +
            t * t * p2;
    }
    void GenerateCorona()
    {
        DeleteCorona();
        if (!coronaSprites) return;
        float pointsMinDistance = 15;
        int n = coronaDensity;
        if (simpleCoronaSprites) n = coronaDensity/10;

        coronaSegments = new CoronaSegment[n];
        for(int i=0; i < n; i++)
        {
            Vector3 point = Random.onUnitSphere;
            bool isNear = false;
            for(int j = 0; j < i; j++)
            {
                float angle = Vector3.Angle(point, coronaSegments[j].point);
                if (angle<pointsMinDistance||angle>(180-pointsMinDistance))
                {
                    isNear = true;
                    break;
                }
            }
            if (isNear) { i--; continue; }
            {
                GameObject segment = GameObject.CreatePrimitive(PrimitiveType.Quad);
                DestroyImmediate(segment.GetComponent<MeshCollider>());
                Renderer rend = segment.GetComponent<Renderer>();
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;

                segment.transform.rotation = Quaternion.LookRotation(point,Random.onUnitSphere);
                segment.transform.parent = coronaParent;
                segment.transform.localScale = Vector3.one * 3.158702f;
                segment.transform.localPosition = Vector3.zero;
                coronaSegments[i] = new CoronaSegment();
                coronaSegments[i].rend = segment.GetComponent<Renderer>();
                coronaSegments[i].rend.material = coronaMateral;
                coronaSegments[i].point = point;
                coronaSegments[i].coronaObj = segment;
            }
        }
        coronaParent.localScale = Vector3.one*coronaSize;
    }
    void DeleteCorona()
    {
        if (transform.Find("Corona") != null)
            DestroyImmediate(transform.Find("Corona").gameObject,true);
        coronaParent = new GameObject("Corona").transform;
        coronaParent.transform.parent = gameObject.transform;
        coronaParent.transform.localPosition = Vector3.zero;
    }
    public void ForceUpdate()
    {
        StartCoroutine(ForceUpdateCor());
    }
    IEnumerator ForceUpdateCor()
    {
        forceUpdate = true;
        yield return null;
        forceUpdate = false;
    }
}
struct CoronaSegment
{
    public Vector3 point;
    public GameObject coronaObj;
    public Renderer rend;
}


