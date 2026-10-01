using UnityEngine;
using UnityEngine.EventSystems;

public delegate void DragStartEvent(UI_DraggableWindow dragTarget, Vector2 startPosition);

public class UI_DraggableWindow : UIBase, IPointerDownHandler
{
    public event DragStartEvent OnDragStart;

    //드래그하면 어떤 트랜스폼을 움직여야 할까>?
    [SerializeField] RectTransform rootTransform;

    /// <summary> 마지막으로 수신받은 마우스 위치 </summary>
    Vector2 currentScreenPosition;
    /// <summary> 이동하려고 했는데 막혀버린 위치 </summary>
    Vector2 shiftedPosition;

    //껐다 켰을 때 위치 초기화용
    private void OnEnable()
    {
        if (rootTransform)
            rootTransform.localPosition = Vector3.zero;

        currentScreenPosition = Vector2.zero;
        shiftedPosition = Vector2.zero;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OnDragStart?.Invoke(this, eventData.position);
    }

    public void SetMouseStartPosition(Vector2 screenPosition)
    {
        currentScreenPosition = screenPosition;
        shiftedPosition = Vector2.zero;
    }
    public void SetMouseCurrentPosition(Vector2 screenPosition)
    {
        Vector2 screenDelta = screenPosition - currentScreenPosition;
        currentScreenPosition = screenPosition;

        //실제로 포지션이 얼마나 움직여야 하는가?
        //shiftedPosition이 남아있어서 이걸 상쇄한다면 어떻게 될까
        //   두 개의 부호가 같다는 것을 확인
        if (shiftedPosition.x * screenDelta.x > 0.0f)
        {
            //둘 중 절대값이 더 작은 쪽을 빼준다
            float counter = Mathf.Min(Mathf.Abs(screenDelta.x), Mathf.Abs(shiftedPosition.x));
            //원래값의 부호를 넣어주기
            counter *= Mathf.Sign(shiftedPosition.x);
            shiftedPosition.x -= counter;
            screenDelta.x -= counter;
        }
        if (shiftedPosition.y * screenDelta.y > 0.0f)
        {
            //둘 중 절대값이 더 작은 쪽을 빼준다
            float counter = Mathf.Min(Mathf.Abs(screenDelta.y), Mathf.Abs(shiftedPosition.y));
            //원래값의 부호를 넣어주기
            counter *= Mathf.Sign(shiftedPosition.y);
            shiftedPosition.y -= counter;
            screenDelta.y -= counter;
        }

        //이제 남은 거리가 없다
        //if (screenDelta.sqrMagnitude == 0f) return;

        Rect rootRect = rootTransform.rect;
        //rect에서 실제로 제공하는 위치는 실제 위치가 아닌 canvas상의 위치
        //월드, 로컬, 특정 대상 => 어떤 위치를 원하는가?
        //위치는 기본적으로 어디를 주는가?
        //본인의 "Pivot"을 기준으로 직접 줘야한다
        //바꾸고 나서 얼마나 튀어나갔는가를 확인해보기
        //                               원래 위치                                       +   이동량
        rootRect.position += (Vector2)(rootTransform.localPosition / UIManager.UIScale) + screenDelta;
        //튀어나온걸 보정해주는 값을 InversedAABB가 돌려주니까 보정해주는 위치만큼 이동을 자제한다
        Vector2 overScreen = rootRect.InversedAABB(UIManager.UIBoundary);

        //이동 당한 총량을 저장해놓기
        shiftedPosition += overScreen;
        screenDelta += overScreen;
        

        Vector3 positionDelta = (Vector3)screenDelta;

        if(UIManager.UIScale > 0f) positionDelta /= UIManager.UIScale;

        rootTransform.localPosition += positionDelta;
    }
}
