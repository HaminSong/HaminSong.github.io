using System.Runtime.InteropServices;
using System;
using UnityEngine;

namespace CommonData
{
    
    /// <summary>
    /// 상수 클래스
    /// </summary>
    static public class Constants
    {
        public const int HEADER_ID_SIZE = 4;//헤더 사이즈는 6(4(ID) + 2(Size))
        public const int HEADER_SIZE = HEADER_ID_SIZE + 2;//헤더 사이즈는 6(4(ID) + 2(Size))

        public const int MAX_NAME_LENGTH = 32;
        public const int MAX_USER_COUNT = 4;//최대 사용자 수

        public const int FRAME_PER_SECOND = 30;//1초당 프레임
        public const int TCP_PORT_NUMBER = 9100;
        public const int UDP_PORT_NUMBER = 9200;
    }

    /// <summary>
    /// 로그인 메시지
    /// </summary>
    static public class MessageID_LogIn
    {
        //ID
        public const ushort LoginID = 0x00;
        //SubID
        public const ushort LogInMsg = 0x00;                        //ID 부여
        public const ushort ChangePlayerInfo = 0x01;                //플레이어 정보 변경 (이름,Mode[플레이어,모니터링])
    }

    /// <summary>
    /// 데이터 동기화 메시지(클라이언트 -> 서버)
    /// </summary>
    static public class MessageID_ClientToServer
    {
        //ID
        public const ushort ClientToServerID    = 0x01;

        //SubID
        public const ushort PlayerPosition      = 0x00;         //플레이어 위치 정보
        public const ushort StartGame           = 0x01;         // 게임 시작
        public const ushort FinishGame          = 0x02;         // 게임 종료
        public const ushort ReadyGame           = 0x03;         // 준비 완료
        public const ushort PlayerDirection     = 0x04;         // 플레이어 이동 방향
        public const ushort PlayerJump          = 0x05;         // 플레이어 점프
        public const ushort PlayerSelfDead      = 0x06;         // 플레이어 자살
    }

    /// <summary>
    /// 데이터 동기화 메시지(서버 -> 클라이언트)
    /// </summary>
    static public class MessageID_ServerToClient
    {
        //ID
        public const ushort ServerToClientID    = 0x02;
        //SubID
        public const ushort AllPlayerName       = 0x00;         // 게임 제어권 전송!! (첫번째 플레이어 : true, 나머지 플레이어 : false)
                                                                // public const ushort InitTokenInfo = 0x00;           // 초기 토큰 정보(게임오브젝트 복원 - enable : true) - 이벤트
        public const ushort StartGame           = 0x01;         // 게임 시작(캐릭터 움직임 제어 가능) - 이벤트
        public const ushort EndGame             = 0x02;         // 게임 시작(캐릭터 움직임 제어 불가) - 이벤트                           
        public const ushort ReadyGame           = 0x03;
        public const ushort PlayerSyncInfo      = 0x04;         // 전체 정보 전송
        public const ushort PlayerAliveInfo     = 0x05;
        public const ushort PlayerDamagedInfo   = 0x06;
        public const ushort BirdThrow           = 0x07;
        public const ushort CastleGateCracked   = 0x08;
        public const ushort ClearGame           = 0x09;
                                                     
        public const ushort WallDestroy         = 0xA0;
        public const ushort BoomDestroy         = 0xA1;
        public const ushort WindMillDestroy     = 0xA2;
        public const ushort TrapDestroy         = 0xA3;
        public const ushort CastleGateDestroy   = 0xA4;

    }

    /// <summary>
    /// 기타 메시지
    /// </summary>
    static public class MessageID_ETC
    {
        //ID
        public const ushort ETCID = 0x02;                   //메시지
        //SubID
        public const ushort Message = 0x00;                 //메시지
    }
}

public class CommonDataClass 
{
    /// <summary>
    /// 헤더 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stHeader
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기
    }
    /// <summary>
    /// 헤더 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stHeader HeaderfromByte(byte[] arr)
    {
        //구조체 초기화
        stHeader str = default(stHeader);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stHeader)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 로그인 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetHeaderToByte(stHeader str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// 로그인 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stLogInMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // 로그인
        [MarshalAs(UnmanagedType.R4/*float*/, SizeConst = 2)]
        public UInt16 udpPortServer; // UDP port 서버
        [MarshalAs(UnmanagedType.R4/*float*/, SizeConst = 2)]
        public UInt16 udpPortClient; // UDP port 클라
    }
    /// <summary>
    /// 로그인 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stLogInMsg GetLogInMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stLogInMsg str = default(stLogInMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stLogInMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 로그인 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetLogInMsgToByte(stLogInMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// 숫자만 넘길때 쓰는 구조체
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stNumberMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 Number; // 숫자
    }
    /// <summary>
    /// 숫자만 넘기기 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stNumberMsg GetNumberMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stNumberMsg str = default(stNumberMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stNumberMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 숫자만 넘기기마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetNumberMsgToByte(stNumberMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// 플레이어 생존 여부 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stPlayerAliveInfo
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // 로그인 넘버

        [MarshalAs(UnmanagedType.Bool/*ushort*/, SizeConst = 1/*ushort size*/)]
        public bool isAlive; // 로그인 넘버
    }
    /// <summary>
    /// 플레이어 생존 여부 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerAliveInfo GetPlayerAliveInfofromByte(byte[] arr)
    {
        //구조체 초기화
        stPlayerAliveInfo str = default(stPlayerAliveInfo);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stPlayerAliveInfo)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 플레이어 생존 여부 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerAliveInfoToByte(stPlayerAliveInfo str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// 유저 이름 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stPlayerInfoMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 LogInID; // 로그인 넘버
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CommonData.Constants.MAX_NAME_LENGTH)]
        public string UserName; // 유저 이름

        //[MarshalAs(UnmanagedType.Bool/*Bool*/, SizeConst = 1)]
        //public bool isMode; // Mode( true : 플레이어, false: 모니터링)

    }
    /// <summary>
    /// 유저 이름 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerInfoMsg GetPlayerInfoMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stPlayerInfoMsg str = default(stPlayerInfoMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stPlayerInfoMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 유저 이름 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerInfoMsgToByte(stPlayerInfoMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stPlayerName
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = CommonData.Constants.MAX_NAME_LENGTH)]
        public string name;
    }

    /// <summary>
    /// 로그인 구조체 마샬링
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stAllPlayerNameMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = CommonData.Constants.MAX_USER_COUNT)]
        public stPlayerName[] UserName; // 유저 이름
        public void SetUserName(int UserCount = CommonData.Constants.MAX_USER_COUNT)
        {
            UserName = new stPlayerName[UserCount];
        }

    }
    /// <summary>
    /// 로그인 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stAllPlayerNameMsg GetAllPlayerNameMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stAllPlayerNameMsg str = default(stAllPlayerNameMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stAllPlayerNameMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 로그인 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetAllPlayerNameMsgToByte(stAllPlayerNameMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stPlayerDirectionMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 ClientID; // 클라이언트 아이디

        [MarshalAs(UnmanagedType.ByValArray/*ushort*/, SizeConst = 3/*ushort size*/)]
        public Vector3 MoveVector; //플레이어가 이동하고 싶은 방향
    }
    /// <summary>
    /// 정보 동기화 메시지 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerDirectionMsg GetstPlayerDirectionMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stPlayerDirectionMsg str = default(stPlayerDirectionMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        if (size > arr.Length)
        {
            throw new Exception();
        }

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stPlayerDirectionMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 정보 동기화 메시지 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetstPlayerDirectionMsgToByte(stPlayerDirectionMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// 플레이어 동기화 메시지 구조체 마샬링 서버 -> 클라이언트
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stTransformInfo
    {
        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 3)]
        public Vector3 position; // 클라이언트 위치 X좌표

        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 4)]
        public Quaternion rotation; // 클라이언트 위치 X좌표
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct stMultiPlayerSyncMsg
    {
        [MarshalAs(UnmanagedType.ByValArray/*array*/, SizeConst = CommonData.Constants.MAX_USER_COUNT)]
        public stTransformInfo[] MultiPlayerInfo; // 클라이언트 사용자 정보


        public void SetMultiPlayerInfo(int nUserCnt = CommonData.Constants.MAX_USER_COUNT)
        {
            MultiPlayerInfo = new stTransformInfo[nUserCnt];
        }
    }
    /// <summary>
    /// 정보 동기화 메시지 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stMultiPlayerSyncMsg GetMultiPlayerSyncMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stMultiPlayerSyncMsg str = default(stMultiPlayerSyncMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        if (size > arr.Length)
        {
            throw new Exception();
        }

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stMultiPlayerSyncMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 정보 동기화 메시지 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetMultiPlayerSyncMsgToByte(stMultiPlayerSyncMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }

    /// <summary>
    /// 플레이어가 입은 데미지를 보내줌
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stPlayerDamaged
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.R4/*ushort*/, SizeConst = 4/*ushort size*/)]
        public float Damage; // 받은 데미지
    }
    /// <summary>
    /// 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stPlayerDamaged GetPlayerDamagedfromByte(byte[] arr)
    {
        //구조체 초기화
        stPlayerDamaged str = default(stPlayerDamaged);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stPlayerDamaged)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetPlayerDamagedToByte(stPlayerDamaged str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
    /// <summary>
    /// 불렛 날리기
    /// </summary>
    [StructLayout(LayoutKind.Sequential/*들어오는순서대로(Queue)*/, Pack = 1/*데이터를 읽을 단위*/)]
    public struct stBirdThrowMsg
    {
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 MsgSubID; // 메시지 ID
        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2/*ushort size*/)]
        public UInt16 PacketSize; // 나머지부분 메시지 크기

        [MarshalAs(UnmanagedType.U2/*ushort*/, SizeConst = 2)]
        public UInt16 IndexNumber; // 버드하우스 넘버

        [MarshalAs(UnmanagedType.ByValArray/*float*/, SizeConst = 3)]
        public Vector3 Direction; // 날릴 방향
    }
    /// <summary>
    /// 구조체 마샬링 함수(Byte->구조체)
    /// </summary>
    /// <param name="arr"></param>
    /// <returns></returns>
    public static stBirdThrowMsg GetBirdThrowMsgfromByte(byte[] arr)
    {
        //구조체 초기화
        stBirdThrowMsg str = default(stBirdThrowMsg);
        int size = Marshal.SizeOf(str);//구조체 Size

        //Size만큼 메모리 할당(메모리 자리 빌리기)
        IntPtr ptr = Marshal.AllocHGlobal(size);

        //데이터를 복사하여 메모리에 넣기(데이터 쑤셔넣기)
        Marshal.Copy(arr, 0, ptr, size);

        //구조체에 넣기(쑤셔넣은 데이터 정리 해서 구조체에 넣기)
        str = (stBirdThrowMsg)Marshal.PtrToStructure(ptr, str.GetType());
        //할당한 메모리 해제
        Marshal.FreeHGlobal(ptr);

        //구조체 리턴
        return str;
    }
    /// <summary>
    /// 구조체 마샬링 함수(구조체->Byte)
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetBirdThrowMsgToByte(stBirdThrowMsg str)
    {
        int size = Marshal.SizeOf(str);
        byte[] arr = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(str, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);

        Marshal.FreeHGlobal(ptr);
        return arr;
    }
}