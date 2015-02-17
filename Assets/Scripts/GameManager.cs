using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using System.Linq;

public enum GameState {
	mainMenu = 0,
	levelSelect = 1,
	prePlay = 2,
	play = 3,
	pause = 4,
	end = 5
}

public enum GameResult {
	win = 0,
	lose = 1
}

public enum Direction {
	up = 0,
	down = 1,
	left = 2,
	right = 3,
	none = 4
}

public struct Level {
	public string name;
	public char[,] content;
}

public class GameManager : MonoBehaviour {
	private static GameManager _instance;
	public List<Level> levels;
	int currentLevel;

	public GameObject levelToggle;
	public Dictionary<string, AudioClip> sounds;
	public Dictionary<string, GameObject> entities;
	public AudioSource gameMusic;
	public AudioSource gameMusicAngry;

	Material groundMaterial;

	public GameState gameState;
	public GameResult gameResult;

	public float playerSpeed = 2f;
	public float enemySpeed = 2f;
	public int enemyNum = 2;
	public int lives = 3;

	public List<Vector2> walkableSpots;
	List<Vector2> spawnSpots;
	Vector2 playerSpawn;

	int total_food = 0;

	//Public variable for other components to use
	public static GameManager instance
	{
		get
		{
			//If _instance hasn't been set yet, we grab it from the scene
			//This will only happen the first time this reference is used
			if(_instance == null)
				_instance = GameObject.FindObjectOfType<GameManager>();
			return _instance;
		}
	}

	/**********************
		INITIALIZATION	
	***********************/

	void LoadSounds() {
		sounds = new Dictionary<string, AudioClip> ();
		Object[] gos = Resources.LoadAll ("Sounds");
		foreach(Object go in gos) {
			sounds.Add(go.name, go as AudioClip);
		}
	}

	void LoadEntities() {
		entities = new Dictionary<string, GameObject> ();
		Object[] gos = Resources.LoadAll ("Prefabs/Entities", typeof(GameObject));
		foreach(Object go in gos) {
			entities.Add(go.name, go as GameObject);
		}
	}

	void Awake() {
		LoadSounds ();
		LoadEntities ();
		groundMaterial = Resources.Load ("Materials/GroundMat") as Material;
	}

	//Handle pausing
	void Update () {
		if (Input.GetKeyDown(KeyCode.Escape)) {
			if (gameState==GameState.play) {
				OnPause();
			} else {
				if (gameState==GameState.pause) {
					OnUnpause();
				}
			}
		}
	}

	/**********************
		MISC FUNCTIONS 
	***********************/

	//Converts array of string into a 2D array of char
	char[,] ReadLevel(string path) {
		string[] lines = File.ReadAllLines(path);
		char[,] content;
		if (lines.Length>2 && lines.Length<64 && lines[0].Length>2 && lines[0].Length<64) {
			int sizex = lines [0].Length;
			int sizey = lines.Length;
			content = new char[sizex, sizey];
			for (int i = 0; i<sizey; i++) {
				for (int j = 0; j<sizex; j++) {
					content[j, i] = lines[i][j];
				}
			}
		} else {
			content = new char[0, 0];
		}
		return content;
	}

	//Clean up scene
	void CleanUp() {
		GameObject level = GameObject.Find ("LevelParent");
		if (level!=null) {
			Destroy (GameObject.Find("LevelParent"));
		}

		GameObject[] enemies = GameObject.FindGameObjectsWithTag ("enemy");
		foreach (GameObject go in enemies) {
			Destroy(go);
		}

		GameObject player = GameObject.FindGameObjectWithTag ("player");
		if (player!=null) {
			Destroy (player);
		}
	}

	public void LoadLevels() {
		DirectoryInfo di = new DirectoryInfo (Application.dataPath + "/levels/");
		levels = new List<Level> ();
		foreach (FileInfo file in di.GetFiles()) {
			if (file.Extension==".lvl") {
				Level l = new Level();
				l.name = Path.GetFileNameWithoutExtension(file.Name);
				//Debug.Log(l.name);
				string fullpath = Application.dataPath + "/levels/"+file.Name;
				l.content = ReadLevel(fullpath);
				levels.Add(l);
			}
		}
	}

	public void BuildLevel(int id) {
		Level lvl = levels [id];
		currentLevel = id;
		total_food = 0;
		GameObject levelParent = new GameObject ();
		levelParent.name = "LevelParent";
		GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Quad);
		Destroy (ground.collider);
		float w = lvl.content.GetLength (0);
		float h = lvl.content.GetLength (1);
		ground.transform.localScale = new Vector3 (w, h, 1);
		ground.transform.SetParent(levelParent.transform);
		ground.transform.position = new Vector3 (w/2, h/2, 0);
		groundMaterial.mainTextureScale = new Vector2 (w, h);
		ground.renderer.sharedMaterial = groundMaterial;
		bool playerPlaced = false;

		Dictionary<string, GameObject> wallTiles = LoadWallTiles();
		walkableSpots = new List<Vector2> ();
		spawnSpots = new List<Vector2> ();

		for (int i = 0; i<lvl.content.GetLength (0); i++) {
			for(int j = 0; j<lvl.content.GetLength (1); j++) {
				GameObject prefab;
				if (lvl.content[i,j]!='a') {
					walkableSpots.Add(new Vector2(i,j));
				}
				if (lvl.content[i,j]=='x') {
					spawnSpots.Add(new Vector2(i,j));
				}
				switch(lvl.content[i,j]) {
					case '0': prefab = null; break;
					case '1': prefab = entities["food_small"]; total_food++; break;
					case '2': prefab = entities["food_big"]; total_food++; break;
					case 'a': prefab = GetWallTile(lvl.content, i, j, wallTiles); break;
					case 'p': 
						prefab = null; 
						if (!playerPlaced) {
							playerSpawn = new Vector2(i,j);
							playerPlaced = true;
							SpawnPlayer(playerSpawn);
						} 
						break;
					default: prefab = null; break;
				}

				if (prefab!=null) {
					Vector3 pos = new Vector3(i+0.5f,h-j-1+0.5f,0);
					/*
					if (prefab!=wallTiles["0neighbors"]) {
						pos+=Vector3.right*((1-prefab.transform.localScale.x)/2);
						pos+=Vector3.down*((1-prefab.transform.localScale.y)/2);
					}*/
					GameObject go = Instantiate(prefab, pos, prefab.transform.rotation) as GameObject;
					go.transform.SetParent(levelParent.transform);
				}
			}
		}
		SpawnEnemies (enemyNum);
		//levelParent.transform.position = new Vector3 (-w/2, -h/2, 0);
		Camera.main.transform.position = new Vector3(w/2, h/2, -30);
	}

	void SpawnEnemies(int count) {
		int spawnSpot = 0;
		char[,] lvl = levels [currentLevel].content;
		int c = 1;
		if (spawnSpots.Count>0) {
			for (int i = 0; i<count; i++) {
				Vector3 pos = new Vector3(spawnSpots[spawnSpot].x+0.5f, lvl.GetLength(1) - 1 - spawnSpots[spawnSpot].y + 0.5f, -0.46f);
				GameObject go = Instantiate(entities["ghost"], pos, Quaternion.identity) as GameObject;
				go.GetComponent<CharController>().level = lvl;
				go.GetComponent<CharController>().currentPos = new int[]{
					Mathf.FloorToInt(spawnSpots[spawnSpot].x), 
					Mathf.FloorToInt(spawnSpots[spawnSpot].y)
				};
				go.GetComponent<EnemyController>().startDelay = c*0.5f;
				spawnSpot++;
				c++;
				if (spawnSpot>spawnSpots.Count-1) {
					spawnSpot = 0;
				}
			}
		}
	}

	void SpawnEnemy() {
		SpawnEnemies (1);
	}

	public void SpawnEnemyWithDelay(float delay) {
		Invoke ("SpawnEnemy", delay);
	}

	void SpawnPlayer(Vector2 p) {
		int i = Mathf.FloorToInt(p.x);
		int j = Mathf.FloorToInt(p.y);
		Vector3 pos = new Vector3(i+0.5f, levels[currentLevel].content.GetLength(1) - 1 - j + 0.5f, -0.4f);
		GameObject go = Instantiate(entities["player"], pos, Quaternion.identity) as GameObject;
		go.GetComponent<CharController> ().level = levels [currentLevel].content;
		go.GetComponent<CharController> ().currentPos = new int[]{i,j};
	}

	Dictionary<string, GameObject> LoadWallTiles() {
		Object[] prefabs = Resources.LoadAll ("Prefabs/WallTiles", typeof(GameObject));
		Dictionary<string, GameObject> dict = new Dictionary<string, GameObject> ();
		foreach (Object pref in prefabs) {
			GameObject go = pref as GameObject;
			//Only add top level parents (dont add children)
			if (go.transform.parent==null) {
				dict.Add(pref.name, go);
			}
		}
		return dict;
	}

	//Returns list of neighboring walls relative to the given cell
	List<Direction> GetNeighbors(char[,] lvl, int i, int j) {
		List<Direction> list = new List<Direction> ();
		if (i>0) {
			if (lvl[i-1,j]=='a') {
				list.Add(Direction.left);
			}
		}
		if (i<lvl.GetLength(0)-1) {
			if (lvl[i+1,j]=='a') {
				list.Add(Direction.right);
			}
		}
		if (j>0) {
			if (lvl[i,j-1]=='a') {
				list.Add(Direction.up);
			}
		}
		if (j<lvl.GetLength(1)-1) {
			if (lvl[i,j+1]=='a') {
				list.Add(Direction.down);
			}
		}

		return list;
	}

	//Returns a specific wall prefab for a given level cell
	GameObject GetWallTile(char[,] lvl, int i, int j, Dictionary<string, GameObject> wallTiles) {
		List<Direction> neighbors = GetNeighbors (lvl, i, j);
		GameObject go = null;
		if (neighbors.Count==0) {
			go = wallTiles["0neighbors"];
		}
		if (neighbors.Count==1) {
			go = wallTiles["1neighbors"];
			switch(neighbors[0]) {
				case Direction.left: go.transform.eulerAngles = new Vector3(0,0,180);break;
				case Direction.right: go.transform.eulerAngles = new Vector3(0,0,0);break;
				case Direction.up: go.transform.eulerAngles = new Vector3(0,0,90);break;
				case Direction.down: go.transform.eulerAngles = new Vector3(0,0,-90);break;
			}
		}
		if (neighbors.Count==2) { 
			go = wallTiles["2neighbors"];
			if (neighbors.Contains(Direction.down) && neighbors.Contains(Direction.up)) {
				go = wallTiles["2neighbors"];
				go.transform.eulerAngles = new Vector3(0,0,90);
			}
			if (neighbors.Contains(Direction.right) && neighbors.Contains(Direction.left)) {
				go = wallTiles["2neighbors"];
				go.transform.eulerAngles = new Vector3(0,0,0);
			}
			if (neighbors.Contains(Direction.down)) {
				if (neighbors.Contains(Direction.right)) {
					go = wallTiles["2neighbors_angle"];
					go.transform.localScale = new Vector3(1,1,1);
				}
				if (neighbors.Contains(Direction.left)) {
					go = wallTiles["2neighbors_angle"];
					go.transform.localScale = new Vector3(-1,1,1);
				}

			}
			if (neighbors.Contains(Direction.up)) {
				if (neighbors.Contains(Direction.right)) {
					go = wallTiles["2neighbors_angle"];
					go.transform.localScale = new Vector3(1,-1,1);
				}
				if (neighbors.Contains(Direction.left)) {
					go = wallTiles["2neighbors_angle"];
					go.transform.localScale = new Vector3(-1,-1,1);
				}
			}
		}
		if (neighbors.Count==3) { 
			go = wallTiles["3neighbors"];
			if (!neighbors.Contains(Direction.up)) {
				go.transform.eulerAngles = Vector3.zero;
			}
			if (!neighbors.Contains(Direction.down)) {
				go.transform.eulerAngles = new Vector3(0,0,180);
			}
			if (!neighbors.Contains(Direction.right)) {
				go.transform.eulerAngles = new Vector3(0,0,-90);
			}
			if (!neighbors.Contains(Direction.left)) {
				go.transform.eulerAngles = new Vector3(0,0,90);
			}
		}
		if (neighbors.Count==4) { 
			go = wallTiles["4neighbors"];
		}
		return go;
	}

	//Calculates the z position of the camera
	//so it can cover the entire level
	public void SetCameraToFit(int id) {
		Level lvl = levels [id];
		float frustumHeight = lvl.content.GetLength(1);
		float screenRatio = Screen.height*1f / Screen.width;
		float levelRatio = lvl.content.GetLength (1) * 1f / lvl.content.GetLength (0);
		//If level ratio is smaller than screen ratio, then we calculate the frustum based on
		//width, rather than height
		if (levelRatio<screenRatio) {
			frustumHeight = lvl.content.GetLength (0)*screenRatio;
		}
		float distance = frustumHeight * 0.5f / Mathf.Tan(Camera.main.fieldOfView * 0.5f * Mathf.Deg2Rad);
		Vector3 p = Camera.main.transform.position;
		Camera.main.transform.position = new Vector3 (p.x, p.y, -distance);
	}

	public void PlaySound(string name, Vector3 pos, float vol) {
		GameObject go = Instantiate(entities["audiosource"], pos, Quaternion.identity) as GameObject;
		AudioSource aud = go.GetComponent<AudioSource>();
		aud.clip = sounds [name];
		aud.volume = vol;
		aud.loop = false;
		aud.Play ();
		Destroy (go, sounds [name].length);
	}

	void EndGame(GameResult gr) {
		gameState = GameState.end;
		gameResult = gr;
		UIController.instance.OnGameEnd (gr);
		CancelInvoke ("SpawnEnemy");
		if (gr==GameResult.lose) {
			PlaySound("game_lose", this.transform.position, 0.4f);
		} else {
			gameMusic.Stop ();
			gameMusicAngry.Stop ();
			GameObject.FindGameObjectWithTag("player").GetComponent<PlayerController>().Stop();
			GameObject[] enemies = GameObject.FindGameObjectsWithTag ("enemy");
			foreach(GameObject go in enemies) {
				go.GetComponent<EnemyController>().Stop();
			}
			PlaySound("game_win", this.transform.position, 0.49f);
		}
	}

	public void StartGame(int lvlid) {
		gameMusic.Play ();
		gameMusicAngry.Stop ();
		lives = 3;
		gameState = GameState.play;
		CleanUp ();
		BuildLevel (lvlid);
		SetCameraToFit (lvlid);
	}

	IEnumerator BackToMenu() {
		Time.timeScale = 1;
		yield return new WaitForSeconds(0.4f);
		CleanUp();
		gameMusic.Stop ();
		gameMusicAngry.Stop ();
		gameState = GameState.mainMenu;
	}

	/*********************
			EVENTS		
	**********************/
	
	public void EatFood() {
		total_food--;
		if (total_food==0) {
			EndGame(GameResult.win);
		}
	}

	public void PlayerDeath() {
		StartCoroutine ("OnDeath");
		UIController.instance.PlayerDeath ();
	}
	
	IEnumerator OnDeath() {
		lives--;
		GameObject[] enemies = GameObject.FindGameObjectsWithTag ("enemy");
		foreach(GameObject go in enemies) {
			go.GetComponent<EnemyController>().Stop();
		}
		CancelInvoke ("SpawnEnemy");
		gameMusic.Stop ();
		gameMusicAngry.Stop ();
		if (lives>0) {
			yield return new WaitForSeconds (1.4f);
			foreach(GameObject go in enemies) {
				Destroy(go);
			}
			SpawnEnemies (enemyNum);
			SpawnPlayer (playerSpawn);
			gameMusic.Play ();
		} else {
			yield return new WaitForSeconds (1.4f);
			EndGame(GameResult.lose);
		}
	}

	public void OnPause() {
		Time.timeScale = 0;
		gameState = GameState.pause;
		UIController.instance.ShowPauseMenu ();
		//Pause all the sounds
		Object[] audios = Object.FindObjectsOfType (typeof(AudioSource));
		foreach (Object obj in audios) {
			AudioSource a = obj as AudioSource;
			if (a.isPlaying) {
				a.gameObject.GetComponent<AudioController>().pausedOnGamePause = true;
				a.Pause();
			}
		}
	}
	
	public void OnUnpause() {
		Time.timeScale = 1;
		gameState = GameState.play;
		UIController.instance.HidePauseMenu ();
		//Unpause all the sounds, checking, if they were paused when player paused the game
		Object[] audios = Object.FindObjectsOfType (typeof(AudioSource));
		foreach (Object obj in audios) {
			AudioSource a = obj as AudioSource;
			if (a.gameObject.GetComponent<AudioController>().pausedOnGamePause) {
				a.Play();
				a.gameObject.GetComponent<AudioController>().pausedOnGamePause = false;
			}
		}
	}
	
	public void OnBackToMenu() {
		StartCoroutine (BackToMenu ());
	}

}
