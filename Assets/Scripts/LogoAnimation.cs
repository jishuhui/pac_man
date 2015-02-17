using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class LogoAnimation : MonoBehaviour {

	Outline outline;
	float t;
	public float speed;
	public float maxValue;
	float lastTime = 0;


	// Use this for initialization
	void Start () {
		outline = this.GetComponent<Outline> (); 
	}
	
	// Update is called once per frame
	void Update () {
		//Calculate our own deltatime, so we could animate stuff even with timeScale=0
		float delta = Time.realtimeSinceStartup - lastTime;
		//t += Time.deltaTime*speed;
		t += delta*speed;
		float x = Mathf.Sin (t)*maxValue;
		float y = Mathf.Cos (t)*maxValue;
		outline.effectDistance = new Vector2(x,y);
		lastTime = Time.realtimeSinceStartup;
	}

}
