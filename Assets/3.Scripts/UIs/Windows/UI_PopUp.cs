using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;

public class UI_PopUp : UIBase, ISystemMessagePossible, IConfirmable
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI contextText;
    [SerializeField] TextMeshProUGUI confirmText;
    [SerializeField] Button confirmButton;
    Action confirmAction;

    public override void Registration(UIManager manager)
    {
        base.Registration(manager);
        confirmButton.onClick.AddListener(Confirm);
    }
    public override void Unregistration(UIManager manager)
    {
        confirmButton.onClick.RemoveListener(Confirm);
        confirmAction = null;
        base.Unregistration(manager);
    }

    public void Confirm()
    {
        confirmAction?.Invoke();
    }

    public void SetConfirmAction(Action newAction)
    {
        confirmAction -= newAction;
        confirmAction += newAction;
    }

    public void SetSystemMessage(string title, string context, string confirm)
    {
        titleText?.SetText(title);
        contextText?.SetText(context);
        confirmText?.SetText(confirm);
    }
}
