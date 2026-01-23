using UnityEngine;
using System.Collections;

public class Monument : MonoBehaviour
{

    public bool IsSelected = false;


    void Start()
    {

    }

    void Update()
    {

        //if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        //{

        //    Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
        //    RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        //    //Select Hool
        //    if (hit && hit.collider.transform.name == "Monument")
        //    {
        //        if (!IsSelected)
        //        {
        //            IsSelected = true;
        //        }
        //        else
        //        {
        //            IsSelected = false;
        //        }
        //    }
        //}
    }
}