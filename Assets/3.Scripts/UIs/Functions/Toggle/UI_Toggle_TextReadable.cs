using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class UI_Toggle_TextReadable : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] TMP_InputField[] inputFields;
    [SerializeField] Image toggleImage;

    [Header("Images")]
    [SerializeField] Sprite show;
    [SerializeField] Sprite hide;

    private bool isVisible;

    private void OnEnable()
    {
        SetVisible(false);
    }

    public void Toggle()
    {
        SetVisible(!isVisible);
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        toggleImage.sprite = visible ? show : hide;

        foreach (var field in inputFields)
        {
            field.contentType = visible ? TMP_InputField.ContentType.Standard : TMP_InputField.ContentType.Password;

            field.ForceLabelUpdate();
        }
    }
}
