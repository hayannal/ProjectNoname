using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class EndDragHandler : MonoBehaviour, IDragHandler, IEndDragHandler {

	public delegate void onEndDragDelegate(Vector2 delta);
	public onEndDragDelegate onEndDrag;

	public void OnDrag(PointerEventData data)
	{
	}

	public void OnEndDrag(PointerEventData data)
	{
		if (onEndDrag != null)
			onEndDrag(data.delta);
	}
}
