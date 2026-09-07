using System;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class JobAppearance
{
    public UnitJob job;

    [Header("Job Object")]
    public GameObject jobRoot;
    public GameObject[] decorations;

    [Header("Head Decoration")]
    public GameObject headDecoration;
    public bool coverHead;
    public bool coverHair;
}

public class MaleUnitAppearance : MonoBehaviour
{
    [Header("Job Appearance")]
    [SerializeField] JobAppearance jobAppearances;

    [Header("Common Head Objects")]
    [SerializeField] GameObject face;
    [SerializeField] Transform eyebrows;
    [SerializeField] Transform eyes;
    [SerializeField] Transform mouths;
    [SerializeField] Transform facialHair;
    [SerializeField] Transform hair;
    [SerializeField] Transform hairForHeadwear;

    //갑옷 색
    const string ObjectsMaterialName = "RGBRecolor_Objects";
    static readonly int Color1Id = Shader.PropertyToID("_Color1");
    static readonly int Color2Id = Shader.PropertyToID("_Color2");
    static readonly int Color3Id = Shader.PropertyToID("_Color3");
    Material objectsMaterialInstance;

    public bool ApplyAppearance()
    {
        RandomizeDecorations(jobAppearances.decorations);

        bool headDecorationActive = RandomizeHeadDecoration(jobAppearances.headDecoration, 0.4f);
        
        //투구 있고 투구가 머리를 가린다면 그대로 끝
        if (headDecorationActive && jobAppearances.coverHead) return true;

        return ApplyHeadAppearance(headDecorationActive && jobAppearances.coverHead, headDecorationActive, jobAppearances.coverHair);
    }


    bool ApplyHeadAppearance(bool headCovered, bool headDecorationActive , bool hairCovered)
    {
        if (!face || !eyebrows || !eyes || !mouths || !facialHair || !hair || !hairForHeadwear)
        {
            Debug.LogError("[MaleUnitAppearance] 공통 머리 외형 오브젝트를 찾지 못했습니다.", this);
            return false;
        }

        // Knight_GreatHelm처럼 얼굴 전체를 가리는 경우
        if (headCovered)
        {
            face.SetActive(false);
            facialHair.gameObject.SetActive(false);
            hair.gameObject.SetActive(false);
            hairForHeadwear.gameObject.SetActive(false);
            return true;
        }

        //혹시 꺼졌을 수 있으니까 켜주기
        face.SetActive(true);
        eyes.gameObject.SetActive(true);
        eyebrows.gameObject.SetActive(true);
        mouths.gameObject.SetActive(true);
        facialHair.gameObject.SetActive(true);

        //얼굴 각 요소 결정하기
        SetOnlyRandomChildActive(eyebrows, false);
        SetOnlyRandomChildActive(eyes, false);
        SetOnlyRandomChildActive(mouths, false);
        RandomizeEachChild(facialHair, 0.2f);

        hair.gameObject.SetActive(false);
        hairForHeadwear.gameObject.SetActive(false);

        //머리장식이 꺼져있으면 일반머리 사용
        if (!headDecorationActive)
        {
            hair.gameObject.SetActive(true);
            SetOnlyRandomChildActive(hair, false);
            return true;
        }

        // 머리 장식이 머리카락 전체를 가리는 경우
        if (hairCovered)
        {
            return true;
        }

        // 머리 장식이 있지만 머리카락 전체를 가리지 않는 경우
        hairForHeadwear.gameObject.SetActive(true);
        Transform selectedHair = SetOnlyRandomChildActive(hairForHeadwear, false);
        if (selectedHair)
        {
            for (int i = 0; i < selectedHair.childCount; i++)
            {
                selectedHair.GetChild(i).gameObject.SetActive(false);
            }
        }

        return true;
    }

    bool CreateRandomObjectsMaterial(Transform jobRoot)
    {
        if (objectsMaterialInstance) return true;

        Renderer[] renderers = jobRoot.GetComponentsInChildren<Renderer>(true);

        foreach(Renderer targetRenderer in renderers)
        {
            Material[] materials = targetRenderer.sharedMaterials;
            bool materialsChanged = false;

            for(int i = 0; i < materials.Length; i++)
            {
                Material sourceMaterial = materials[i];

                if (!sourceMaterial || sourceMaterial.name != ObjectsMaterialName) continue;
                //유닛 전용 마테리얼은 최초 한번만 생성
                if (!objectsMaterialInstance)
                {
                    objectsMaterialInstance = new Material(sourceMaterial);
                    objectsMaterialInstance.name = $"{ObjectsMaterialName}_Runtime";
                    objectsMaterialInstance.SetColor(Color1Id, CreateRandomColor());
                    objectsMaterialInstance.SetColor(Color2Id, CreateRandomColor());
                    objectsMaterialInstance.SetColor(Color3Id, CreateRandomColor());

                }
                //모든 Rendere에 동일한 인스턴스 연결
                materials[i] = objectsMaterialInstance;
                materialsChanged = true;
            }
            //반복문 후 한번만 적용
            if (materialsChanged) targetRenderer.sharedMaterials = materials;
        }

        if (!objectsMaterialInstance)
        {
            Debug.LogError($"[MaleUnitAppearance] '{ObjectsMaterialName}'을 찾지 못했습니다.", this);
            return false;
        }
        return true;
    }

    static Color CreateRandomColor()
    {
        return Random.ColorHSV(0f, 1f, 0.35f, 1f, 0.35f, 1f);
    }

    static Transform SetOnlyRandomChildActive(Transform group, bool allowNone)
    {
        int selectedIndex = allowNone
            ? Random.Range(-1, group.childCount)
            : Random.Range(0, group.childCount);

        for (int i = 0; i < group.childCount; i++)
        {
            group.GetChild(i).gameObject.SetActive(i == selectedIndex);
        }

        return selectedIndex >= 0 ? group.GetChild(selectedIndex) : null;
    }

    static void RandomizeEachChild(Transform group, float percent)
    {
        for (int i = 0; i < group.childCount; i++)
        {
            group.GetChild(i).gameObject.SetActive(Random.value < percent);
        }
    }

    static void RandomizeDecorations(GameObject[] decorations)
    {
        if (decorations is null)
        {
            Debug.LogWarning($"[MaleUnitAppearance] 장식들을 찾지 못했습니다.");
            return;
        }

        foreach (GameObject decoration in decorations)
        {
            if (!decoration)
            {
                Debug.LogWarning($"[MaleUnitAppearance] '{decoration.name}'을 찾지 못했습니다.");
                continue;
            }

            decoration.gameObject.SetActive(Random.value < 0.5f);
        }
    }

    static bool RandomizeHeadDecoration(GameObject headDecoration, float percent)
    {
        if (!headDecoration)
        {
            Debug.LogWarning($"[MaleUnitAppearance] 머리 장식을 찾지 못했습니다.");
            return false;
        }

        bool isActive = Random.value < percent;
        headDecoration.SetActive(isActive);
        return isActive;
    }

    void OnDestroy()
    {
        if (objectsMaterialInstance)
        {
            Destroy(objectsMaterialInstance);
        }
    }
}
