using UnityEngine;
using System.Collections;

public class MouseFollow : MonoBehaviour
{

    public float Distance = 10;

    public float xoffset = 0.0f;
    public float yoffset = 0.0f;

    public float zoffset = 0.0f;

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        Ray r = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector3 pos = r.GetPoint(Distance);
        pos = new Vector3(pos.x+xoffset, pos.y+ yoffset, pos.z+ zoffset);
        transform.position = pos;
    }
}