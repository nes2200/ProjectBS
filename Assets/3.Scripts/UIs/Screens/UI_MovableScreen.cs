using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class UI_MovableScreen : UI_ScreenBase
{
    [SerializeField] List<UIBase> popupList = new();
    Vector3 popupPosition = Vector3.zero;
    Vector3 popupShift = new(15f,-15f, 0f);

    UI_DraggableWindow currentDragTarget = null;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        //InputManager.OnCancel += (value) => UIManager.ClaimToggleUI(UIType.Menu);
        InputManager.OnMouseMove -= MouseMove;
        InputManager.OnMouseMove += MouseMove;
        InputManager.OnMouseLeftButton -= MouseLeft;
        InputManager.OnMouseLeftButton += MouseLeft;
        UIManager.OnPopUp -= PopUp;
        UIManager.OnPopUp += PopUp;
    }
    public override void Unregistration(UIManager manager)
    {
        base.Unregistration(manager);
        InputManager.OnMouseMove -= MouseMove;
        InputManager.OnMouseLeftButton -= MouseLeft;
        UIManager.OnPopUp -= PopUp; 
    }

    protected override GameObject OnSetChild(GameObject newChild)
    {
        //새로운 자식에게 UIManager가서 등록받아오라 시킨다
        UIManager.ClaimSetUI(newChild);
        
        if (newChild)
        {
            UI_DraggableWindow asDraggable = newChild.GetComponentInChildren<UI_DraggableWindow>();
            if (asDraggable)
            {
                asDraggable.OnDragStart -= SetDragTarget;
                asDraggable.OnDragStart += SetDragTarget;
            }
        }

        return base.OnSetChild(newChild);
    }
    protected override void OnUnsetChild(GameObject oldChild)
    {
        UIManager.ClaimUnsetUI(oldChild);

        if (oldChild)
        {
            UI_DraggableWindow asDraggable = oldChild.GetComponentInChildren<UI_DraggableWindow>();
            if (asDraggable)
            {
                asDraggable.OnDragStart -= SetDragTarget;
            }
        }

        base.OnUnsetChild(oldChild);
    }

    void SetDragTarget(UI_DraggableWindow dragTarget, Vector2 startPosition)
    {
        currentDragTarget = dragTarget;
        if (currentDragTarget)
        {
            currentDragTarget.SetMouseStartPosition(startPosition);
        }
    }

    private void MouseLeft(bool value, Vector2 screenPosition, Vector3 position)
    {
        if (!value) currentDragTarget = null;
    }

    void MouseMove(Vector2 screenPosition, Vector3 worldPosition)
    {
        if (currentDragTarget)
        {
            currentDragTarget.SetMouseCurrentPosition(screenPosition);
        }
    }

    private void PopUp(string title, string context, string confirm)
    {
        GameObject newChild = SetChild(ObjectManager.CreateObject("PopUp"));
        if (newChild)
        {
            if(newChild.TryGetComponent(out UIBase newUI))
            {
                //만들고 자리 먼저 잡기
                newChild.transform.localPosition = GetNextPopupPosition();

                //리스트에 중복체크 하고 넣기
                if(!popupList.Contains(newUI)) popupList.Add(newUI);
            }

            if(newChild.TryGetComponent(out ISystemMessagePossible target))
            {
                target.SetSystemMessage(title, context, confirm);
            }

            if(newChild.TryGetComponent(out IConfirmable confirmTarget))
            {
                confirmTarget.SetConfirmAction(() => // 팝업창을 누르면
                {
                    if(newUI) popupList.Remove(newUI); //팝업에서 제거
                    UnsetChild(newChild); //자식에서 제외하고
                    ObjectManager.DestroyObject(newChild); //파괴함
                });
            }
        }
    }

    public bool TryClosePopup()
    {
        for(int i = popupList.Count - 1; i >= 0; i--)
        {
            UIBase popup = popupList[i];
            if (!popup || !popup.gameObject.activeInHierarchy) continue;

            popupList.RemoveAt(i);
            UnsetChild(popup.gameObject);
            ObjectManager.DestroyObject(popup.gameObject);
            return true;
        }
        return false;
    }

    public Vector3 GetNextPopupPosition()
    {
        //팝업 포지션 계산
        //가지고 있는 팝업 리스트 중에서 가장 오른쪽 아래에 있는 녀석을 구하기
        Vector3 bestScore = Vector3.zero;

        if(popupList.Count == 0) return bestScore;

        foreach (UIBase currentPopup in popupList)
        {
            Vector3 currentScore = currentPopup.transform.localPosition;
            if (bestScore.x < currentScore.x) bestScore.x = currentScore.x;
            if (bestScore.y > currentScore.y) bestScore.y = currentScore.y;
        }
        return bestScore + popupShift;
    }
}
