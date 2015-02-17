using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.EventSystems;

public class UIController : MonoBehaviour {

	private static UIController _instance;
	UIFader mainMenuParent;
	UIFader pauseMenuParent;
	UIFader levelSelectParent;
	UIFader blackOverlay;
	UIFader logo;
	UIFader endgameMenuParent;
	public Text playerSpeedText;
	public Text enemySpeedText;
	public Text enemyNumText;
	public Text livesText;

	int selectedLevel;
	
	public GameObject levelToggle;
	
	//Public variable for other components to use
	public static UIController instance
	{
		get
		{
			//If _instance hasn't been set yet, we grab it from the scene
			//This will only happen the first time this reference is used
			if(_instance == null)
				_instance = GameObject.FindObjectOfType<UIController>();
			return _instance;
		}
	}

	// Use this for initialization
	void Start () {
		mainMenuParent = GameObject.Find ("MainMenuParent").GetComponent<UIFader> ();
		pauseMenuParent = GameObject.Find ("PauseMenuParent").GetComponent<UIFader> ();
		levelSelectParent = GameObject.Find ("LevelSelectParent").GetComponent<UIFader> ();
		blackOverlay = GameObject.Find ("BlackOverlay").GetComponent<UIFader> ();
		logo = GameObject.Find ("TitleLogo").GetComponent<UIFader> ();
		endgameMenuParent = GameObject.Find ("EndgameMenuParent").GetComponent<UIFader> ();
	}


	public void HideMenuUI() {
		mainMenuParent.FadeOut (0);
		levelSelectParent.FadeOut (0);
		logo.FadeOut (0);
	}

	public void OnLevelChange() {
		ToggleGroup tg = GameObject.Find ("LevelsSelect").GetComponent<ToggleGroup> ();
		IEnumerable<Toggle> actives = tg.ActiveToggles ();
		Toggle active = actives.ElementAtOrDefault (0);
		int id = active.gameObject.GetComponent<LevelToggleInfo> ().id;
		Button startButton = GameObject.Find("StartButton").GetComponent<Button>();
		if (GameManager.instance.levels[id].content.GetLength(0)==0 || GameManager.instance.levels[id].content.GetLength(1)==0) {
			startButton.interactable = false;
		} else {
			startButton.interactable = true;
		}
		selectedLevel = id;
	}

	public IEnumerator ShowLevelsAndSettings() {
		mainMenuParent.FadeOut (0.3f);
		yield return new WaitForSeconds (0.3f);
		levelSelectParent.FadeIn (0.3f);
		GameManager.instance.LoadLevels ();
		int i = 0;
		Object[] levels = Object.FindObjectsOfType (typeof(LevelToggleInfo));
		foreach(Object obj in levels) {
			LevelToggleInfo go = obj as LevelToggleInfo;
			if (go.gameObject.activeSelf) {
				Destroy(go.gameObject);
			}
		}
		foreach (Level l in GameManager.instance.levels) {
			GameObject go = Instantiate(levelToggle) as GameObject;
			go.SetActive(true);
			go.transform.SetParent(levelToggle.transform.parent, true);
			LayoutElement le = go.GetComponent<LayoutElement>();
			le.preferredHeight = le.transform.parent.transform.parent.GetComponent<RectTransform> ().rect.height/4;
			go.transform.Find("Padding/LevelName").GetComponent<Text>().text = l.name;
			Toggle t = go.GetComponent<Toggle>();
			go.GetComponent<LevelToggleInfo>().id = i;
			t.onValueChanged.AddListener((v) => OnLevelChange());
			if (i==0) {
				go.GetComponent<Toggle>().isOn = true;
			} else {
				go.GetComponent<Toggle>().isOn = false;
			}
			i++;
		}
		selectedLevel = 0;
		//wait for 1 frame, cause Content Size Fitter wont work right away
		yield return new WaitForSeconds (0.05f);
		//scroll the list to the top
		Scrollbar scroll = GameObject.Find ("LevelsScrollbar").GetComponent<Scrollbar> ();
		scroll.value = 1;
	}
	
	public void PlayClick() {
		ClickSound();
		StartCoroutine (ShowLevelsAndSettings ());
	}
	
	IEnumerator StartLevel() {
		blackOverlay.FadeIn (0.5f);
		HideMenuUI ();
		yield return new WaitForSeconds (0.5f);
		blackOverlay.FadeOut (0.5f);
		GameManager.instance.StartGame (selectedLevel);
		livesText.text = "LIVES: "+GameManager.instance.lives;
	}
	
	public void StartLevelClick() {
		ClickSound();
		StartCoroutine (StartLevel ());
	}

	public void RestartLevelClick() {
		ClickSound();
		StartCoroutine (RestartLevel ());
	}

	IEnumerator RestartLevel() {
		blackOverlay.FadeIn (0.5f);
		yield return new WaitForSeconds (0.5f);
		endgameMenuParent.FadeOut (0);
		blackOverlay.FadeOut (0.5f);
		GameManager.instance.StartGame (selectedLevel);
		livesText.text = "LIVES: "+GameManager.instance.lives;
	}


	public void PlayerDeath() {
		StartCoroutine ("OnDeath");
	}

	IEnumerator OnDeath() {
		livesText.text = "LIVES: "+GameManager.instance.lives;
		if (GameManager.instance.lives>0) {
			yield return new WaitForSeconds (1.1f);
			blackOverlay.FadeIn (0.3f);
			yield return new WaitForSeconds (0.3f);
			blackOverlay.FadeOut (0.3f);
		}
	}

	public void OnGameEnd(GameResult gr) {
		endgameMenuParent.FadeIn (0.3f);
		string title = "";
		switch (gr) {
		case GameResult.win: title = "VICTORY"; break;
		case GameResult.lose: title = "GAME OVER"; break;
		}
		GameObject.Find ("EndgameText").GetComponent<Text> ().text = title;
	}

	public void OnPlayerSpeedChange(float v) {
		float value = Mathf.Round(v * 100f) / 100f;
		playerSpeedText.text = "Player speed: " + value;
		GameManager.instance.playerSpeed = value;
	}

	public void OnEnemySpeedChange(float v) {
		float value = Mathf.Round(v * 100f) / 100f;
		enemySpeedText.text = "Enemy speed: " + value;
		GameManager.instance.enemySpeed = value;
	}

	public void OnEnemyNumChange(float v) {
		int value = Mathf.FloorToInt (v);
		enemyNumText.text = "Enemies: " + value;
		GameManager.instance.enemyNum = value;
	}

	public void ShowPauseMenu() {
		CanvasGroup cg = pauseMenuParent.GetComponent<CanvasGroup> ();
		cg.alpha = 1;
		cg.interactable = true;
		cg.blocksRaycasts = true;

		Object[] buttons = Object.FindObjectsOfType (typeof(Button));
		foreach (Object obj in buttons) {
			Button but = obj as Button;
			PointerEventData pointer = new PointerEventData(EventSystem.current);
			ExecuteEvents.Execute(but.gameObject, pointer, ExecuteEvents.pointerUpHandler);
		}

	}

	public void HidePauseMenu() {
		CanvasGroup cg = pauseMenuParent.GetComponent<CanvasGroup> ();
		cg.alpha = 0;
		cg.interactable = false;
		cg.blocksRaycasts = false;
	}

	public void HideEndgameMenu() {
		CanvasGroup cg = endgameMenuParent.GetComponent<CanvasGroup> ();
		cg.alpha = 0;
		cg.interactable = false;
		cg.blocksRaycasts = false;
	}

	public void ResumeClick() {
		ClickSound();
		GameManager.instance.OnUnpause ();
	}

	public void BackToMenuClick() {
		ClickSound();
		StartCoroutine (BackToMenu());
		HidePauseMenu ();
		HideEndgameMenu ();
		GameManager.instance.OnBackToMenu ();
	}

	void ClickSound() {
		GameManager.instance.PlaySound ("click", Camera.main.transform.position, 0.34f);
	}

	IEnumerator BackToMenu() {
		blackOverlay.FadeIn (0.4f);
		yield return new WaitForSeconds (0.4f);
		livesText.text = "";
		blackOverlay.FadeOut (0.4f);
		mainMenuParent.FadeIn (0.6f);
		logo.FadeIn (0);
	}
}
