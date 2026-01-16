using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Parent class responsible for extracting beats from..
/// ..spectrum value given by AudioSpectrum.cs
/// </summary>
/// 
public class AudioSyncer : MonoBehaviour {

	public GameObject tapper;
	/// <summary>
	/// Inherit this to cause some behavior on each beat
	/// </summary>
	///
	float lastTime = 0;
	public float speed = 0;
	public float avspeed = 0;

	private float timer = 0.0f;

	public float finalavg=0.0f;
	float lastavg = 0f;

	public float multfinalavg = 0.0f;

	public int randomtargetint = 0;
	public int maxtargets =1;

	public bool randomtarget=false;

	public virtual void OnBeat()
	{

		if (tapper != null)
		{
			tapper.GetComponent<TapToMoveInfiniteZoom>().beat1 = true;
			speed = timer - lastTime;
			lastTime = timer;

			//speed = Time.time - lastTime;
			//lastTime = Time.time;

			avspeed = (avspeed + speed) / 2f;

			timer += Time.deltaTime;
			finalavg = Mathf.Lerp(lastavg, avspeed, 0.05f);
			lastavg = finalavg;
			multfinalavg = finalavg * 2000;

			tapper.GetComponent<TapToMoveInfiniteZoom>().autorotatespeedx = multfinalavg/300f;

			if (randomtarget == true)
			{
				randomtargetint = Random.Range(1, maxtargets);
			}
            else
            {
				randomtargetint = 4;

			}
			tapper.GetComponent<TapToMoveInfiniteZoom>().randomtargetint = randomtargetint;


		//	tapper.GetComponent<TapToMoveInfiniteZoom>().beat1 = false;

		}
		//		Debug.Log("beat");
		m_timer = 0;
		m_isBeat = true;
	}

	/// <summary>
	/// Inherit this to do whatever you want in Unity's update function
	/// Typically, this is used to arrive at some rest state..
	/// ..defined by the child class
	/// </summary>
	public virtual void OnUpdate()
	{ 
		// update audio value
		m_previousAudioValue = m_audioValue;
		m_audioValue = AudioSpectrum.spectrumValue;

		// if audio value went below the bias during this frame
		if (m_previousAudioValue > bias &&
			m_audioValue <= bias)
		{
			// if minimum beat interval is reached
			if (m_timer > timeStep)
			{
				//	OnBeat();
			}
		}

		// if audio value went above the bias during this frame
		if (m_previousAudioValue <= bias &&
			m_audioValue > bias)
		{
			// if minimum beat interval is reached
			if (m_timer > timeStep)
				OnBeat();
		}

		m_timer += Time.deltaTime;
	}

	private void Update()
	{
		OnUpdate();
		//timer += Time.deltaTime;
	}

	public float bias;
	public float timeStep;
	public float timeToBeat;
	public float restSmoothTime;

	private float m_previousAudioValue;
	private float m_audioValue;
	private float m_timer;

	protected bool m_isBeat;
}
