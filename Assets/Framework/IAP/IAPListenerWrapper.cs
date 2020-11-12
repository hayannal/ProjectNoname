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
// 캐시샵 OnEnable 부분에서 처리할 경우 들어오자마자 초기화 되는 동안
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
	// IAP Button이 하나도 보이기 전에 수동초기화를 할때는 메세지를 받아야하지만 캐시샵이 보이고 나서 처리하면 안되므로
	// on off 기능을 추가하기로 한다.
	//
	// 최초로 호출된다는 점에서 Initialize 역할도 하고있다.
	// 인앱결제를 하고나서 OnPurchaseComplete이 호출되기전에 앱을 내리거나 종료하면
	// 다음번 구동 후 최초 IAP Initialize때 구매처리가 완료되지 않은 항목에 대해서 다시 OnPurchaseComplete함수가 오게된다.
	// 이때 만약 처리하지 않더라도 ConfirmPendingPurchase을 호출하기 전까지는 계속 날아오니(재설치해도 날아온다.)
	// 서버에서의 영수증 검증이 끝나고 템이 지급된 후 호출해주면 모든게 완료된다.
	//
	// 대신 유의할게 있는데 위 얘기는 IAP Button과 IAP Listener에 있는 Consume Purchase옵션을 모두 껐을때 해당되는 얘기다.
	// 공식문서 상으로는 서버 검증할때는 꼭 꺼야한다고 나와있다.(코드리스 쪽에서 설명안하고 구매처리쪽에서 설명한다.)
	// 이 옵션이 켜있을때는(디폴트로는 켜있다.)
	// 클라에서 OnPurchaseComplete함수가 호출되는 순간 Consume까지 자동으로 완료시키겠단 의미인데
	// 정상적인 상황에서는 크게 문제될게 없지만,
	// OnPurchaseComplete이 호출되기 전에 앱을 내리거나 종료하면 다음번 구동 후 최초 IAP Initialize때 1회만 OnPurchaseComplete 함수가 호출되면서 Consume 처리가 진행된다.
	// 즉 재구동해서 IAP Initialize할때 기회를 날리면 더는 복구하기 애매해진다는거다.
	// 그러니 서버검증을 쓰는 상황이라면 무조건 저 체크를 풀고 써야한다.
	//
	// 참고로 구글 결제 페이지에서 나오는 청구됨은 이거와 상관없이 그냥 카드결제가 오케이 되는걸 의미하는거 같다.
	// 저 옵션값이랑 상관없이 무조건 5분되면 청구됨으로 바뀐다.
	public void EnableListener(bool enable)
	{
		cachedIAPListener.enabled = enable;
	}

	Product _pendingProduct;
	PurchaseFailureReason _pendingReason;
	public Product pendingProduct { get { return _pendingProduct; } }

	public void OnPurchaseComplete(Product product)
	{
		//Debug.Log("IAP Listener OnPurchaseComplete");

		// 여기에 상품이 들어온다는거 자체가 이전에 인앱결제를 성공했는데 OnPurchaseComplete를 받기도 전에 앱이 종료되서
		// 다음번 구동 후 IAP Initialize때 호출되었다는걸 의미한다.
		// 이땐 DuplicateTransaction와 마찬가지로 구매완료가 다 안된거처럼 처리해서 복구를 시켜주면 된다.
		// 창을 바로 띄우기보단 필요할때 사용할테니 이렇게 저장해두고 불러다 쓰기로 한다.
		_pendingProduct = product;
	}

	// 구매처리 완료되지 않은 상태에서 재구동 후 IAP Initialize때 오는건 이게 아니라 위 Complete함수다.
	// 그러니 사실 Listener에서는 이걸 처리할 필요가 없긴 한데 혹시 몰라서 그냥 두기로 한다. 아마 이쪽으로 호출되진 않을거다.
	public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
	{
		//Debug.Log("IAP Listener OnPurchaseFailed");

		if (reason == PurchaseFailureReason.UserCancelled)
		{
			// 초기화 Listener인데 이런게 오진 않겠지
		}
		else
		{
			_pendingProduct = product;
			_pendingReason = reason;
			Debug.LogFormat("PurchaseFailed reason {0}", reason.ToString());
		}
	}

	public void CheckConfirmPendingPurchase(Product product)
	{
		// 제대로 종료되었다면 null로 리셋시켜둔다.
		if (_pendingProduct != null && _pendingProduct.definition.id == product.definition.id)
			_pendingProduct = null;
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