using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine.UI;
using System.IO;
using System.Runtime.InteropServices;//마샬링을 위한 어셈블리
using System.Text.RegularExpressions;//Regex사용을 위한 어셈블리
using static NetworkManager_Server;
using System.Reflection;
using JetBrains.Annotations;
using System.Runtime.InteropServices.ComTypes;
using CommonData;
using static CommonDataClass;
using UnityEditor;
using TMPro;
using System.Linq;
using System.Net.Http;

/// <summary>
/// TCP 네트워크 모듈 클래스
/// </summary>
public class TCPNetworkModule
{
    public TcpClient clientSocket;//클라이언트 소켓(통신 통로)
    public NetworkStream stream;//클라이언트 통신 도구

    public int ClientID;//클라이언트 이름

    public string strPlayerName;//클라이언트 이름
    //public bool isMode; // Mode( true : 플레이어, false: 모니터링)

    //받은 데이터 저장공간
    public byte[] buffer;
    //받은 데이터가 잘릴 경우를 대비하여 임시버퍼에 저장하여 관리
    public byte[] tempBuffer;//임시버퍼
    public bool isTempByte;//임시버퍼 유무
    public int nTempByteSize;//임시버퍼의 크기

    public bool isReady; //준비완료눌렀는지

    public TCPNetworkModule(TcpClient clientSocket, int ClientID = 0)
    {
        this.clientSocket = clientSocket;
        this.stream = clientSocket.GetStream();

        this.ClientID = ClientID;

        //데이터 저장공간 초기화
        this.buffer = new byte[4098];
        //임시버퍼 초기화
        this.tempBuffer = new byte[4098];
        this.isTempByte = false;
        this.nTempByteSize = 0;


        

    }
}

/// <summary>
/// 네트워크 모듈 클래스(TCP + UDP)
/// </summary>
public class NetworkModule : TCPNetworkModule
{
    public IPEndPoint EndPointSend;//클라이언트 EndPoint
    public UdpClient udpClient;
    public ushort udpPort;//클라이언트의 port


    public NetworkModule(TcpClient clientSocket, ushort udpPort, int ClientID = 0) : base(clientSocket, ClientID)
    {
        this.udpPort = udpPort;
        IPEndPoint iPEndPoint = (IPEndPoint)clientSocket.Client.RemoteEndPoint;
        //tcpClient에서 받아온 값으로 주소값을 찾아 넣어주고 포트는 재할당
        EndPointSend = new IPEndPoint(iPEndPoint.Address, udpPort);
        udpClient = new UdpClient();
    }
}

public class NetworkManager_Server : MonoBehaviour
{
    public NetGameMgr netGameMgr;

    //쓰레드
    private Thread tcpListenerThread;
    private Thread udpListenerThread;

    private Thread MultiPlayerSyncThread;
    private Thread sendDestroyObjectMsgThread;
    private Thread sendBulletThrowMsgThread;
    //서버의 소켓
    private TcpListener tcpListener ;
    private UdpClient udpSocketReceive;//받을때까지 대기함으로 서버의 받는 UDP 소켓은 하나로 고정하자
    public IPEndPoint IPEndPointReceive;//모든 타겟

    //클라이언트 목록
    public List<NetworkModule> connectedClients;//연결된 클라이언트 목록
    public List<NetworkModule> disconnectedClients;//연결해제된 클라이언트 목록

    protected int udpPort;

    //서버 상태
    public bool serverReady;
    //클라이언트 ID 부여 번호
    //int ClientIDIndex = 0;

    //이벤트
    public Queue<NetworkModule> process_connectClient = new Queue<NetworkModule>();//클라이언트 연결 이벤트
    public Queue<NetworkModule> process_disconnectClient = new Queue<NetworkModule>();//클라이언트 연결 해제 이벤트

    public Queue<stPlayerDirectionMsg> process_PlayerDirection = new Queue<stPlayerDirectionMsg>();
    public Queue<stNumberMsg> process_PlayerJump = new Queue<stNumberMsg>();
    public Queue<int> readyPlayer = new Queue<int>();

    protected bool isRecivePlayerName = false;
    //public bool isReciveStartGameMsg = false;

    protected virtual void Awake()
    {
        udpPort = Constants.UDP_PORT_NUMBER - netGameMgr.thisStageNumber;
        //연결된 클라이언트 목록 초기화
        connectedClients = new List<NetworkModule>();
        //연결해제된 클라이언트 목록 초기화
        disconnectedClients = new List<NetworkModule>();
    }


    // Start is called before the first frame update
    protected virtual void Start()
    {

    }

    // Update is called once per frame
    protected virtual void Update()
    {

    }

    /// <summary>
    /// 게임 서버 동기화에 필요한 쓰레드를 생성 및 실행해줌
    /// </summary>
    protected void GameServerThreadStart()
    {
        // 접속된 클라이언트 정보를 전송하는 쓰레드
        MultiPlayerSyncThread = new Thread(new ThreadStart(SendMultiPlayerSyncDataUDP));
        MultiPlayerSyncThread.IsBackground = true;
        MultiPlayerSyncThread.Start();

        sendDestroyObjectMsgThread = new Thread(new ThreadStart(SendDestroyObjectMsg));
        sendDestroyObjectMsgThread.IsBackground = true;
        sendDestroyObjectMsgThread.Start();

        sendBulletThrowMsgThread = new Thread(new ThreadStart(SendBulletThrowInfo));
        sendBulletThrowMsgThread.IsBackground = true;
        sendBulletThrowMsgThread.Start();
    }

    /// <summary>
    /// 서버 생성 버튼
    /// </summary>
    public void ServerCreate()
    {
        // TCP서버 배경 스레드 시작
        tcpListenerThread = new Thread(new ThreadStart(ListenForIncommingRequeset));
        tcpListenerThread.IsBackground = true;
        tcpListenerThread.Start();

        udpListenerThread = new Thread(new ThreadStart(UdpListenForIncommingRequeset));
        udpListenerThread.IsBackground = true;
        udpListenerThread.Start();
    }
    /// <summary>
    /// 파괴된 오브젝트 정보를 날려주는 쓰레드
    /// </summary>
    public void SendDestroyObjectMsg()
    {
        try
        {
            while (true)
            {
                if (netGameMgr.destroyObjectMsg.Count > 0)
                {
                    stNumberMsg stLogInMsgData = netGameMgr.destroyObjectMsg.Dequeue();
                    BroadcastByte(GetNumberMsgToByte(stLogInMsgData));
                }
                Thread.Sleep(10);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// 불렛을 날리는 정보를 던지는 쓰레드
    /// </summary>
    public void SendBulletThrowInfo()
    {
        try
        {
            while (true)
            {
                if(Server.stBirdThrowList.Count > 0)
                {
                    stBirdThrowMsg stBirdThrowMsgData = Server.stBirdThrowList.Dequeue();
                    BroadcastByte(GetBirdThrowMsgToByte(stBirdThrowMsgData));
                }
                Thread.Sleep(20);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// 접속된 클라이언트 정보를 전송하는 쓰레드
    /// </summary>
    public void SendMultiPlayerSyncDataUDP()
    {
        int milliSecond = 1000 / Constants.FRAME_PER_SECOND;
        try
        {
            // 데이터 리시브 항시 대기(Update)
            while (true)
            {
                if (connectedClients.Count <= 0) continue;

                stMultiPlayerSyncMsg MultiPlayerSyncMsgData = new stMultiPlayerSyncMsg();
                MultiPlayerSyncMsgData.SetMultiPlayerInfo();

                for(int i=0; i< Constants.MAX_USER_COUNT; i++)
                {
                    MultiPlayerSyncMsgData.MultiPlayerInfo[i].position = netGameMgr.playerInfo[i].Position;
                    MultiPlayerSyncMsgData.MultiPlayerInfo[i].rotation = netGameMgr.playerInfo[i].Rotation;
                }



                //클라이언트 하나의 동기화 정보를
                foreach (NetworkModule client in connectedClients)
                {
                    if(client != null)
                    {
                        //전송
                        SendMsgUDP(client, GetMultiPlayerSyncMsgToByte(MultiPlayerSyncMsgData));
                    }
                    
                }
                //전송 주기
                Thread.Sleep(milliSecond);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }


    public void SendEndGameMsg()
    {
        //플레이어 움직임 비활성화
        //아직 연결된 클라이언트가 남아있다면
        if (connectedClients.Count > 0)
        {
            stHeader stHeaderMsgData = new stHeader();

            stHeaderMsgData.MsgID = MessageID_ServerToClient.ServerToClientID; // 메시지 ID
            stHeaderMsgData.MsgSubID = MessageID_ServerToClient.EndGame; // 메시지 ID
            stHeaderMsgData.PacketSize = (ushort)Marshal.SizeOf(stHeaderMsgData);//메시지 크기; // 나머지부분 메시지 크기

            //모든 클라이언트에게
            foreach (NetworkModule client2 in connectedClients)
            {
                if (client2 != null)
                {
                    //전송
                    SendMsg(client2, GetHeaderToByte(stHeaderMsgData));
                }

            }
        }
    }

    /// <summary>
    /// 서버 쓰레드 시작
    /// </summary>
    public void ListenForIncommingRequeset()
    {
        try
        {
            // 소켓 생성
            tcpListener = new TcpListener(IPAddress.Any/*서버에 접속 가능한 IP*/, Constants.TCP_PORT_NUMBER + netGameMgr.thisStageNumber);
            tcpListener.Start();

            // 서버 상태 ON
            serverReady = true;

            // 데이터 리시브 항시 대기(Update)
            while (true)
            {
                // 서버를 연적이 없다면
                if(!serverReady)
                    break;

                //연결 시도중인 클라이언트 확인
                if(tcpListener != null)
                {
                    
                    if(tcpListener.Pending())
                    {
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();
                        if (GameSingletonItems.g_State != GameState.Loading || connectedClients.Count >= Constants.MAX_USER_COUNT)
                        {
                            tcpClient.Close();
                            continue;
                        }
                        bool isMiddleEmpty = false;
                        int clientIdNumber = connectedClients.Count;
                        for (int i = 0; i < connectedClients.Count; i++)
                        {
                            if (connectedClients[i].ClientID != i)
                            {
                                //중간에 빠진 항목이 있다면 그곳에 추가
                                clientIdNumber = i;
                                isMiddleEmpty = true;
                                break;
                            }
                        }

                        //중간에 빠진 항목이 없다면 새로 추가
                        if (isMiddleEmpty == false)
                        {
                            //연결된 클라이언트 목록에 저장
                            connectedClients.Add(new NetworkModule(tcpClient, (ushort)(Constants.UDP_PORT_NUMBER + netGameMgr.thisStageNumber * Constants.MAX_USER_COUNT + clientIdNumber), clientIdNumber));
                            //클라이언트 연결 처리(UI)
                            process_connectClient.Enqueue(connectedClients[clientIdNumber]/*방금 추가된 클라이언트*/);
                        }
                        else
                        {
                            connectedClients.Insert(clientIdNumber, new NetworkModule(tcpClient, (ushort)(Constants.UDP_PORT_NUMBER + netGameMgr.thisStageNumber * Constants.MAX_USER_COUNT + clientIdNumber), clientIdNumber));
                            process_connectClient.Enqueue(connectedClients[clientIdNumber]/*방금 추가된 클라이언트*/);
                        }
                        ////연결된 클라이언트 목록에 저장
                        //connectedClients.Add(new TCPNetworkModule(tcpListener.AcceptTcpClient(), ClientIDIndex));
                        ////클라이언트 연결 처리(UI)
                        //process_connectClient.Enqueue(connectedClients[connectedClients.Count - 1]/*방금 추가된 클라이언트*/);

                        // 접속되는 숫자만큼 증가 시킨다. 설마 1000번 이상 접속하겠음? ㅎ
                        //ClientIDIndex++;
                    }
                    
                }

                //접속된 클라이언트 존재시 상호작용 처리
                foreach (NetworkModule client in connectedClients)
                {
                    if (client != null)
                    {
                        //클라이언트 접속 종료시
                        if (!IsConnected(client.clientSocket))
                        {
                            //클라이언트 연결 해제 처리(UI)
                            process_disconnectClient.Enqueue(client/*연결 해제된 클라이언트*/);

                            ///이곳에서 바로 클라이언트를 삭제하게 되면 쓰레드간의 딜레이 차이로 에러가 발생하므로 연결해제된 클라이언트 목록으로 관리한다.
                            //연결해제된 클라이언트 목록에 추가
                            disconnectedClients.Add(client);

                            continue;
                        }

                        //메시지가 들어왔다면
                        if (client.stream.DataAvailable)
                        {
                            //메시지 저장 공간 초기화
                            Array.Clear(client.buffer, 0, client.buffer.Length);

                            //메시지를 읽는다.
                            int messageLength = client.stream.Read(client.buffer, 0, client.buffer.Length);

                            //실제 처리하는 버퍼
                            byte[] pocessBuffer = new byte[messageLength + client.nTempByteSize];//지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                                                                                          //남았던 메시지가 있다면
                            if (client.isTempByte)
                            {
                                //앞 부분에 남았던 메시지 복사
                                Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                                //지금 읽은 메시지 복사
                                Array.Copy(client.buffer, 0, pocessBuffer, client.nTempByteSize, messageLength);
                            }
                            else
                            {
                                //남았던 메시지가 없으면 지금 읽어온 메시지를 저장
                                Array.Copy(client.buffer, 0, pocessBuffer, 0, messageLength);
                            }

                            //처리해야 하는 메시지의 길이가 0이 아니라면
                            if (client.nTempByteSize + messageLength > 0)
                            {
                                //받은 메시지 처리
                                OnIncomingData(client, pocessBuffer);
                            }
                        }
                        else if(client.nTempByteSize > 0)
                        {
                            byte[] pocessBuffer = new byte[client.nTempByteSize];//지금 읽어온 메시지에 남은 메시지의 사이즈를 더해서 처리할 버퍼 생성
                            //앞 부분에 남았던 메시지 복사
                            Array.Copy(client.tempBuffer, 0, pocessBuffer, 0, client.nTempByteSize);
                            OnIncomingData(client, pocessBuffer);
                        }

                    }
                }

                ///정방향으로 반복하면 삭제시 index가 망가짐
                //접속해제된 클라이언트 목록 처리
                for (int i = disconnectedClients.Count - 1; i >= 0; i--)
                {
                    //접속된 클라이언트 목록에서 삭제
                    connectedClients.Remove(disconnectedClients[i]);

                    //처리후 접속해제된 클라이언트 목록에서 삭제
                    disconnectedClients.Remove(disconnectedClients[i]);
                }

                //연결된 클라이언트 목록(connectedClients)에 추가가 되어 foreach문을 타게 되지만 내용은 안들어가서 client가 null이 되는 현상이 발생하여 딜레이를 준다
                Thread.Sleep(1);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("SocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// UDP 수신 쓰레드 함수
    /// </summary>
    public void UdpListenForIncommingRequeset()
    {
        try
        {
            IPEndPointReceive = new IPEndPoint(IPAddress.Any, udpPort);
            udpSocketReceive = new UdpClient(IPEndPointReceive);
            byte[] udpBuffer = new byte[2048];
            // 데이터 리시브 항시 대기(Update)
            while (true)
            {
                udpBuffer = udpSocketReceive.Receive(ref IPEndPointReceive);

                process_PlayerDirection.Enqueue(GetstPlayerDirectionMsgfromByte(udpBuffer));

                Array.Clear(udpBuffer, 0, udpBuffer.Length);
            }
        }
        catch (SocketException socketException)
        {
            Debug.Log("UDPSocketException " + socketException.ToString());
        }
    }

    /// <summary>
    /// 받은 메시지 처리
    /// </summary>
    /// <param name="data"></param>
    private void OnIncomingData(TCPNetworkModule client, byte[] data)
    {
        // 데이터의 크기가 헤더의 크기보다도 작으면
        if (data.Length < Constants.HEADER_SIZE)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //헤더부분 잘라내기(복사하기)
        byte[] headerDataByte = new byte[Constants.HEADER_SIZE];
        Array.Copy(data, 0, headerDataByte, 0, headerDataByte.Length); //헤더 사이즈 만큼 데이터 복사
        //헤더 데이터 구조체화(마샬링)
        stHeader headerData = HeaderfromByte(headerDataByte);

        // 헤더의 사이즈보다 남은 메시지의 사이즈가 작으면
        if (headerData.PacketSize > data.Length)
        {
            Array.Copy(data, 0, client.tempBuffer, client.nTempByteSize, data.Length);     // 임지 저장 버퍼에 지금 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize += data.Length;
            return;
        }

        //헤더의 메시지크기만큼만 메시지 복사하기
        byte[] msgData = new byte[headerData.PacketSize]; //패킷 분리를 위한 현재 읽은 헤더의 패킷 사이즈만큼 버퍼 생성
        Array.Copy(data, 0, msgData, 0, headerData.PacketSize); //생성된 버퍼에 패킷 정보 복사

        //헤더의 메시지가
        if (headerData.MsgID == MessageID_LogIn.LoginID)//로그인 메시지
        {
            if (headerData.MsgSubID == MessageID_LogIn.LogInMsg)//로그인 ID 메시지 수신 
            {
                stPlayerInfoMsg stPlayerInfoMsgData = GetPlayerInfoMsgfromByte(msgData);
                connectedClients[stPlayerInfoMsgData.LogInID].strPlayerName = stPlayerInfoMsgData.UserName;
                isRecivePlayerName = true;
                SendAllPlayerName();
                if (stPlayerInfoMsgData.LogInID == 0)
                {
                    connectedClients[0].isReady = true;
                }
                else
                {
                    stNumberMsg stNumberMsgData = new stNumberMsg();
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.ReadyGame;
                    stNumberMsgData.PacketSize = (ushort)Marshal.SizeOf(stNumberMsgData);
                    for(int i = 0; i<connectedClients.Count; i++)
                    {
                        if (i == 0) continue;
                        if (connectedClients[i].isReady)
                        {
                            stNumberMsgData.Number = (ushort)i;
                            SendMsg(connectedClients[stPlayerInfoMsgData.LogInID], GetNumberMsgToByte(stNumberMsgData));
                        }
                    }

                }
            }
        }
        if (headerData.MsgID == MessageID_ClientToServer.ClientToServerID)//데이터 동기화 메시지(클라이언트 -> 서버)
        {
            if(headerData.MsgSubID == MessageID_ClientToServer.StartGame)
            {
                stNumberMsg stNumberMsgData = GetNumberMsgfromByte(msgData);
                //0번째 플레이어가 시작 권한 가짐
                if (stNumberMsgData.Number == 0)
                {
                    GameSingletonItems.g_State = GameState.Play;
                    netGameMgr.isConnectedServer = true;
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.StartGame;
                    BroadcastByte(GetNumberMsgToByte(stNumberMsgData));
                }
            }
            else if(headerData.MsgSubID == MessageID_ClientToServer.ReadyGame)
            {
                stNumberMsg stNumberMsgData = GetNumberMsgfromByte(msgData);
                if (stNumberMsgData.Number != 0)
                {
                    readyPlayer.Enqueue(stNumberMsgData.Number);
                    stNumberMsgData.MsgID = MessageID_ServerToClient.ServerToClientID;
                    stNumberMsgData.MsgSubID = MessageID_ServerToClient.ReadyGame;
                    connectedClients[stNumberMsgData.Number].isReady = true;
                    BroadcastByte(GetNumberMsgToByte(stNumberMsgData));
                }
            }
            //else if (headerData.MsgSubID == MessageID_ClientToServer.PlayerDirection)
            //{
            //    process_PlayerDirection.Enqueue(GetstPlayerDirectionMsgfromByte(msgData));
            //}
            else if(headerData.MsgSubID == MessageID_ClientToServer.PlayerJump)
            {
                process_PlayerJump.Enqueue(GetNumberMsgfromByte(msgData));
            }
        }
        else//식별되지 않은 ID
        {

        }

        // 모든 메시지가 처리되서 남은 메시지가 없을 경우 
        if (data.Length == msgData.Length)
        {
            client.isTempByte = false;
            client.nTempByteSize = 0;
        }
        // 메시지 처리 후 메시지가 남아있는 경우
        else
        {
            //임시 버퍼 청소
            Array.Clear(client.tempBuffer, 0, client.tempBuffer.Length);

            //생성된 버퍼에 패킷 정보 복사
            Array.Copy(data, msgData.Length, client.tempBuffer, 0, data.Length - (msgData.Length));// 임시 저장 버퍼에 남은 메시지 저장
            client.isTempByte = true;
            client.nTempByteSize = data.Length - (msgData.Length);
        }
    }

    /// <summary>
    /// 클라이언트 접속 확인
    /// </summary>
    /// <param name="client"></param>
    /// <returns></returns>
    private bool IsConnected(TcpClient client)
    {
        try
        {
            if(client != null && client.Client != null && client.Client.Connected)
            {
                if(client.Client.Poll(0, SelectMode.SelectRead))
                {
                    return !(client.Client.Receive(new byte[1], SocketFlags.Peek) == 0);
                }
                else
                {
                    return true;
                }
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 동기화 데이터 전송 쓰레드
    /// </summary>
    public void SyncDataThread_Send()
    {
        //try
        //{
        //    // 데이터 리시브 항시 대기(Update)
        //    while (true)
        //    {
        //        if (connectedClients.Count <= 0) continue;

        //        //접속한 모든 클라이언트에게
        //        foreach (TCPNetworkModule targetClient in connectedClients)
        //        {
        //            //동기화 데이터가 아직 없다면
        //            if (targetClient == null || targetClient.dataSync.strClientName == null) continue;

        //            //클라이언트 하나의 동기화 정보를
        //            foreach (TCPNetworkModule client in connectedClients)
        //            {
        //                if (client.dataSync.strClientName == null)
        //                    continue;
        //                //전송
        //                SendMsg(targetClient, GetDataSyncMsgToByte(client.dataSync));
        //            }
        //        }

        //        //전송 주기
        //        Thread.Sleep(33);
        //    }
        //}
        //catch (SocketException socketException)
        //{
        //    Debug.Log("SocketException " + socketException.ToString());
        //}
    }

    protected void SendLogInMsg(NetworkModule client, ushort udpPortServer,ushort udpPortClient)
    {
        // 정보 변경 구조체 초기화
        stLogInMsg stLogInMsgData = new stLogInMsg();

        //메시지 작성
        stLogInMsgData.MsgID = MessageID_LogIn.LoginID;//메시지 ID
        stLogInMsgData.MsgSubID = MessageID_LogIn.LogInMsg;//메시지 ID
        stLogInMsgData.PacketSize = (ushort)Marshal.SizeOf(stLogInMsgData);//메시지 크기
        stLogInMsgData.LogInID = (ushort)client.ClientID;
        stLogInMsgData.udpPortServer = udpPortServer;
        stLogInMsgData.udpPortClient = udpPortClient;

        SendMsg(client, GetLogInMsgToByte(stLogInMsgData));
    }

    /// <summary>
    /// 매개변수 메시지 보내기
    /// </summary>
    public void SendMsg(NetworkModule client, byte[] message)
    {
        //서버가 연상태가 아니라면
        if (!serverReady)
            return;

        //전송
        client.stream.Write(message, 0, message.Length);
        client.stream.Flush();
    }
    /// <summary>
    /// 매개변수 메시지 보내기
    /// </summary>
    public void SendMsgUDP(NetworkModule client, byte[] message)
    {
        //서버가 연상태가 아니라면
        if (!serverReady)
            return;

        //전송
        client.udpClient.Send(message, message.Length, client.EndPointSend);
    }

    /// <summary>
    /// 브로드캐스트 메시지 전송
    /// </summary>
    public void BroadcastByte(byte[] message)
    {
        //연결된 클라이언트에게 메시지 전송
        foreach (NetworkModule client in connectedClients)
        {
            if (client != null)
                SendMsg(client, message);
        }
    }

    /// <summary>
    /// 서버 닫기
    /// </summary>
    public void CloseSocket()
    {
        //서버를 연적이 없다면
        if (!serverReady)
        {
            return;
        }
        else//초기화
        {
            //소켓 종료 및 초기화
            //if (tcpListener != null)
            //{

            if (tcpListener != null)
            {
                tcpListener.Stop(); tcpListener = null;
            }
            //상태 초기화
            serverReady = false;

            if(udpSocketReceive != null)
            {
                udpSocketReceive.Close();
                udpSocketReceive = null;
            }

            if (tcpListenerThread != null)
            {
                //쓰레드 초기화
                tcpListenerThread.Abort();
                tcpListenerThread = null;
            }
            if(udpListenerThread != null)
            {
                udpListenerThread.Abort();
                udpListenerThread = null;
            }

            if(MultiPlayerSyncThread != null)
            {
                MultiPlayerSyncThread.Abort();
                MultiPlayerSyncThread = null;
            }
            if (sendDestroyObjectMsgThread != null)
            {
                sendDestroyObjectMsgThread.Abort();
                sendDestroyObjectMsgThread = null;
            }
            if (sendBulletThrowMsgThread != null)
            {
                sendBulletThrowMsgThread.Abort();
                sendBulletThrowMsgThread = null;
            }

            //연결된 클라이언트 초기화
            foreach (NetworkModule client in connectedClients)
            {
                if (client != null)
                {
                    client.stream.Close();
                    client.stream = null;
                    client.clientSocket.Close();
                    client.isTempByte = false;
                    client.nTempByteSize = 0;
                }
            }
            connectedClients.Clear();



            //}
        }

        Debug.Log("서버 닫기 완료");
    }

    /// <summary>
    /// 어플 종료시
    /// </summary>
    private void OnApplicationQuit()
    {
        CloseSocket();
    }
    private void SendAllPlayerName()
    {
        stAllPlayerNameMsg stAllPlayerNameMsg = new stAllPlayerNameMsg();
        stAllPlayerNameMsg.MsgID = MessageID_ServerToClient.ServerToClientID;
        stAllPlayerNameMsg.MsgSubID = MessageID_ServerToClient.AllPlayerName;
        stAllPlayerNameMsg.PacketSize = (ushort)Marshal.SizeOf(stAllPlayerNameMsg);
        stAllPlayerNameMsg.SetUserName();

        int connectedClientsCount = 0;
        for (int i = 0; i < Constants.MAX_USER_COUNT; i++)
        {
            if (i >= connectedClients.Count)
            {
                stAllPlayerNameMsg.UserName[i].name = "@!destroy!@";
                continue;
            }
            if (connectedClients[connectedClientsCount].ClientID == i)
            {
                stAllPlayerNameMsg.UserName[i].name = connectedClients[connectedClientsCount].strPlayerName;
                connectedClientsCount++;
            }
            else
                stAllPlayerNameMsg.UserName[i].name = "@!destroy!@";
        }
        //연결된 클라이언트에게 메시지 전송
        BroadcastByte(GetAllPlayerNameMsgToByte(stAllPlayerNameMsg));
    }
}
