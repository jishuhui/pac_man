using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerController : MonoBehaviour {
	CharController cc;
	AudioSource waka;
	bool angry = false;
	List<Direction> pressed;
	public GameObject deathFX;
	bool alive = true;
	Animator anim;
	public Material normalMat;
	public Material angryMat;

	// Use this for initialization
	void Start () {
		cc = this.GetComponent<CharController> ();
		waka = this.transform.FindChild ("waka").GetComponent<AudioSource>();
		anim = this.GetComponent<Animator> ();
		pressed = new List<Direction> ();
	}
	
	// Update is called once per frame
	void Update () {
		//Using they input queue - the last pressed key has the priority
		if (GameManager.instance.gameState==GameState.play) {
			if (Input.GetKeyDown(KeyCode.LeftArrow)) {
				pressed.Add(Direction.left);
			}
			if (Input.GetKeyUp(KeyCode.LeftArrow)) {
				pressed.Remove(Direction.left);
			}
			if (Input.GetKeyDown(KeyCode.RightArrow)) {
				pressed.Add(Direction.right);
			}
			if (Input.GetKeyUp(KeyCode.RightArrow)) {
				pressed.Remove(Direction.right);
			}
			if (Input.GetKeyDown(KeyCode.UpArrow)) {
				pressed.Add(Direction.up);
			}
			if (Input.GetKeyUp(KeyCode.UpArrow)) {
				pressed.Remove(Direction.up);
			}
			if (Input.GetKeyDown(KeyCode.DownArrow)) {
				pressed.Add(Direction.down);
			}
			if (Input.GetKeyUp(KeyCode.DownArrow)) {
				pressed.Remove(Direction.down);
			}
		
			//Try to change direction to the last pressed key
			if (pressed.Count>0) {
				cc.TrySetMoveDirection(pressed[pressed.Count-1]);
			}
		}
		waka.volume = Mathf.Lerp(waka.volume, rigidbody.velocity.normalized.magnitude * 0.24f, Time.deltaTime*16);
		if (rigidbody.velocity.magnitude > 0.1f) {
			anim.SetBool("eat", true);
		} else {
			anim.SetBool("eat", false);
		}

		Direction dir = cc.currentDirection;
		switch(dir) {
		case Direction.down: transform.eulerAngles = new Vector3(0,0,-90);break;
		case Direction.up: transform.eulerAngles = new Vector3(0,0,90);break;
		case Direction.left: transform.eulerAngles = new Vector3(0,180,0);break;
		case Direction.right: transform.eulerAngles = new Vector3(0,0,0);break;
		}
	}

	void StopAnger() {
		angry = false;
		waka.clip = GameManager.instance.sounds["waka_normal"];
		waka.Play();
		GameManager.instance.gameMusic.Play();
		GameManager.instance.gameMusicAngry.Stop();
		MeshRenderer[] mrs = this.GetComponentsInChildren<MeshRenderer>();
		foreach (MeshRenderer mr in mrs) {
			mr.material = normalMat;
		}
	}

	void Death() {
		alive = false;
		GameObject go = Instantiate (GameManager.instance.entities ["player_dead"], this.transform.position, Quaternion.identity) as GameObject;
		Rigidbody[] rbs = go.GetComponentsInChildren<Rigidbody> ();
		foreach (Rigidbody rb in rbs) {
			rb.AddForce(new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), Random.Range(-2f,-3f))*26);
			rb.AddTorque(new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f), Random.Range(-1f,1f))*20);
		}
		GameManager.instance.PlaySound ("death", this.transform.position, 0.34f);
		GameObject fx = Instantiate (deathFX, this.transform.position, deathFX.transform.rotation) as GameObject;
		Destroy (fx, 2);
		GameManager.instance.PlayerDeath ();
		Destroy (this.gameObject);
	}

	void Eat(GameObject go) {
		GameManager.instance.PlaySound ("eat", go.transform.position, 0.35f);
		Destroy(go);
		GameManager.instance.EatFood();
	}

	void OnTriggerEnter(Collider col) {
		if (col.CompareTag("food_small")) {
			Eat (col.gameObject);
		}
		if (col.CompareTag("food_big")) {
			Eat (col.gameObject);
			if (!angry) {
				angry = true;
				GameManager.instance.gameMusic.Pause();
				GameManager.instance.gameMusicAngry.Play();
				Invoke("StopAnger", 7);
				MeshRenderer[] mrs = this.GetComponentsInChildren<MeshRenderer>();
				foreach (MeshRenderer mr in mrs) {
					mr.material = angryMat;
				}
			} else {
				CancelInvoke("StopAnger");
				Invoke("StopAnger", 7);
			}
		}
		if (col.CompareTag("enemy")) {
			if (alive) {
				OnEnemyHit(col.gameObject);
			}
		}
	}

	void OnEnemyHit(GameObject go) {
		if (!angry) {
			Death ();
		} else {
			go.GetComponent<EnemyController>().Death();
		}
	}

	void FixedUpdate() {
		rigidbody.angularVelocity = Vector3.zero;
	}

	public void Stop() {
		pressed = new List<Direction> ();
		CancelInvoke("StopAnger");
		cc.Stop ();
	}
}
