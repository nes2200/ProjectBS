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

    public int[] solution(string today, string[] terms, string[] privacies)
    {
        List<int> answer = new();

        Dictionary<string, int> dic = new();
        for(int i = 0; i < terms.Length; i++)
        {
            string[] term = terms[i].Split(' ');
            dic.Add(term[0], int.Parse(term[1]));
        }
        for(int i = 0; i < privacies.Length; i++)
        {
            //dateTime ¾²±â
            

        }

        return answer.ToArray();
    }
}