using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum ColorChannel {
	r = 0,
	g = 1,
	b = 2,
}

public class EnemyController : MonoBehaviour {
	CharController cc;
	float chaseTime;
	float wanderTime;
	float wanderInterval;
	bool moveTillGoalReach;
	bool isWandering;
	public float startDelay;
	bool dead = false;
	float scale = 1;

	// Use this for initialization
	void Start () {
		cc = this.GetComponent<CharController> ();

		//Randomize the behaviour and the colors of an enemy ghost
		chaseTime = Random.Range (3f, 8f);
		wanderTime = Random.Range (6f, 15f);
		wanderInterval = Random.Range (3f, 6f);

		if (Random.value < 0.5f) {
			moveTillGoalReach = true;
		} else {
			moveTillGoalReach = false;
		}

		if (Random.value < 0.5f) {
			isWandering = true;
		} else {
			isWandering = false;
		}
		isWandering = false;

		Color topColor = GenerateBrightColor ();
		Color botColor = ShiftHUE (topColor, 0.3f);
		this.transform.FindChild ("model").renderer.material.SetColor ("_ColorTop", botColor);
		this.transform.FindChild ("model").renderer.material.SetColor ("_ColorBot", topColor);

		if (isWandering) {
			if (!moveTillGoalReach) {
				InvokeRepeating ("SetRandomPath", startDelay, wanderInterval);
			} else {
				Invoke ("SetRandomPath", startDelay);
			}	
		} else {
			InvokeRepeating("Chase", startDelay, 1f);
		}
		StartCoroutine ("BehavioursLoop");
	}

	public void OnDestination() {
		if (moveTillGoalReach && isWandering) {
			SetRandomPath ();
		}
	}

	void Chase() {
		GameObject pl = GameObject.FindGameObjectWithTag ("player");
		if (pl!=null) {
			int[] pos = pl.GetComponent<CharController>().currentPos;
			Vector2 point = new Vector2(pos[0], pos[1]);
			cc.MoveToPoint (point);
		}
	}

	Color GenerateBrightColor() {
		Color c = new Color ();
		List<ColorChannel> channels = new List<ColorChannel> ();
		channels.Add (ColorChannel.r);
		channels.Add (ColorChannel.g);
		channels.Add (ColorChannel.b);

		//Select random primary color channel (R || G || B)
		int p = Random.Range (0, channels.Count);
		ColorChannel primary = channels [p];
		channels.RemoveAt (p);

		//Select random secondary color channel (R || G || B)
		int s = Random.Range (0, channels.Count);
		ColorChannel secondary = channels [s];

		float val = 1;
		float val2 = Random.value;
		switch(primary) {
		case ColorChannel.r: c.r = val;break;
		case ColorChannel.g: c.g = val;break;
		case ColorChannel.b: c.b = val;break;
		}
		switch(secondary) {
		case ColorChannel.r: c.r = val2;break;
		case ColorChannel.g: c.g = val2;break;
		case ColorChannel.b: c.b = val2;break;
		}

		return c;
	}

	//Shifts the color hue
	//this method is very approximate
	Color ShiftHUE(Color c, float amount) {
		Color r = c;
		float min = c.r;
		ColorChannel darkest = ColorChannel.r;
		if (c.g<min) {
			min = c.g;
			darkest = ColorChannel.g;
		}
		if (c.b<min) {
			min = c.b;
			darkest = ColorChannel.b;
		}
		switch(darkest) {
		case ColorChannel.r: r.g-=amount;r.b-=amount;r.r+=amount*1.5f;break;
		case ColorChannel.g: r.r-=amount;r.b-=amount;r.g+=amount*1.5f;break;
		case ColorChannel.b: r.r-=amount;r.g-=amount;r.b+=amount*1.5f;break;
		}
		return r;
	}

	void SetRandomPath() {
		int spot = Random.Range (0, GameManager.instance.walkableSpots.Count);
		cc.MoveToPoint (GameManager.instance.walkableSpots[spot]);
	}
	
	IEnumerator BehavioursLoop() {
		while(GameManager.instance.gameState==GameState.play) {
			if (isWandering) {
				yield return new WaitForSeconds(wanderTime);
			} else {
				yield return new WaitForSeconds(chaseTime);
			}
			isWandering = !isWandering;
			if (isWandering) {
				CancelInvoke("Chase");
				if (!moveTillGoalReach) {
					InvokeRepeating ("SetRandomPath", 0, wanderInterval);
				} else {
					SetRandomPath();
				}
			} else {
				CancelInvoke("SetRandomPath");
				InvokeRepeating("Chase", 0, 1f);
			}
		}
	}

	public void Stop() {
		CancelInvoke ("SetRandomPath");
		CancelInvoke ("Chase");
		StopCoroutine ("BehavioursLoop");
		cc.Stop ();
	}

	public void Death() {
		CancelInvoke ("SetRandomPath");
		CancelInvoke ("Chase");
		StopCoroutine ("BehavioursLoop");
		cc.Stop ();
		collider.enabled = false;
		dead = true;
		GameManager.instance.SpawnEnemyWithDelay (10f);
		GameManager.instance.PlaySound ("ghost_death", this.transform.position, 0.35f);
	}

	void Update() {
		if (dead) {
			this.transform.localScale = Vector3.one*scale;
			scale-=Time.deltaTime*5.5f;
			if (scale<0) {
				Destroy (this.gameObject);
			}
		}
	}
}
