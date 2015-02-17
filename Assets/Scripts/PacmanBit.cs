using UnityEngine;
using System.Collections;

public class PacmanBit : MonoBehaviour {

	float life = 0;

	// Use this for initialization
	void Start () {
	
	}
	
	// Do the dissolve effect
	void Update () {
		life += Time.deltaTime * 0.7f;
		renderer.material.SetFloat ("_Dissolve", life);
		if (life>1) {
			Destroy (this.gameObject);
		}
		float sc = 1 - life*0.5f;
		this.transform.localScale = new Vector3 (1, sc, sc);
	}
}
