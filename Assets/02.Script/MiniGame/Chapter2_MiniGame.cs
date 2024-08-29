using Org.BouncyCastle.X509;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Chapter2_MiniGame : MonoBehaviour
{
    [SerializeField] private List<Rice> RiceObjects;
    [SerializeField] private TMP_Text Text_timer;

    [SerializeField] private float SetTimer;
    private float timer;

    private bool isGameClear;

    private void OnEnable()
    {
        isGameClear = false;
        timer = SetTimer;
    }

    private void Start()
    {
        StartCoroutine(StartTimer());
    }

    IEnumerator StartTimer()
    {
        while(true)
        {
            yield return null;
            timer -= Time.deltaTime;
            Text_timer.text = timer.ToString("0");

            isGameClear = CheckGameClear();

            if (isGameClear)
            {
                GameClear();
                yield break;
            }

            if (timer <= 0)
            {
                GameOver();
                yield break;
            }
        }
    }

    private void GameOver()
    {
        DebugLogger.Log($"Clear 실패");
    }

    private void GameClear()
    {
        // 디버그 용
        DebugLogger.Log($"게임 클리어 : {isGameClear}");
    }

    private bool CheckGameClear()
    {
        bool isClear = true;
        foreach(var rice in RiceObjects)
        {
            isClear = rice.isSlices;
            if (!isClear) break;
        }

        return isClear;
    }
}
