// Converted from UnityScript to C# at http://www.M2H.nl/files/js_to_c.php - by Mike Hergaarden
// Do test the code! You usually need to change a few small bits.

using UnityEngine;
using System.Collections;

public class Keyboardorbit : MonoBehaviour {

public	Transform target;

	public float distanceMin= -99999999999999999.0f;
	public float distanceMax= 99999999999999999.0f;
public float distanceInitial= 12.5f;
public float scrollSpeed= 1.0f;

	public float xSpeed= 80.0f;
	public float ySpeed= 80.0f;

	public int yMinLimit= -999;
	public int yMaxLimit= 999;
    public float rotatespeedx = 0.0000003f;

    public float rotatespeedy = 0.0000003f;


 float x= 0.0f;
 float y= 0.0f;
private float distanceCurrent= 0.0f;

//@script AddComponentMenu ("Camera-Control/Key Mouse Orbit")

void  Start (){

		//string filePath = System.IO.Path.Combine(Application.streamingAssetsPath, "novation/Novation-Launchpad.maxpat");
		//System.Diagnostics.Process.Start(filePath);
    Vector3 angles= transform.eulerAngles;
    x = angles.y;
    y = angles.x;

	distanceCurrent = distanceInitial;

	// Make the rigid body not change rotation
		if (GetComponent<Rigidbody>()){

		}
	//	GetComponent.<Rigidbody>().freezeRotation = true;
}

void  LateUpdate (){
    if (target) {
            x += Input.GetAxis("Horizontal") * xSpeed * 0.02f+rotatespeedx;
            y -= Input.GetAxis("Vertical") * ySpeed * 0.02f+ rotatespeedy;
			distanceCurrent -= Input.GetAxis("Mouse ScrollWheel") * ((distanceCurrent/100)*scrollSpeed);

		distanceCurrent = Mathf.Clamp(distanceCurrent, distanceMin, distanceMax);
 		y = ClampAngle(y, yMinLimit, yMaxLimit);

        Quaternion rotation= Quaternion.Euler(y, x, 0);
		Vector3 position= rotation * new Vector3(0.0f, 0.0f, -distanceCurrent) + target.position;

        transform.rotation = rotation;
        transform.position = position;
    }
}
//
	public static float  ClampAngle ( float angle ,   float min ,   float max  ){
		if (angle < -360) {
			angle += 360;
		}
		if (angle > 360) {
			angle -= 360;
		}
		var moo = Mathf.Clamp (angle, min, max);
		return moo;
}
}