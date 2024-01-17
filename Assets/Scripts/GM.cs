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
//이거 마스터에서만 돌아가게하고 게임상태 동기화를 RPC로 하게끔 만들어야겠다.
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

    // 매 판 초기화 해야되는 애들
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
    private int[] FinalVoteStatus = new int[2]; // 0번이 동의(라이어) 1번이 비동의(시민)
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
        //DebuggingText.text = "LiarName:" + LiarName + ", LiarID:" + LiarID +", 주제:"+AnswerSubject + ", 제시어:" +AnswerWord;
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
                // 본인 턴인 클라이언트만 디스크라이브 진행
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
                // 투표
                Vote();
                break;

            case State.argue:
                timerTime = 20;
                // 투표결과
                GetVoteResult();
                // 최종투표
                FinalVote();
                break;

            case State.revote:
                timerTime = 20;
                // 투표
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
                Announcement.text = "게임이 " + timerTime.ToString() + "초 뒤에 시작합니다.";
            }
            else if (state == State.selectLiar)
            {
                Announcement.text = "Liar와 제시어를 고르는 중입니다.";
            }
            else if (state == State.describe)
            {
                Announcement.text = gamePlayers[currentTurn].GetComponent<GamePlayer>().nickName + "님은 제시어를 설명해주세요.";

            }
            else if (state == State.vote)
            {
                Announcement.text = "누가 Liar일 것 같은지 투표하세요.";
            }
            else if (state == State.argue)
            {
                Announcement.text = gamePlayers[voteResultID].GetComponent<GamePlayer>().nickName + "님이 Liar로 지목되었습니다. 최후변론을 하십시오.";
            }
            else if (state == State.revote)
            {
                Announcement.text = "누가 Liar일 것 같은지 다시 투표하세요.";
            }


            yield return new WaitForSecondsRealtime(1.0f);
        }
        // 타이머 끝나면
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
            // 마지막 플레이어 턴이 끝났으면 다음 스테이트로
            if(currentTurn == gamePlayers.Count - 1)
            {
                state = State.vote;
            }
            else
            {
                // 아니면 턴만 올라가고 스테이트는 그대로
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
            // 10점 이상 달성한 사람이 있으면 승자 표기 후 게임 종료
            for(int i = 0; i < gamePlayers.Count; i++)
            {
                if (gamePlayers[i].GetComponent<GamePlayer>().score >= 10)
                {
                    isWinner = true;
                }
            }

            if(isWinner)
            {
                Announcement.text = "승자가 정해졌습니다. 축하합니다!";
                state = State.gameOver;
                ToLobbyButton.gameObject.SetActive(true);
            }
            // 없으면 변수들 초기화 후, 한 판 더 진행
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
            // 플레이어 오브젝트 생성, 플레이어 리스트를 게임매니저가 관리
            GameObject player = Instantiate(PlayerPrefab, PlayerSpawn.transform);

            player.GetComponent<GamePlayer>().nickName = PhotonNetwork.PlayerList[i].NickName;
            player.GetComponent<GamePlayer>().color = playerColors[i];
            player.GetComponent<GamePlayer>().isTurn = (i == 0) ? true : false;
            gamePlayers.Add(player);
            
            playerID.Add(player.GetComponent<GamePlayer>().nickName, i);

            // 플레이어셀 UI 활성화
            PlayerCells[i].interactable = true;
            PlayerCells[i].transform.GetChild(0).GetComponent<Text>().text = player.GetComponent<GamePlayer>().nickName;
            //PlayerCells[i].transform.GetChild(1).GetComponent<Text>().text = player.GetComponent<GamePlayer>().isTurn ? "플레이어 턴" : "";
            PlayerCells[i].transform.GetChild(2).GetComponent<Text>().text = player.GetComponent<GamePlayer>().score.ToString() + "/10";

        }

    }

    #region 게임진행
    // 마스터에서 이 함수로 게임 세팅하고 RPC로 동기화
    private void SelectLiar()
    {
        // UI 갱신 마스터 전역변수값 기준으로
        if(PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            // 주제, 제시어 선정
            AnswerSubject = DB.GetRandomSubject();
            AnswerWord = DB.GetRandomWord(AnswerSubject);

            // 라이어 선정
            LiarID = Random.Range(0, gamePlayers.Count);
            LiarName = gamePlayers[LiarID].GetComponent<GamePlayer>().nickName;
            Debug.Log("player count: " + gamePlayers.Count);
            gamePlayers[LiarID].GetComponent<GamePlayer>().isLiar = true;

            PV.RPC("SyncLiar", RpcTarget.All, AnswerSubject, AnswerWord, LiarID, LiarName);
        }
    }
    [PunRPC] private void SyncLiar(string answerSubject, string answerWord, int liarID, string liarName)
    {
        // 내 클라이언트의 전역변수 마스터 클라랑 동기화
        AnswerSubject = answerSubject;
        AnswerWord = answerWord;
        LiarID = liarID;
        LiarName = liarName;
        // 동기화된 정보로 UI 표시
        if(PhotonNetwork.LocalPlayer.NickName.Equals(LiarName))
        {
            WordText.text = "당신은 <color=red>Liar</color>입니다.";
        }
        else
        {
            WordText.text = "제시어는 <color=red>"+AnswerWord +" </color>입니다";
        }
        SubjectText.text = "주제: <color=blue>" + AnswerSubject + "</color>";
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
        PlayerCells[currentTurn].transform.GetChild(1).GetComponent<Text>().text = "플레이어 턴";
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
        // 투표함 초기화
        for (int i = 0; i < PhotonNetwork.PlayerList.Length; i++)
        {
            voteStatus[i] = 0;
        }
        // 채팅 허용
        ChatInput.interactable = true;
        for (int i=0; i< PhotonNetwork.PlayerList.Length; i++)
        {
            VoteButtons[i].gameObject.SetActive(true);
        }
    }
    public void OnClickVote(int n)
    {
        // 투표용지 제출
        PV.RPC("SyncVote", RpcTarget.All, n);
        // 투표 비활성화
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
        int max = 0; // 최다 득표수
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
        // 동의 비동의 초기화
        FinalVoteStatus[0] = 0;
        FinalVoteStatus[1] = 0;
        // 버튼 활성화
        FinalVoteButtons[0].gameObject.SetActive(true);
        FinalVoteButtons[1].gameObject.SetActive(true);
    }
    public void OnClickAgreeBtn(int n)
    {
        // 투표용지 제출
        PV.RPC("SyncFinalVote", RpcTarget.All, n);
        // 투표 비활성화
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
        // 투표결과가 라이어가 맞는경우
        if(voteResultID == LiarID)
        {
            Announcement.text = LiarName + "님은 <color=red>Liar</color>가 맞습니다. 제시어를 맞춰주세요.";
            if(PhotonNetwork.LocalPlayer.NickName.Equals(LiarName))
            {
                AnswerInput.text = "";
                AnswerInput.gameObject.SetActive(true);
                AnswerSendButton.gameObject.SetActive(true);
            }
            
        }
        // 라이어가 아닌 경우
        else
        {
            Announcement.text = gamePlayers[voteResultID].GetComponent<GamePlayer>().nickName + "님은 <color=red>Liar</color>가 아닙니다.\n" +
                LiarName + "님이 <color=red>Liar</color>였습니다.";
            // 라이어 플레이어 2점 득점
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
            Announcement.text = "<color=red>Liar</color>가 제시어를 맞췄습니다. <color=red>Liar</color> 승리!";
            gamePlayers[LiarID].GetComponent<GamePlayer>().score += 2;
        }
        else
        {
            isAnswer = false;
            Announcement.text = "<color=red>Liar</color>가 제시어를 맞추지 못했습니다. <color=blue>시민</color> 승리!";
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
    #region 채팅
    public void Send()
    {
        string sender = PhotonNetwork.LocalPlayer.NickName;
        string msg = "<color="+ playerColors[playerID[sender]] +">" + sender +"</color>"+ " : " + ChatInput.text;
        PV.RPC("ChatRPC", RpcTarget.All, msg);
        ChatInput.text = "";
    }
    [PunRPC] // RPC는 플레이어가 속해 있는 방 모든 인원에게 전달한다.
    void ChatRPC(string msg)
    {
        GameObject chat = Instantiate(ChatPrefab, ChatWindow.transform);
        chat.GetComponentInChildren<Text>().text = msg;
    }

    #endregion

    
}
