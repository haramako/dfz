using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;

public class GamePanel : Graphic, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler {

	public GameScene scene;

	// Update is called once per frame
	void Update () {
	
	}

	public void OnBeginDrag(PointerEventData ev){
		scene.OnBeginDrag (ev);
	}

	public void OnEndDrag(PointerEventData ev){
		scene.OnEndDrag (ev);
	}

	public void OnDrag(PointerEventData ev){
		scene.OnDrag (ev);
	}

	public void OnPointerClick (PointerEventData ev){
		scene.OnPointerClick (ev);
	}

}
