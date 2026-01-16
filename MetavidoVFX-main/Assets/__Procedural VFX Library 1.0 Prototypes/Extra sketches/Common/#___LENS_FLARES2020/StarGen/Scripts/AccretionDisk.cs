using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[ExecuteInEditMode]
public class AccretionDisk : MonoBehaviour {

    public Material material;
    [SerializeField]
    private GameObject segment;
    [SerializeField]
    private float _size;
    public float size
    {
        get { return _size; }
        set
        {
            _size = value;
            transform.localScale = Vector3.one * _size;
        }
    }
    [SerializeField]
    private int _segments;
    public int segments
    {
        get { return _segments; }
        set
        {
            _segments = value;
            color = color;
            GenerateSegments();
        }
    }
    [SerializeField]
    private float _innerRadius;
    public float innerRadius
    {
        get { return _innerRadius; }
        set
        {
            _innerRadius = value;
            material.SetFloat("_InnerRadius", _innerRadius);
        }
    }
    [SerializeField]
    private float _border=1;
    public float border
    {
        get { return _border; }
        set
        {
            _border = value;
            material.SetFloat("_Border",_border);
        }
    }
    [SerializeField]
    private float _twist;
    public float twist
    {
        get { return _twist; }
        set
        {
            _twist = value;
            material.SetFloat("_Twist", _twist);
        }
    }
    [SerializeField]
    private float _speed;
    public float speed
    {
        get { return _speed; }
        set
        {
            _speed = value;
            material.SetFloat("_Speed", _speed);
        }
    }
    [SerializeField]
    private Color _color;
    public Color color
    {
        get { return _color; }
        set
        {
            _color = value;
            _color.a = 1.0f / segments * 15;
            material.SetColor("_Color", _color);
        }
    }
    [SerializeField]
    private Color _color2;
    public Color color2
    {
        get { return _color2; }
        set
        {
            _color2 = value;
            material.SetColor("_Color2", _color2);
        }
    }

    private void Update()
    {
        material.SetVector("_ParentRotation", transform.forward);
    }
    public void GenerateSegments()
    {
        if (segments <= 0) return;
        DeleteSegments();
        for (int i = 0; i < segments; i++)
        {
            GameObject tempSeg = GameObject.CreatePrimitive(PrimitiveType.Quad);
            tempSeg.transform.parent = transform;
            tempSeg.transform.Rotate(Vector3.right, 90);
            tempSeg.transform.localScale = Vector3.one * 70;
            tempSeg.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tempSeg.GetComponent<Renderer>().sharedMaterial = material;
            tempSeg.transform.Rotate(Vector3.forward, (float)i / segments * 360, Space.Self);
            tempSeg.transform.Rotate(Vector3.right, Random.value * 5, Space.Self);
        }
    }
    public void ForceUpdate()
    {
        material.SetFloat("_InnerRadius", innerRadius);
        material.SetFloat("_Twist", twist);
        material.SetFloat("_Speed", speed);
        material.SetColor("_Color", color);
        material.SetColor("_Color2", color2);
        transform.localScale = Vector3.one * _size;
        GenerateSegments();
    }
    void DeleteSegments()
    {
        while(transform.childCount>0)
        {
            DestroyImmediate(transform.GetChild(0).gameObject);
        }
        
    }
}
