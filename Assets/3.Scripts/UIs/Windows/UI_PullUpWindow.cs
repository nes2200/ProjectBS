using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;


public class UI_PullUpWindow : MonoBehaviour, IPointerDownHandler
{
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
    }


    public int solution(int[] ingredient)
    {
        List<int> stack = new();
        for(int i = 0; i < ingredient.Length; i++)
        {
            stack.Add(ingredient[i]);
            if (stack.Count < 4) continue;
        }
        return 0; 
    }
}