using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.VFX;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.VFX.Utility;


public class TextureToParticles : MonoBehaviour
{
	public Light Light;
	public Texture2D LightColor;
	// Start is called before the first frame update
	public int ParticleCount;


//	VisualEffect visualEffect;
	VFXEventAttribute eventAttribute;

	static readonly ExposedProperty positionAttribute = "Position";
	static readonly ExposedProperty enteredTriggerEvent = "EnteredTrigger";

	Color[] _positions;

	//ExposedProperty m_MyProperty;
	VisualEffect m_VFX;

    //public Im



	void Start()
    {

	//	this.VFXInstance = Instantiate(this.VFX);
		// Create texture
		var texture = new Texture2D(ParticleCount, 1, TextureFormat.RFloat, false);

		// Set all of your particle positions in the texture
		_positions = new Color[ParticleCount];


		m_VFX = GetComponent<VisualEffect>();
		m_VFX.SetTexture("ExposedTextureProperty", texture);

		// End do this on every frame
	}

	// Update is called once per frame
	void Update()
    {


		// Begin do this on every frame
		for (int i = 0; i < ParticleCount; i++)
		{
		//	_positions[i] = new Color(myParticle.x, myParticle.y, myParticle.z, 0);
		}

		//texture.SetPixels(_positions);
		//texture.Apply();


	}











	//private IEnumerator Summon()
	//{
	//	float lightStep = 0;
	//	this.summonning = true;
	//	float minClippingLevel = -1;
	//	float maxClippingLevel = 2;
	//	float clippingLevel = maxClippingLevel;
	//	this.Beauty.SetActive(true);
	//	this.VFXInstance = Instantiate(this.VFX);
	//	this.VFXInstance.transform.position = this.Beauty.transform.position;
	//	this.VFXInstance.transform.rotation = this.Beauty.transform.rotation;
	//	this.Light.transform.position = this.Beauty.transform.position + new Vector3(0, 3, 0);
	//	while (clippingLevel > minClippingLevel)
	//	{
	//		this.UpdateSize(this.Beauty);
	//		this.UpdateCachePoint(this.Beauty);
	//		clippingLevel -= Mathf.Abs(maxClippingLevel - minClippingLevel) / 5 * Time.deltaTime;
	//		lightStep = Mathf.Abs(maxClippingLevel - clippingLevel) / Mathf.Abs(maxClippingLevel - minClippingLevel) * 0.5f;
	//		SkinnedMeshRenderer[] renderers = this.Beauty.GetComponentsInChildren<SkinnedMeshRenderer>();
	//		foreach (SkinnedMeshRenderer renderer in renderers)
	//		{
	//			foreach (Material material in renderer.materials)
	//			{
	//				material.SetFloat("_ClippingLevel", clippingLevel);
	//			}

	//		}
	//		this.VFXInstance.GetComponent<VisualEffect>().SetTexture("PointCache", this.pointCache);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetFloat("Size", this.size);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetFloat("ClippingLevel", clippingLevel - 0.5f);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetBool("Emit", true);
	//		this.Light.color = LightColor.GetPixel((int)(lightStep * LightColor.width), (int)(0.5f * LightColor.width));
	//		yield return 0;
	//	}
	//	this.Beauty.SetActive(false);
	//	yield return new WaitForSeconds(3);
	//	minClippingLevel = -1;
	//	maxClippingLevel = 3;
	//	this.Beast.SetActive(true);
	//	while (clippingLevel < maxClippingLevel)
	//	{
	//		this.UpdateSize(this.Beast);
	//		this.UpdateCachePoint(this.Beast);
	//		clippingLevel += Mathf.Abs(maxClippingLevel - minClippingLevel) / 10 * Time.deltaTime;
	//		lightStep = (1 - Mathf.Abs(maxClippingLevel - clippingLevel) / Mathf.Abs(maxClippingLevel - minClippingLevel)) * 0.5f + 0.5f;
	//		SkinnedMeshRenderer[] renderers = this.Beast.GetComponentsInChildren<SkinnedMeshRenderer>();
	//		foreach (SkinnedMeshRenderer renderer in renderers)
	//		{
	//			foreach (Material material in renderer.materials)
	//			{
	//				material.SetFloat("_ClippingLevel", clippingLevel);
	//			}
	//		}
	//		this.VFXInstance.GetComponent<VisualEffect>().SetTexture("PointCache", this.pointCache);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetFloat("Size", this.size);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetFloat("ClippingLevel", clippingLevel);
	//		this.VFXInstance.GetComponent<VisualEffect>().SetBool("Emit", false);
	//		this.Light.color = LightColor.GetPixel((int)(lightStep * LightColor.width), (int)(0.5f * LightColor.width));
	//		yield return 0;
	//	}
	//	yield return new WaitForSeconds(1);
	//	this.summonning = false;
	//}



}
