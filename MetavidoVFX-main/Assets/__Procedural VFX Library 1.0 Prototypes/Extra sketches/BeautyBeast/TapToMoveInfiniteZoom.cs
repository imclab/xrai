using UnityEngine;
//using UnityEngine.Rendering.PostProcessing;

public class TapToMoveInfiniteZoom : MonoBehaviour
{


    private Vector3 cluster1 = new Vector3(0, 2.5f, 15.8f);
    private Vector3 cluster2 = new Vector3(-1, -11, 3);
    private Vector3 cluster3 = new Vector3(2692, 81, 2526);
    private Vector3 cluster4 = new Vector3(531, 2317, 3776);
    private Vector3 cluster5 = new Vector3(-587, 2043, 2194);

    public GameObject[] waypoints;
    //flag to check if the user has tapped / clicked.
    //Set to true on click. Reset to false on reaching destination
    private bool flag = false;
    //destination point
    public Vector3 endPoint;
    Vector3 position;
    Vector3 oldpos;

    //public    Transform target;

    //alter this to change the speed of the movement of player / gameobject
    public float duration = 50.0f;
    //vertical position of the gameobject
    private float yAxis;


    public DepthOfFieldEffect camdof;
    public Camera cam;


    public Transform target;


    public float distanceMin = -99999999999999999.0f;
    public float distanceMax = 99999999999999999.0f;
    public float distanceInitial = 12.5f;
    public float scrollSpeed = 1.0f;

    public float xSpeed = 80.0f;
    public float ySpeed = 80.0f;

    public int yMinLimit = -999;
    public int yMaxLimit = 999;

    public float rotationspeed = 0.1F;

    public float autorotatespeedx = 0.0000003f;

    public float autorotatespeedy = 0.0000003f;

    //public ImageEffectAfterScale;

    float x = 0.0f;
    float y = 0.0f;
    private float distanceCurrent = 0.0f;

    float curZoomPos, zoomTo; // curZoomPos will be the value
    float zoomFrom = 20f; //Midway point between nearest and farthest zoom values (a "starting position")


    private float initHeightAtDist;
    //private bool dzEnabled;

    //// Calculate the frustum height at a given distance from the camera.
    //float FrustumHeightAtDistance(float distance)
    //{
    //    return 9.0f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    //}

    //// Calculate the FOV needed to get a given frustum height at a given distance.
    //float FOVForHeightAndDistance(float height, float distance)
    //{
    //    return 9.0f * Mathf.Atan(height * 0.5f / distance) * Mathf.Rad2Deg;
    //}

    //// Start the dolly zoom effect.
    //void StartDZ()
    //{
    //    var distance = Vector3.Distance(transform.position, target.position+new Vector3(0.0f,0.0f,0.0f));
    //    initHeightAtDist = FrustumHeightAtDistance(distance);
    //    dzEnabled = true;
    //}

    //// Turn dolly zoom off.
    //void StopDZ()
    //{
    //    dzEnabled = false;
    //}

    Quaternion oldrot;
    Vector3 olddist;
    Quaternion rotation;
    float oldx;
    float oldy;

    // Quaternion ranrot;
    float ranrotx;
    public float ranrotxmax=360f;
    public float ranrotxmin=-360f;


    float ranroty;
    public float ranrotymax = 360f;
    public float ranrotymin = -360f;

    public bool beat1 = false;



    public int randomtargetint = 0;

    public int everyNthbeat = 1;
    bool gobeat = false;

    GameObject[] targetgameObjects;



    int counter;
    int ran1;
    float ran2;

    void Start()
    {
       // StartDZ();

        //save the y axis value of gameobject
        yAxis = gameObject.transform.position.y;

        Vector3 angles = transform.eulerAngles;
        x = angles.y;
        y = angles.x;

        distanceCurrent = distanceInitial;





//        GameObject[] targetgameObjects;
        targetgameObjects = GameObject.FindGameObjectsWithTag("camtarget");

        //if (targetgameObjects.Length == 0)
        //{
        //    Debug.Log("No game objects are tagged with 'Enemy'");
        //}
    }

    //declare a variable of RaycastHit struct
    RaycastHit hit;
    //Create a Ray on the tapped / clicked position
    Ray ray;


    // Update is called once per frame
    void Update()
    {

        //check if the screen is touched / clicked
        if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) || (Input.GetMouseButtonDown(0)))
        {
   
            //for unity editor
#if UNITY_EDITOR || UNITY_STANDALONE
            ray = cam.ScreenPointToRay(Input.mousePosition);
            //for touch device
#elif (UNITY_ANDROID || UNITY_IPHONE || UNITY_WP8)
   ray = cam.ScreenPointToRay(Input.GetTouch(0).position);
#endif

            //Check if the ray hits any collider
            if (Physics.Raycast(ray, out hit))
            {
                //set a flag to indicate to move the gameobject
                flag = true;
                //save the click / tap position
                endPoint = hit.point;
                target = hit.transform;
                //as we do not want to change the y axis value based on touch position, reset it to original y axis value
                // endPoint.y = yAxis;
                // Debug.Log(endPoint);
            }

        }


        ////check if the flag for movement is true and the current gameobject position is not same as the clicked / tapped position
        //if (flag && !Mathf.Approximately(gameObject.transform.position.magnitude, endPoint.magnitude))
        //{ //&& !(V3Equal(transform.position, endPoint))){
        //  //move the gameobject to the desired position
        //  //   gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, endPoint, 1/(duration*(Vector3.Distance(gameObject.transform.position, endPoint))));

        //    float ziz = endPoint.z;
        //    //  endPoint.z = endPoint.z - 3.0f;

        //    gameObject.transform.position = Vector3.Lerp(gameObject.transform.position, endPoint, 1 / (duration * (1)));
        //    //ziz = ziz + 10.0f;
        //    //gameObject.transform.position = new Vector3 (gameObject.transform.position.x, gameObject.transform.position.y, ziz);
        //    //gameObject.transform.position.z = ;
        //}
        ////set the movement indicator flag to false if the endPoint and current gameobject position are equal
        //else if (flag && Mathf.Approximately(gameObject.transform.position.magnitude, endPoint.magnitude))
        //{
        //    flag = false;
        //    //Debug.Log("I am here");
        //}












        if (target)
        {

            var xSpeedorig = xSpeed;
            var ySpeedorig = ySpeed;

            //if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            //{
            //    x = x * 2;
            //    y = y * 2;
            //    xSpeed = xSpeedorig * 2;
            //    ySpeed = ySpeedorig * 2;

            //}
            //else{

            //    xSpeed = xSpeedorig;
            //    ySpeed = ySpeedorig;

            //}



            //  x += Input.GetAxis("Horizontal") * xSpeed * 0.02f+ autorotatespeedx;
            // y -= Input.GetAxis("Vertical") * ySpeed * 0.02f+ autorotatespeedy;
          

       


            //if(Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift)){
            //    x = x * 2;
            //    y = y * 2;

            //}
            //          distanceCurrent -= Input.GetAxis("Mouse ScrollWheel") * ((distanceCurrent/100)*scrollSpeed);
            //
            //          distanceCurrent = Mathf.Clamp(distanceCurrent, distanceMin, distanceMax);
            //          y = ClampAngle(y, yMinLimit, yMaxLimit);

            distanceCurrent -= Input.GetAxis("Mouse ScrollWheel") * ((distanceCurrent / 100) * scrollSpeed);

            distanceCurrent = Mathf.Clamp(distanceCurrent, distanceMin, distanceMax);


            // y = ClampAngle(y, yMinLimit, yMaxLimit);




            //position =  target.position;

            //Quaternion rotation= Quaternion.Euler(y, x, 0);


            //Quaternion oldrot = transform.rotation;
            // oldpos = transform.position;
            //Vector3 olddist = new Vector3(distanceCurrent, 0f, 0f);


            // Vector3 olddist
            //Vector3 position= rotation * new Vector3(0.0f, 0.0f, -distanceCurrent) + target.position;

            //if (beat1 == false)
            //{
                x = Mathf.Lerp(oldx, x, 10.01f);
                y = Mathf.Lerp(oldy, y, 10.01f);
                x += Input.GetAxis("Horizontal") * xSpeed * 0.02f + 0;
                y -= Input.GetAxis("Vertical") * ySpeed * 0.02f + 0;

                oldx = Input.GetAxis("Horizontal") * xSpeed * 0.02f + autorotatespeedx;
                oldy = Input.GetAxis("Vertical") * ySpeed * 0.02f + autorotatespeedy;

                rotation = Quaternion.Euler(y, x, 0);
                position = rotation * new Vector3(target.position.x, target.position.y, -distanceCurrent) + target.position;
                transform.rotation = Quaternion.Lerp(oldrot, rotation, 1 / rotationspeed);
                transform.position = Vector3.Lerp(oldpos, position, 1 / (duration * (1)));
                oldrot = transform.rotation;
                oldpos = transform.position;
                olddist = new Vector3(distanceCurrent, 0f, 0f);
          //  }


            //x = Mathf.Lerp(oldx, x, 10.01f);
            //y = Mathf.Lerp(oldy, y, 10.01f);

            //x += Input.GetAxis("Horizontal") * xSpeed * 0.02f + autorotatespeedx;
            //y -= Input.GetAxis("Vertical") * ySpeed * 0.02f + autorotatespeedy;
     




            //  Vector3 distLerp = Vector3.Lerp(olddist, new Vector3(distanceCurrent,0f,0f), 1 / (duration * (1)));


            //if (dzEnabled)
            //{
            //    // Measure the new distance and readjust the FOV accordingly.
            //    // var currDistance = Vector3.Distance(transform.position, target.position);
            //    //cam.fieldOfView = FOVForHeightAndDistance(initHeightAtDist, (distLerp.x / 30));




            //    //cam.fieldOfView = (distLerp.x / 30f)+50f;









            //    //DepthOfField camdof2 = cam.GetComponent<DepthOfField>();
            //    // camdof.
            //    //  float distnew = rotation.z*-distanceCurrent + position.z;
            //    //  camdof.focusDistance = (distnew*-1.0f)+10f;
            //    //camdof.focusDistance = distanceCurrent;
            //    // camdof.focusDistance = ((transform.position.z-endPoint.z)*-1.3f)-0.0f;
            //    float aValue = ((transform.position.z - endPoint.z) * -1.3f) - 0.0f;
            //    float aLow = 0.1f;
            //    float aHigh = 50f;
            //    float bLow = 0.1f;
            //    float bHigh = 30f;

            //    float normal = Mathf.InverseLerp(aLow, aHigh, aValue);
            //    float bValue = Mathf.Lerp(bLow, bHigh, normal);

            //    //                camdof.focusDistance = bValue;

            //}


            //Attaches the float y to scrollwheel up or down


            //// float zoom = Input.mouseScrollDelta.y;
            //float zoom = distanceCurrent;
            // // If the wheel goes up it, decrement 5 from "zoomTo"
            // //if (zoom >= 1)
            // //{
            // ////    zoomTo -= 5f;
            // // //   Debug.Log("Zoomed In");
            // //}

            // //// If the wheel goes down, increment 5 to "zoomTo"
            // //else if (zoom >= -1)
            // //{
            // // //   zoomTo += 5f;
            // //  //  Debug.Log("Zoomed Out");
            // //}

            // // creates a value to raise and lower the camera's field of view
            //// curZoomPos = zoomFrom + zoomTo;

            // curZoomPos = Mathf.Clamp(distanceCurrent, 5f, 35f);

            // // Stops "zoomTo" value at the nearest and farthest zoom value you desire
            // zoomTo = Mathf.Clamp(zoomTo, -15f, 30f);

            // // Makes the actual change to Field Of View
            // Camera.main.fieldOfView = curZoomPos;



        }


        if (Input.GetKeyUp("2")) {

            beat1 = true;
        }




        if (beat1 == true)
        {
            counter++;
            Debug.Log(counter);
            if (counter % everyNthbeat == 0)
            {

                gobeat = true;
                //ran1 = Random.Range(1, 2);
                //ran2 = Random.Range(-1f, 1f);

                //if (ran1 == 1)
                //{

                //    //   ko.rotatespeedx = BrownianBounceVal * BrownianBounceMult * throttle * ran2;
                //}

                //if (ran1 == 2)
                //{
                //    //    ko.rotatespeedy = BrownianBounceVal * BrownianBounceMult * throttle * ran2;
                //}

                counter = 1;
            }
            else
            {
                gobeat=false;
            }

        }



        if ((Input.GetKey("1")||randomtargetint==1) && beat1 == true)
        {
            //	transform.position = cluster1;
            target = waypoints[0].transform;
            //  transform.position = Vector3.Lerp(position, cluster1, 1 / (duration * (100)));

            ranrotx = Random.Range(ranrotxmin, ranrotxmax);
            ranroty = Random.Range(ranrotymin, ranrotymax);

            //x = transform.rotation.x + ranrotx;
            //y = transform.rotation.y + ranroty;

            x = transform.rotation.x + ranrotx;
            y = transform.rotation.y + ranroty;

            beat1 = false;
            randomtargetint = 0;


        }

        if ((Input.GetKey("2") || randomtargetint == 2) && beat1 == true)
        {

            if (targetgameObjects.Length != 0)
            {
//                Debug.Log("No game objects are tagged with 'Enemy'");

                target = targetgameObjects[Random.Range(0, targetgameObjects.Length)].transform;

                //rotation = Quaternion.Euler(y, x, 0);
                //position = rotation * new Vector3(target.position.x, target.position.y, -distanceCurrent) + target.position;
                //transform.rotation = Quaternion.Lerp(oldrot, rotation, 1 / rotationspeed);
                //transform.position = Vector3.Lerp(oldpos, position, 1 / (duration * (1)));
                //oldrot = transform.rotation;
                //oldpos = transform.position;
                //olddist = new Vector3(distanceCurrent, 0f, 0f);
            }
            else
            {
                //            target = gameObject [0].transform;
                target = waypoints[1].transform;
            }
                //transform.position = cluster2;
            // transform.position = Vector3.Lerp(position, cluster2, 1 / (duration * (100)));
            ranrotx = Random.Range(ranrotxmin, ranrotxmax);
            ranroty = Random.Range(ranrotymin, ranrotymax);

            x = transform.rotation.x+ranrotx;
            y = transform.rotation.y+ranroty;

            beat1 = false;
        }

        if ((Input.GetKey("3") || randomtargetint == 3) && beat1 == true)
        {
            target = waypoints[2].transform;
            //transform.position = cluster3;
            // transform.position = Vector3.Lerp(oldpos, cluster3, 1 / (duration * (1)));
            ranrotx = Random.Range(ranrotxmin, ranrotxmax);
            ranroty = Random.Range(ranrotymin, ranrotymax);

            x = ranrotx;
            y = ranroty;
            beat1 = false;
        }

        if ((Input.GetKey("4") || randomtargetint == 4) && beat1 == true)
        {

            if (gobeat == true)
            {
                target = waypoints[3].transform;
                //transform.position = cluster4;
                // transform.position = Vector3.Lerp(oldpos, cluster4, 1 / (duration * (1)));
                ranrotx = Random.Range(ranrotxmin, ranrotxmax);
                ranroty = Random.Range(ranrotymin, ranrotymax);

                x += ranrotx;
                y += ranroty;
            }
            beat1 = false;
        }

        if (Input.GetKey("5") || randomtargetint == 5)
        {
            target = waypoints[4].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
            ranrotx = Random.Range(ranrotxmin, ranrotxmax);
            ranroty = Random.Range(ranrotymin, ranrotymax);

            x += ranrotx;
            y += ranroty;
         //   beat1 = false;
        }

        if (Input.GetKey("6") || randomtargetint == 6)
        {
            target = waypoints[5].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
         //   beat1 = false;
        }

        if (Input.GetKey("7"))
        {
            target = waypoints[6].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
        }

        if (Input.GetKey("8"))
        {
            target = waypoints[7].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
        }

        if (Input.GetKey("9"))
        {
            target = waypoints[8].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
        }

        if (Input.GetKey("0"))
        {
            target = waypoints[9].transform;
            //transform.position = cluster5;
            // transform.position = Vector3.Lerp(oldpos, cluster5, 1 / (duration * (1)));
        }

        //Quaternion OriginalRot = transform.rotation;
        //transform.LookAt(target);
        //Quaternion NewRot = transform.rotation;
        //transform.rotation = OriginalRot;
        //transform.rotation = Quaternion.Lerp(transform.rotation, NewRot, rotationspeed * Time.deltaTime);


    }




    public static float ClampAngle(float angle, float min, float max)
    {
        if (angle < -360)
        {
            angle += 360;
        }
        if (angle > 360)
        {
            angle -= 360;
        }
        var moo = Mathf.Clamp(angle, min, max);
        return moo;
    }


}
