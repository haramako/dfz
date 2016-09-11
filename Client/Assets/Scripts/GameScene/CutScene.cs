using UnityEngine;
using System.Collections;

public class CutScene : MonoBehaviour {


	//===================================================================
	// カットシーン
	//===================================================================

	public Camera Camera;

	public float targetNum;
	public GameObject[] CameraTargets;
	public GameObject CameraContainer;

	public GameObject Scene;

	Vector3 targetPosition;
	bool disable;

	public IEnumerator Start(){
		targetPosition = CameraTargets [0].transform.position;
		yield return null;
		var anim = GetComponent<Animation> ();
		while (true) {
			if (!anim.isPlaying) {
				Scene.SendMessage ("CutSceneFinished");
				disable = true;
				//GameObject.Destroy (gameObject);
				break;
			}
			yield return null;
		}
	}

	public void Update() {
		if (disable)
			return;
		Vector3 target;
		if (targetNum < 0) {
			target = CameraTargets [0].transform.position;
		} else if (targetNum >= CameraTargets.Length - 1) {
			target = CameraTargets [CameraTargets.Length - 1].transform.position;
		} else {
			var t1 = CameraTargets [(int)targetNum];
			var t2 = CameraTargets [(int)targetNum + 1];
			target = Vector3.Lerp (t1.transform.position, t2.transform.position, Mathf.Repeat (targetNum, 1));
		}

		targetPosition = Vector3.Lerp (target, targetPosition, Mathf.Pow(0.1f, Time.deltaTime));

		Camera.transform.position = CameraContainer.transform.position;
		Camera.transform.LookAt (targetPosition, Vector3.up);
	}
}
