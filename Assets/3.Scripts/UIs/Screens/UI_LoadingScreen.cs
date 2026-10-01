using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

public class UI_LoadingScreen : UI_ScreenBase, IProgress<int>, IStatus<string>
{
    public int Current { get; protected set; }
    public int Max { get; protected set; }
    public float Progress => (Max != 0) ? (float)Current / (float)Max : 0.0f;

    public int AddCurrent(int value) => Set(Current + value);

    public int AddMax(int value) => Set(Current, Max + value);

    [Header("Progress Bar")]
    [SerializeField] UnityEngine.UI.Slider progressBar;
    [SerializeField] TextMeshProUGUI progressText;
    [SerializeField] TextMeshProUGUI loadingText;

    [Header("Loading Layout")]
    [SerializeField] GameObject layoutOnComplete;
    [SerializeField] GameObject layoutOnLoading;

    [Header("Text")]
    [SerializeField] TextMeshProUGUI middleText;

    string[] context = { "조합이 중요합니다", "무기에 따라 유닛은 크게 바뀝니다", "스킬을 잘 선택해 주세요" };

    private void Awake()
    {
        SetScreenText();
    }

    // IStatus<T>
    public string SetCurrentStatus(string newText)
    {
        loadingText.SetText(newText);
        return newText;
    }

    public void SetComplete()
    {
        layoutOnComplete.SetActive(true);
        layoutOnLoading.SetActive(false);
    }

    public int Set(int newCurrent) 
    {
        Current = Mathf.Min(newCurrent, Max);
        progressBar.value = Progress;
        progressText.SetText($"{Progress * 100f : 0.00}%");
        return Current;
    }
    public int Set(int newCurrent, int newMax)
    {
        layoutOnComplete.SetActive(false);
        layoutOnLoading.SetActive(true);
        Max = newMax;
        return Set(newCurrent);
    }

    void SetScreenText()
    {
        middleText.text = context[Random.Range(0, context.Length)];
    }
}
