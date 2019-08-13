using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// 젤 첨에 조이스틱을 스크린 오버레이 공간으로 만들었더니 월드스페이스로 되어있는 3D UI의 인풋을 막아버리는 현상이 발생했다.
// 그렇다고 스크린 스페이스로 바꾸자니 조이스틱 라인 그리는거부터 뎁스 조절이 쉽지가 않다.
// 그래서 차라리 인풋은 스크린 스페이스의 최하단에서 받되
// 렌더링은 만들어진대로 스크린 오버레이에서 하는게 가장 좋다는 결론을 내렸다.
// 그래서 만들어진게 스크린 조이스틱 이벤트 핸들러 클래스다.
// 오버레이 캔버스에 있는 스크린 조이스틱에서는 인풋 이벤트를 직접 받지 않고
// 스크린 스페이스에 있는 이 핸들러로부터 이벤트를 전달받아서 처리하면 모든게 끝.
public class ScreenJoystickEventHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	public void OnPointerDown(PointerEventData eventData)
	{
		ScreenJoystick.instance.OnPointerDown(eventData);
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		ScreenJoystick.instance.OnPointerUp(eventData);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		ScreenJoystick.instance.OnBeginDrag(eventData);
	}

	public void OnDrag(PointerEventData eventData)
	{
		ScreenJoystick.instance.OnDrag(eventData);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		ScreenJoystick.instance.OnEndDrag(eventData);
	}
}
