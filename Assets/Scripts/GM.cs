using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using UnityEditor;
using Photon.Pun.Demo.PunBasics;
using Unity.VisualScripting;
using System.Linq;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
// github change
//�̰� �����Ϳ����� ���ư����ϰ� ���ӻ��� ����ȭ�� RPC�� �ϰԲ� �����߰ڴ�.
public class GM : MonoBehaviourPunCallbacks
{
    [Header("Game Info")]
    public Text SubjectText;
    public Text WordText;
    public Text TimerText;
    public Text Announcement;

    [Header("Chat")]
    public InputField ChatInput;
    public GameObject ChatPrefab;
    public GameObject ChatWindow;
    public Button SkipTurnBtn;
   

    [Header("Photon")]
    public PhotonView PV;

    [Header("Player")]
    public Button[] PlayerCells;
    public Button[] VoteButtons;
    public GameObject PlayerPrefab;
    public GameObject PlayerSpawn;
    public Button[] FinalVoteButtons;
    public InputField AnswerInput;
    public Button AnswerSendButton;
    public Button ToLobbyButton;

    [Header("DB")]
    public Database DB;

    [Header("Debug")]
    public Text DebuggingText;


    private List<GameObject> gamePlayers = new List<GameObject>();
    private string[] playerColors = {"red", "yellow", "green", "cyan", "blue", "purple", "magenta", "white" };
    private Dictionary<string, int> playerID = new Dictionary<string, int>();

    // �� �� �ʱ�ȭ �ؾߵǴ� �ֵ�
    private string AnswerSubject;
    private string AnswerWord;
    private int LiarID;
    private string LiarName;
    private float timerTime;
    private int currentTurn;
    public State state;
    private int[] voteStatus = new int[8];
    private int voteResult;
    private int voteResultID;
    private bool isAgree;
    private int[] FinalVoteStatus = new int[2]; // 0���� ����(���̾�) 1���� ����(�ù�)
    private bool isAnswer = false;

    private void InitializeEverything()
    {
        SubjectText.text = "";
        WordText.text = "";
        AnswerSubject = "";
        AnswerWord = "";
        LiarID = -1;
        LiarName = "";
        currentTurn = 0;
        for(int i = 0; i < 8; i++) { voteStatus[i] = 0; }
        voteResult = 0; 
        voteResultID = -1;
        isAgree = false;
        FinalVoteStatus[0] = 0;
        FinalVoteStatus[1] = 0;
        isAnswer = false;

        ChatInput.interactable = false;
    }
    private void ShowScore()
    {
        for(int i = 0;i<gamePlayers.Count;i++)
        {
            PlayerCells[i].transform.GetChild(2).GetComponent<Text>().text = gamePlayers[i].GetComponent<GamePlayer>().score.ToString() + "/10";
        }
    }
    public enum State
    {
        gameReady, selectLiar, describe, vote, argue, revote, showResult, gameOver
    }

    private void Update()
    {
        //DebuggingText.text = "LiarName:" + LiarName + ", LiarID:" + LiarID +", ����:"+AnswerSubject + ", ���þ�:" +AnswerWord;
    }

    private void Start()
    {
        InitializeEverything();
        SetPlayer();
        state = State.gameReady;
        StartCoroutine(GameSchedular());

    }
    IEnumerator GameSchedular()
    {
        switch (state)
        {
            case State.gameReady: 
                timerTime = 10;
                InitializeEverything();
                ShowScore();
                break;

            case State.selectLiar:
                timerTime = 10;
                SelectLiar();
                break;

            case State.describe:
                timerTime = 30;
                // ���� ���� Ŭ���̾�Ʈ�� ��ũ���̺� ����
                if(playerID[PhotonNetwork.LocalPlayer.NickName] == currentTurn)
                {
                    Describe();
                }
                else
                {
                    StayStill();
                }
                break;

            case State.vote:
                timerTime = 15;
                // ��ǥ
                Vote();
                break;

            case State.argue:
                timerTime = 20;
                // ��ǥ���
                GetVoteResult();
                // ������ǥ
                FinalVote();
                break;

            case State.revote:
                timerTime = 20;
                // ��ǥ
                Vote();
                break;

            case State.showResult:
                timerTime = 20;
                ShowResult();
                break;
        }
        while (timerTime > 0)
        {
            timerTime--;
            TimerText.text = timerTime.ToString();
            if (state == State.gameReady)
            {
                Announcement.text = "������ " + timerTime.ToString() + "�� �ڿ� �����մϴ�.";
            }
            else if (state == State.selectLiar)
            {
                Announcement.text = "Liar�� ���þ ���� ���Դϴ�.";
            }
            else if (state == State.describe)
            {
                Announcement.text = gamePlayers[currentTurn].GetComponent<GamePlayer>().nickName + "���� ���þ �������ּ���.";

            }
            else if (state == State.vote)
            {
                Announcement.text = "���� Liar�� �� ������ ��ǥ�ϼ���.";
            }
            else if (state == State.argue)
            {
                Announcement.text = gamePlayers[voteResultID].GetComponent<GamePlayer>().nickName + "���� Liar�� ����Ǿ����ϴ�. ���ĺ����� �Ͻʽÿ�.";
            }
            else if (state == State.revote)
            {
                Announcement.text = "���� Liar�� �� ������ �ٽ� ��ǥ�ϼ���.";
            }


            yield return new WaitForSecondsRealtime(1.0f);
        }
        // Ÿ�̸� ������
        ChangeGameStateFrom(state);
        
    }

    private void ChangeGameStateFrom(State currentState)
    {
        if(currentState == State.gameReady)
        {
            state = State.selectLiar;
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.selectLiar)
        {
            state = State.describe;
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.describe)
        {
            // ������ �÷��̾� ���� �������� ���� ������Ʈ��
            if(currentTurn == gamePlayers.Count - 1)
            {
                state = State.vote;
            }
            else
            {
                // �ƴϸ� �ϸ� �ö󰡰� ������Ʈ�� �״��
                currentTurn++;
            }
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.vote)
        {
            state = State.argue;
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.argue)
        {
            GetFinalVoteResult();
            if(isAgree)
            {
                state = State.showResult;
            }
            else
            {
                state = State.revote;
            }
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.revote)
        {
            state = State.showResult;
            StartCoroutine(GameSchedular());
        }
        else if(currentState == State.showResult)
        {
            bool isWinner = false;
            // 10�� �̻� �޼��� ����� ������ ���� ǥ�� �� ���� ����
            for(int i = 0; i < gamePlayers.Count; i++)
            {
                if (gamePlayers[i].GetComponent<GamePlayer>().score >= 10)
                {
                    isWinner = true;
                }
            }

            if(isWinner)
            {
                Announcement.text = "���ڰ� ���������ϴ�. �����մϴ�!";
                state = State.gameOver;
                ToLobbyButton.gameObject.SetActive(true);
            }
            // ������ ������ �ʱ�ȭ ��, �� �� �� ����
            else
            {
                state = State.gameReady;
            }
            StartCoroutine(GameSchedular());
        }
    }

    private void SetPlayer()
    {
        int numberOfPlayers = PhotonNetwork.PlayerList.Length;
        Debug.Log("numofp = " + numberOfPlayers);
        for (int i = 0; i < numberOfPlayers; i++)
        {
            // �÷��̾� ������Ʈ ����, �÷��̾� ����Ʈ�� ���ӸŴ����� ����
            GameObject player = Instantiate(PlayerPrefab, PlayerSpawn.transform);

            player.GetComponent<GamePlayer>().nickName = PhotonNetwork.PlayerList[i].NickName;
            player.GetComponent<GamePlayer>().color = playerColors[i];
            player.GetComponent<GamePlayer>().isTurn = (i == 0) ? true : false;
            gamePlayers.Add(player);
            
            playerID.Add(player.GetComponent<GamePlayer>().nickName, i);

            // �÷��̾ UI Ȱ��ȭ
            PlayerCells[i].interactable = true;
            PlayerCells[i].transform.GetChild(0).GetComponent<Text>().text = player.GetComponent<GamePlayer>().nickName;
            //PlayerCells[i].transform.GetChild(1).GetComponent<Text>().text = player.GetComponent<GamePlayer>().isTurn ? "�÷��̾� ��" : "";
            PlayerCells[i].transform.GetChild(2).GetComponent<Text>().text = player.GetComponent<GamePlayer>().score.ToString() + "/10";

        }

    }

    #region ��������
    // �����Ϳ��� �� �Լ��� ���� �����ϰ� RPC�� ����ȭ
    private void SelectLiar()
    {
        // UI ���� ������ ���������� ��������
        if(PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            // ����, ���þ� ����
            AnswerSubject = DB.GetRandomSubject();
            AnswerWord = DB.GetRandomWord(AnswerSubject);

            // ���̾� ����
            LiarID = Random.Range(0, gamePlayers.Count);
            LiarName = gamePlayers[LiarID].GetComponent<GamePlayer>().nickName;
            Debug.Log("player count: " + gamePlayers.Count);
            gamePlayers[LiarID].GetComponent<GamePlayer>().isLiar = true;

            PV.RPC("SyncLiar", RpcTarget.All, AnswerSubject, AnswerWord, LiarID, LiarName);
        }
    }
    [PunRPC] private void SyncLiar(string answerSubject, string answerWord, int liarID, string liarName)
    {
        // �� Ŭ���̾�Ʈ�� �������� ������ Ŭ��� ����ȭ
        AnswerSubject = answerSubject;
        AnswerWord = answerWord;
        LiarID = liarID;
        LiarName = liarName;
        // ����ȭ�� ������ UI ǥ��
        if(PhotonNetwork.LocalPlayer.NickName.Equals(LiarName))
        {
            WordText.text = "����� <color=red>Liar</color>�Դϴ�.";
        }
        else
        {
            WordText.text = "���þ�� <color=red>"+AnswerWord +" </color>�Դϴ�";
        }
        SubjectText.text = "����: <color=blue>" + AnswerSubject + "</color>";
    }

    private void Describe()
    {
        ChatInput.interactable = true;
        SkipTurnBtn.interactable = true;
        PV.RPC("ShowTurn", RpcTarget.All);
    }
    private void StayStill()
    {
        ChatInput.interactable = false;
        SkipTurnBtn.interactable = false;
        PV.RPC("ShowTurn", RpcTarget.All);
    }
    [PunRPC] private void ShowTurn()
    {
        for(int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            PlayerCells[i].transform.GetChild(1).GetComponent<Text>().text = "";
        }
        PlayerCells[currentTurn].transform.GetChild(1).GetComponent<Text>().text = "�÷��̾� ��";
    }
    public void OnClickSkipTurn()
    {
        PV.RPC("SkipTurn", RpcTarget.All);
    }
    [PunRPC] private void SkipTurn()
    {
        timerTime = 2;
    }

    private void Vote()
    {
        // ��ǥ�� �ʱ�ȭ
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            voteStatus[i] = 0;
        }
        // ä�� ���
        ChatInput.interactable = true;
        for (int i=0; i< PhotonNetwork.PlayerList.Length; i++)
        {
            VoteButtons[i].gameObject.SetActive(true);
        }
    }
    public void OnClickVote(int n)
    {
        // ��ǥ���� ����
        PV.RPC("SyncVote", RpcTarget.All, n);
        // ��ǥ ��Ȱ��ȭ
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            VoteButtons[i].gameObject.SetActive(false);
        }
    }
    [PunRPC] private void SyncVote(int n)
    {
        voteStatus[n]++;
    }

    private void GetVoteResult()
    {
        int max = 0; // �ִ� ��ǥ��
        for (int i = 0; i < voteStatus.Length; i++)
        {
            if (voteStatus[i] > max)
            {
                max = voteStatus[i];
                voteResultID = i;
            }
            voteResult = max;
        }
    }

    private void FinalVote()
    {
        // ���� ���� �ʱ�ȭ
        FinalVoteStatus[0] = 0;
        FinalVoteStatus[1] = 0;
        // ��ư Ȱ��ȭ
        FinalVoteButtons[0].gameObject.SetActive(true);
        FinalVoteButtons[1].gameObject.SetActive(true);
    }
    public void OnClickAgreeBtn(int n)
    {
        // ��ǥ���� ����
        PV.RPC("SyncFinalVote", RpcTarget.All, n);
        // ��ǥ ��Ȱ��ȭ
        FinalVoteButtons[0].gameObject.SetActive(false);
        FinalVoteButtons[1].gameObject.SetActive(false);
    }
    [PunRPC]
    private void SyncFinalVote(int n)
    {
        FinalVoteStatus[n]++;
    }
    private void GetFinalVoteResult()
    {
        if (FinalVoteStatus[0] >= FinalVoteStatus[1])
        {
            isAgree = true;
        }
        else
            isAgree = false;
    }

    private void ShowResult()
    {
        // ��ǥ����� ���̾ �´°��
        if(voteResultID == LiarID)
        {
            Announcement.text = LiarName + "���� <color=red>Liar</color>�� �½��ϴ�. ���þ �����ּ���.";
            if(PhotonNetwork.LocalPlayer.NickName.Equals(LiarName))
            {
                AnswerInput.text = "";
                AnswerInput.gameObject.SetActive(true);
                AnswerSendButton.gameObject.SetActive(true);
            }
            
        }
        // ���̾ �ƴ� ���
        else
        {
            Announcement.text = gamePlayers[voteResultID].GetComponent<GamePlayer>().nickName + "���� <color=red>Liar</color>�� �ƴմϴ�.\n" +
                LiarName + "���� <color=red>Liar</color>�����ϴ�.";
            // ���̾� �÷��̾� 2�� ����
            gamePlayers[LiarID].GetComponent<GamePlayer>().score += 2;
        }
    }
    public void OnClickAnswerSend()
    {
        PV.RPC("AnswerSend", RpcTarget.All, AnswerInput.text);
    }
    [PunRPC] private void AnswerSend(string liarAnswer)
    {

        if (liarAnswer.Equals(AnswerWord))
        {
            isAnswer = true;
            Announcement.text = "<color=red>Liar</color>�� ���þ ������ϴ�. <color=red>Liar</color> �¸�!";
            gamePlayers[LiarID].GetComponent<GamePlayer>().score += 2;
        }
        else
        {
            isAnswer = false;
            Announcement.text = "<color=red>Liar</color>�� ���þ ������ ���߽��ϴ�. <color=blue>�ù�</color> �¸�!";
            for (int i = 0; i < gamePlayers.Count; i++)
            {
                if (i != LiarID)
                    gamePlayers[i].GetComponent<GamePlayer>().score += 2;
            }
        }
        AnswerInput.text = "";
        AnswerInput.gameObject.SetActive(false);
        AnswerSendButton.gameObject.SetActive(false);
    }
    #endregion
    #region ä��
    public void Send()
    {
        string sender = PhotonNetwork.LocalPlayer.NickName;
        string msg = "<color="+ playerColors[playerID[sender]] +">" + sender +"</color>"+ " : " + ChatInput.text;
        PV.RPC("ChatRPC", RpcTarget.All, msg);
        ChatInput.text = "";
    }
    [PunRPC] // RPC�� �÷��̾ ���� �ִ� �� ��� �ο����� �����Ѵ�.
    void ChatRPC(string msg)
    {
        GameObject chat = Instantiate(ChatPrefab, ChatWindow.transform);
        chat.GetComponentInChildren<Text>().text = msg;
    }

    #endregion

    
}
