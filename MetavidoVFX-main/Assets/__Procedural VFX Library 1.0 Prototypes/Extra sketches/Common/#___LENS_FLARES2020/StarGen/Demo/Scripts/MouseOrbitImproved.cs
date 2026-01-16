using UnityEngine;

using System.Collections;



public class MouseOrbitImproved : MonoBehaviour
{


    public Transform target;
    public float distance = 3f;
    //    public int cameraSpeed= 5 ;

    public float xSpeed = 175.0f;
    public float ySpeed = 75.0f;

    private float lastDist = 0;
    //    private float curDist = 0;

    public int yMinLimit = 10; //Lowest vertical angle in respect with the target.
    public int yMaxLimit = 10;

    private float x = 0.0f;
    private float y = 0.0f;

    private float X = 0f;
    private float Y = 0f;

    bool closeUp = false;

    public Transform closeUpTransorm;
    Vector3 originalPos;
    Quaternion originalRot;

    private Camera cam;
    void Start()
    {
        cam = Camera.main;

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;



        // Make the rigid body not change rotation

        if (GetComponent<Rigidbody>())
            GetComponent<Rigidbody>().freezeRotation = true;

    }

    public void OnSwitchAngle()
    {
        if (!closeUp)
            StartCoroutine(CloseUp());
            else StartCoroutine(Panorama());
    }
    IEnumerator CloseUp()
    {
        closeUp = true;
        originalPos = transform.position;
        originalRot = transform.rotation;
            float t = 0;
        while (t < 1)
        {
            transform.position = GetPoint(transform.position,transform.position+transform.right, closeUpTransorm.position, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, closeUpTransorm.rotation, t);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = closeUpTransorm.position;
        transform.rotation = closeUpTransorm.rotation;

    }
    IEnumerator Panorama()
    {
        float t = 0;
        while (t < 1)
        {
            transform.position = GetPoint(transform.position,transform.position+transform.right, originalPos, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, originalRot, t);
            t += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
        transform.rotation = originalRot;
        closeUp = false;
    }

    void Update()
    {
        if (closeUp) return;
        distance -= Input.GetAxis("Mouse ScrollWheel")*0.5f;
        distance = Mathf.Clamp(distance, 0.55f, 10);
        {



            //Detect mouse drag;

            if (Input.GetMouseButton(1))
            {

                x += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
                y -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;

            }
                y = ClampAngle(y, yMinLimit, yMaxLimit);

                Quaternion rotation = Quaternion.Euler(y, x, 0);
                Vector3 vTemp = new Vector3(0.0f, 0.0f, -distance);
                Vector3 position = rotation * vTemp + target.position;


                transform.position = Vector3.Lerp(transform.position, position, xSpeed * Time.deltaTime + 2);
                transform.rotation = rotation;
 



        }


    }
    public static Vector3 GetPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        t = Mathf.Clamp01(t);
        float oneMinusT = 1f - t;
        return
            oneMinusT * oneMinusT * p0 +
            2f * oneMinusT * t * p1 +
            t * t * p2;
    }


    static float ClampAngle(float angle, float min, float max)
    {

        if (angle < -360)
            angle += 360;

        if (angle > 360)
            angle -= 360;

        return Mathf.Clamp(angle, min, max);

    }



}