using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Database : MonoBehaviour
{
    public string GetRandomSubject()
    {
        string[] subjects = { "음식", "동물", "직업" };
        int randomNumber = Random.Range(0, 2);
        return subjects[randomNumber];
    }

    public string GetRandomWord(string subject)
    {
        if (subject.Equals("음식"))
        {
            string[] words = { "단무지", "짜장면", "파스타", "타코", "초밥", "카레", "김치볶음밥", "김치찌개", "떡볶이", "족발" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        else if(subject.Equals("동물"))
        {
            string[] words = { "북극곰", "사막여우", "무스", "범고래", "호랑이", "카피바라", "캥거루", "기니피그", "비둘기", "고양이" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        else if(subject.Equals("직업"))
        {
            string[] words = { "의사", "건축가", "경제학자", "물리치료사", "승무원", "간호사", "사회복지사", "사서", "화가", "연예인" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        return "해당주제없음";
    }
}
