using UnityEngine;
using System.Collections;

public enum FadeType {
	none = 0,
	fadein = 1,
	fadeout = 2
}

public class UIFader : MonoBehaviour {
	float duration;
	FadeType fadeType;
	CanvasGroup cg;
	float timePassed;

	// Use this for initialization
	void Start () {
		cg = this.GetComponent<CanvasGroup> ();
	}
	
	// Update is called once per frame
	void Update () {
		if (fadeType==FadeType.fadeout) {
			if (duration==0) {
				cg.alpha = 0;
				fadeType = FadeType.none;
			} else {
				cg.alpha = Mathf.Lerp(1, 0, timePassed);
				timePassed+=Time.deltaTime*(1/duration);
				if (cg.alpha == 0) {
					fadeType = FadeType.none;
				}
			}
		}
		if (fadeType==FadeType.fadein) {
			if (duration==0) {
				cg.alpha = 1;
				fadeType = FadeType.none;
			} else {
				cg.alpha = Mathf.Lerp(0, 1, timePassed);
				timePassed+=Time.deltaTime*(1/duration);
				if (cg.alpha == 1) {
					fadeType = FadeType.none;
				}
			}
		}
	}

	public void FadeOut(float d) {
		fadeType = FadeType.fadeout;
		duration = d;
		timePassed = 0;
		cg.interactable = false;
		cg.blocksRaycasts = false;
	}

	public void FadeIn(float d) {
		fadeType = FadeType.fadein;
		duration = d;
		timePassed = 0;
		cg.interactable = true;
		cg.blocksRaycasts = true;
	}

}
