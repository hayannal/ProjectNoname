using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class DragHandler : MonoBehaviour, IDragHandler
{
	public delegate void onDragDelegate(Vector2 delta);
	public onDragDelegate onDrag;
	
	public void OnDrag(PointerEventData data)
	{
		if (onDrag != null)
			onDrag(data.delta);
	}
}
