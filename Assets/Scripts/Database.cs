using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Database : MonoBehaviour
{
    public string GetRandomSubject()
    {
        string[] subjects = { "����", "����", "����" };
        int randomNumber = Random.Range(0, 2);
        return subjects[randomNumber];
    }

    public string GetRandomWord(string subject)
    {
        if (subject.Equals("����"))
        {
            string[] words = { "�ܹ���", "¥���", "�Ľ�Ÿ", "Ÿ��", "�ʹ�", "ī��", "��ġ������", "��ġ�", "������", "����" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        else if(subject.Equals("����"))
        {
            string[] words = { "�ϱذ�", "�縷����", "����", "����", "ȣ����", "ī�ǹٶ�", "Ļ�ŷ�", "����Ǳ�", "��ѱ�", "�����" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        else if(subject.Equals("����"))
        {
            string[] words = { "�ǻ�", "���డ", "��������", "����ġ���", "�¹���", "��ȣ��", "��ȸ������", "�缭", "ȭ��", "������" };
            int randomNumber = Random.Range(0, words.Length - 1);
            return words[randomNumber];
        }
        return "�ش���������";
    }
}
