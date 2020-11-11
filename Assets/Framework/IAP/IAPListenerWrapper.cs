using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;

// 앱 시작하자마자 초기화 하는 옵션을 사용하지 않는다면
// IAPButton이나 IAPListener가 최초로 등장할때 IAP 초기화가 이뤄지는데
// 이때 결제 오류 났다거나 펜딩 걸린것들의 결과가 오게된다.
// 이걸 받아서 처리하려면 IAPListener가 있어야 하는데 이걸 위해 만든 Wrapper 클래스다.
// 만들고나서 다시 만들필요 없으므로 DontDestroyOnLoad 걸어둔다.
//
// 이걸 로비에서 만들어야하는 이유가 하나 더 있는데
// 캐시샵에다가 할 경우 들어오자마자 초기화 되는 동안
// 인앱결제 버튼을 누르면 초기화가 덜 된 상태일때 결제 실패 에러가 뜨게된다.
// 결제를 하려는 유저 입장에선 좋지 않은 결과이므로 이왕이면 로비에서 미리 초기화해서 결제는 아무문제 없이 되는게 좋을거 같다.
public class IAPListenerWrapper : MonoBehaviour
{
	public static IAPListenerWrapper instance
	{
		get
		{
			if (_instance == null)
			{
				// 콜백 설정이 되어있는채로 로드하기 위해 프리팹을 로드한다.
				_instance = Instantiate<GameObject>(Resources.Load<GameObject>("IAPListenerWrapper")).GetComponent<IAPListenerWrapper>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}
	static IAPListenerWrapper _instance = null;

	// Listener를 테스트 해보니 IAPButton이 제대로 다 동작하는 캐시샵 페이지에서도 모든 메세지를 Listen하고 있어서 중복으로 들어오게 된다.
	// IAP Button이 하나도 보이기 전에 수동초기화를 할때만 받게 하고 싶은거라
	// on off 기능을 추가하기로 한다.
	public void EnableListener(bool enable)
	{
		cachedIAPListener.enabled = enable;
	}

	public void OnPurchaseComplete(Product product)
	{
		Debug.Log("IAP Listener OnPurchaseComplete");
	}

	Product _failedProduct;
	PurchaseFailureReason _failedReason;
	public Product failedProduct { get { return _failedProduct; } }
	public PurchaseFailureReason failedReason { get { return _failedReason; } }
	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
	{
		Debug.Log("IAP Listener OnPurchaseFailed");

		if (reason == PurchaseFailureReason.UserCancelled)
		{
			// 초기화 Listener인데 이런게 오진 않겠지
		}
		else
		{
			// DuplicateTransaction인게 있다면 미리 기억해놔야한다.
			// 창을 바로 띄우진 않고 필요할때 사용하기로 한다.
			_failedProduct = product;
			_failedReason = reason;
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}

	public void ConfirmPending(Product product)
	{
		// 제대로 종료되었다면 null로 리셋시켜둔다.
		if (_failedProduct.definition.id == product.definition.id)
			_failedProduct = null;
	}



	IAPListener _iapListener;
	public IAPListener cachedIAPListener
	{
		get
		{
			if (_iapListener == null)
				_iapListener = GetComponent<IAPListener>();
			return _iapListener;
		}
	}
}