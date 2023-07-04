using CommonData;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

public class GetDamage_Player : GetDamage
{
    [HideInInspector]
    public bool isClientPlayer = false;

    private GameMgr gameMgr;
    private CameraFollow cameraFollow;
    private Slider ui_HpSlider;

    public float GetCurHpValue()
    {
        return curHp;
    }

    public override void GetDamaged()
    {
        base.GetDamaged();
        if (curHp <= 0)
        {
            SetHpSlider(0);
            GameSingletonItems.isPlayerAlive = false;
            gameObject.SetActive(false);
            if (GeneralMgr.isServer) GetComponent<NetPlayerControl>().isAlive = false;
        }
    }
    public override void GetDamaged(float damage)
    {
        if (damage < 1) return;
        base.GetDamaged(damage);

        float shakeTime = damage / 100;
        if(shakeTime > 1)  shakeTime = 1;
        if(shakeTime > 0.2 && GeneralMgr.isServer == false)
            cameraFollow.SetCameraShakeTimer(shakeTime);
        if (curHp <= 0)
        {
            ui_HpSlider.value = 0;
            GameSingletonItems.isPlayerAlive = false;
            gameObject.SetActive(false);
            if (GeneralMgr.isServer) GetComponent<NetPlayerControl>().isAlive = false;
        }
        else
        {
            SetHpSlider(curHp / maxHp);
        }
    }


    public void HpReset()
    {
        curHp = maxHp;
        SetHpSlider(curHp/maxHp);
    }

    public void SetHpSlider(float value)
    {
        if (!isClientPlayer) return;
        ui_HpSlider.value = value;
    }

    private void Awake()
    {
        cameraFollow = FindObjectOfType<CameraFollow>();
        gameMgr = FindFirstObjectByType<GameMgr>();
        ui_HpSlider = gameMgr.ui_PlayerHpBar.GetComponent<Slider>();
        isClientPlayer = false;
    }

    protected override void Start()
    {
        base.Start();
        if (GameSingletonItems.isNetGame == false)
            isClientPlayer = true; //√ ±‚»≠
        SetHpSlider(1);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("GoldBird"))
        {
            //missionMgr.getBirdCount++;
            StartCoroutine(other.gameObject.GetComponent<Bird>().PlayerEnterTarget(1));
            gameMgr.AchieveGoldBirdCount++;
            other.gameObject.SetActive(false);
        }
        else if (other.gameObject.CompareTag("Target"))
        {
            if (GameSingletonItems.isNetGame == false)
            {
                GameSingletonItems.isPlayerAlive = false;
                GameSingletonItems.g_State = GameState.Clear;
            }
            if (GeneralMgr.isServer)
            {
                GameSingletonItems.g_State = GameState.Clear;
                NetGameMgr netGameMgr = FindObjectOfType<NetGameMgr>();
                for (int i = 0; i< Constants.MAX_USER_COUNT; i++)
                {
                    if (netGameMgr.PlayerGroup[i] == gameObject)
                    {
                        netGameMgr.playerNumber = i;
                        break;
                    }
                }
            }

            
            StartCoroutine(other.gameObject.GetComponent<Bird>().PlayerEnterTarget(1));
            other.gameObject.SetActive(false);
        }
    }
}
