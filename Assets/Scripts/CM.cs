using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;

public class CM : MonoBehaviourPunCallbacks
{
    [Header ("Panels")]
    public GameObject connectPanel;
    public GameObject lobbyPanel;
    public GameObject roomPanel;
    public GameObject GamePanel;

    [Header("UI")]
    public Text ConnectionStatus;
    public Text MasterText;
    public Text LobbyInfoText;
    public Text IDtext;
    public Text RoomName;
    public Text MemberNumberText;
    public Button SendBtn;
    public Button connectBtn;
    public Button StartGameBtn;
    public InputField ChatInput;

    [Header("Room")]
    public Button[] CellBtn;
    public Button PreviousBtn;
    public Button NextBtn;

    [Header("Prefabs")]
    public GameObject ChatPrefab;
    public GameObject MemberPrefab;

    [Header("Window")]
    public GameObject ChatWindow;
    public GameObject MemberWindow;

    [Header("Photon")]
    public PhotonView PV;

    [Header("Manager")]
    public GameObject GameManager;
    public GameObject AudioManager;

    
    private void Awake()
    {
        Screen.SetResolution(960, 540, false); // 창 모드로 띄우기
        connectPanel.SetActive(true);
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
        GamePanel.SetActive(false);
        GameManager.SetActive(false);
    }
    private void Update()
    {
        // 실시간 네트워크 연결 상황 확인
        ConnectionStatus.text = PhotonNetwork.NetworkClientState.ToString();
        LobbyInfoText.text = (PhotonNetwork.CountOfPlayers - PhotonNetwork.CountOfPlayersInRooms) + "로비 / " + PhotonNetwork.CountOfPlayers + "접속";
    }

    #region 방 리스트 갱신
    List<RoomInfo> myList = new List<RoomInfo>();
    int currentPage = 1, maxPage, multiple;
    public void MyListClick(int num)
    {
        if (num == -2) --currentPage;
        else if (num == -1) ++currentPage;
        else PhotonNetwork.JoinRoom(myList[multiple + num].Name);
        MyListRenewal();
    }
    void MyListRenewal()
    {
        // 최대페이지
        maxPage = (myList.Count % CellBtn.Length == 0) ? myList.Count / CellBtn.Length : myList.Count / CellBtn.Length + 1;

        // 이전, 다음버튼
        PreviousBtn.interactable = (currentPage <= 1) ? false : true;
        NextBtn.interactable = (currentPage >= maxPage) ? false : true;

        // 페이지에 맞는 리스트 대입
        multiple = (currentPage - 1) * CellBtn.Length;
        for (int i = 0; i < CellBtn.Length; i++)
        {
            CellBtn[i].interactable = (multiple + i < myList.Count) ? true : false;
            CellBtn[i].transform.GetChild(0).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].Name : "";
            CellBtn[i].transform.GetChild(1).GetComponent<Text>().text = (multiple + i < myList.Count) ? myList[multiple + i].PlayerCount + "/" + myList[multiple + i].MaxPlayers : "";
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        int roomCount = roomList.Count;
        for (int i = 0; i < roomCount; i++)
        {
            if (!roomList[i].RemovedFromList)
            {
                if (!myList.Contains(roomList[i])) myList.Add(roomList[i]);
                else myList[myList.IndexOf(roomList[i])] = roomList[i];
            }
            else if (myList.IndexOf(roomList[i]) != -1) myList.RemoveAt(myList.IndexOf(roomList[i]));
        }
        MyListRenewal();
    }
    #endregion

    #region 서버 연결
    public void Connect() => PhotonNetwork.ConnectUsingSettings(); // 포톤 네트워크를 연결하는 함수로 사용
    public override void OnConnectedToMaster()
    {
        print("서버 접속 완료");
        PhotonNetwork.LocalPlayer.NickName = IDtext.text;
        PhotonNetwork.JoinLobby();
    }
    public override void OnJoinedLobby()
    {
        print("로비 접속 완료");
        connectPanel.SetActive(false);
        lobbyPanel.SetActive(true);
    }

    public void Disconnect() => PhotonNetwork.Disconnect();
    public override void OnDisconnected(DisconnectCause cause)
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(false);
        connectPanel.SetActive(true);
    }
    #endregion
    
    #region 방
   
    public override void OnJoinedRoom()
    {
        lobbyPanel.SetActive(false);
        roomPanel.SetActive(true);
        RoomRenewal();
        ChatInput.text = "";
    }
    public void CreateRoom() => PhotonNetwork.CreateRoom(RoomName.text == "" ? "Room" + Random.Range(0, 100) : RoomName.text, new RoomOptions { MaxPlayers = 8 });
    public void LeaveRoom() => PhotonNetwork.LeaveRoom();

    public override void OnLeftRoom()
    {
        // 내가 나갔을때 내쪽에서 부르는 함수
        ClearChat();
        lobbyPanel.SetActive(true);
        roomPanel.SetActive(false);
        GamePanel.SetActive(false);
        GameManager.SetActive(false);
        RoomName.text = "";
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        RoomName.text = "";
        CreateRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        // 남이 들어왔을때 내쪽에서 부르는 함수
        RoomRenewal();
        ChatRPC("<color=yellow>" + newPlayer.NickName + "님이 입장하셨습니다</color>");
    }
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        // 남이 나갔을때 내쪽에서 부르는 함수
        RoomRenewal();
        ChatRPC("<color=yellow>" + otherPlayer.NickName + "님이 퇴장하셨습니다</color>");
    }
    void RoomRenewal()
    {
        // 접속한 멤버 초기화하기
        ClearMember();

        int numberOfPlayers = PhotonNetwork.PlayerList.Length;
        for (int i = 0; i < numberOfPlayers; i++)
        {
            // 접속한 멤버 띄우기
            GameObject member = Instantiate(MemberPrefab, MemberWindow.transform);
            member.GetComponentInChildren<Text>().text = PhotonNetwork.PlayerList[i].NickName;
        }
        // 접속 명수 띄우기
        MemberNumberText.text = "접속자 " + numberOfPlayers + "/8";
        // 누가 방장인지 띄우기, 방장만 게임시작 활성화
        if (PhotonNetwork.LocalPlayer.IsMasterClient)
        {
            MasterText.text = "You are master of this room AC: " + PhotonNetwork.LocalPlayer.ActorNumber;
            StartGameBtn.interactable = true;
        }
        else
        {
            MasterText.text = "You are member of this room, ActorNumber: " + PhotonNetwork.LocalPlayer.ActorNumber;
            StartGameBtn.interactable = false;
        }
        
    }
    void ClearMember()
    {
        Transform[] childList = MemberWindow.GetComponentsInChildren<Transform>();
        if(childList != null)
        {
            for(int i=1; i<childList.Length; i++)
            {
                if (childList[i] != transform)
                    Destroy(childList[i].gameObject);
            }
        }
    }
    void ClearChat()
    {
        Transform[] childList = ChatWindow.GetComponentsInChildren<Transform>();
        if (childList != null)
        {
            for (int i = 1; i < childList.Length; i++)
            {
                if (childList[i] != transform)
                    Destroy(childList[i].gameObject);
            }
        }
    }
    #endregion

    #region 채팅
    public void Send()
    {
        string msg = PhotonNetwork.NickName + " : " + ChatInput.text;
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

    #region 게임
    public void OnClickStartGame()
    {
        PV.RPC("StartGame", RpcTarget.All);
    }

    [PunRPC]
    public void StartGame()
    {
        roomPanel.SetActive(false);
        GamePanel.SetActive(true);
        GameManager.SetActive(true);
        AudioManager.SetActive(true);
    }
    #endregion
}
