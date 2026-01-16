using UnityEngine;
using System.Collections;

public class Orbit : MonoBehaviour {

	public GameObject cube;
	public Transform center;
	public Vector3 axis = Vector3.up;
	public Vector3 desiredPosition;
	public float radius = 2.0f;
	public float radiusSpeed = 0.5f;
	public float rotationSpeed = 80.0f;
	public GameObject particle;
	public Transform other;
	public GameObject myLine;

	public int xyz=0;

	void Start () {




		cube = GameObject.FindWithTag("Cube");
		center = cube.transform;
		transform.position = (transform.position - center.position).normalized * radius + center.position;
		radius = 2.0f;

		//DrawLine (center.position, transform.position, new Color(0.2F, 0.3F, 0.4F, 0.5F));

	}

	void LateUpdate () {

		if (xyz == 0) {
			axis = Vector3.up;
		}
		if (xyz == 1) {
			axis = Vector3.down;
		}
		if (xyz == 2) {
			axis = Vector3.left;
		}

		if (xyz == 3) {
			axis = Vector3.right;
		}

		if (xyz == 4) {
			axis = Vector3.forward;
		}

		if (xyz == 5) {
			axis = Vector3.back;
		}


		transform.RotateAround (center.position, axis, rotationSpeed * Time.deltaTime);
		desiredPosition = (transform.position - center.position).normalized * radius + center.position;
		transform.position = Vector3.MoveTowards(transform.position, desiredPosition, Time.deltaTime * radiusSpeed);



		if (other) {

            other.transform.position = transform.position;
            //		other.transform.localRotation.x = 90.0f;

            //myLine = new GameObject();
            other.transform.rotation = transform.rotation;

            float dist = Vector3.Distance(center.position, transform.position);
		//	other.transform.localScale.y = 2.0f;
			other.transform.localScale = new Vector3 (0.1f, 1f, 0.1f);
			print("Distance to other: " + dist);
			//Instantiate (particle, transform.position, transform.rotation);
			DrawLine (center.position, transform.position, new Color(0.0F, 0.4F, 2.0F, 0.7F));


		}
	//	 Construct a ray from the current touch coordinates
//		var ray = Camera.main.ScreenPointToRay (center.position);
//		if (Physics.Raycast (ray)) {
			// Create a particle if hit
		//}


	}


	void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 20.1f)
	{

		 myLine = new GameObject();
		myLine.transform.position = start;
		myLine.AddComponent<LineRenderer>();
		LineRenderer lr = myLine.GetComponent<LineRenderer>();
		lr.material = new Material(Shader.Find("Particles/Alpha Blended Premultiply"));
		lr.SetColors(color, color);
		lr.SetWidth(0.07f, 0.07f);
		lr.SetPosition(0, start);
		lr.SetPosition(1, end);
	myLine.transform.rotation = transform.rotation;
		GameObject.Destroy(myLine, duration);

}

}