﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using com.PlugStudio.Patterns;

public class Game : State
{
    [Header("UI")]
    public RectTransform canvas;
    public RectTransform contentView;
    public Animator settingView;
    public Text title;
    public Text attckLimitText; // 공격횟수
    public Text timeLimitText;

    [Header("Touch Layer")]
    public LayerMask raycastMask;

    [Space]
    public Puzzle puzzle;

    // 터치 관련
    private Ray2D ray;
    private RaycastHit2D hit;
    private StageData stage;

    // 게임진행관련
    private Animator timeLimitAnimator;
    private AudioSource clockSound;
    private float currentTimeLimit;

    public override IEnumerator Initialize(params object[] _data)
    {
        Vector2 size = contentView.sizeDelta;

        size.y = SizeChanger.Calculate(canvas);

        contentView.sizeDelta = size;
        settingView.GetComponent<RectTransform>().sizeDelta = size;

        if (AdsManager.Instance.LoadedReward == false)
        {
            AdsManager.Instance.RequestReward();
        }

        int index = (int)_data[0];

        stage = GameManager.Instance.Stages[index];
        timeLimitAnimator = timeLimitText.GetComponent<Animator>();
        title.text = stage.title;

        if(stage.isAchieve[0])
        {
            puzzle.currentAttackLimit = stage.AttackLimit;
            puzzle.isAttackLimit = true;
            attckLimitText.gameObject.SetActive(true);
            attckLimitText.text = string.Format("{0}", stage.AttackLimit);
        }

        if(stage.isAchieve[1])
        {
            currentTimeLimit = stage.TimeLimit;

            timeLimitText.gameObject.SetActive(true);
            timeLimitText.text = string.Format("{0:F0}", currentTimeLimit);
        }
        
        StartCoroutine(puzzle.Initialized(stage));

        InputController.Instance.AddObservable(this);

        yield return null;
    }

    public override void Begin()
    {
        // 튜토리얼 체크
        if (stage.level == 1 && GameManager.Instance.IsViewTutorial == false)
        {
            GameManager.Instance.IsViewTutorial = true;
            GameManager.Instance.ShowTutorial();
        }
        else
        {
            GameManager.Instance.StartGame();
        }
    }

    public override void Execute()
    {
        if (stage == null)
        {
            return;
        }

        if (clockSound != null)
        {
            clockSound.mute = GameManager.Instance.GameData.muteSE;

            if(GameManager.Instance.IsPause)
            {
                clockSound.Pause();
            }
            else if(clockSound.isPlaying == false)
            {
                clockSound.UnPause();
            }
        }

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            // 게임세팅 보기 & 게임 퍼즈
            PauseGame();
        }
        else if(Input.GetKeyDown(KeyCode.Return))
        {
            // 스테이지 클리어
            GameManager.Instance.FinishGame(true);
        }
        else if(Input.GetKeyDown(KeyCode.LeftControl))
        {
            SkillManager.Instance.AddEnergy();
        }

        attckLimitText.text = string.Format("{0}", puzzle.currentAttackLimit);

        if (GameManager.Instance.IsPlaying && puzzle.createComplete)
        {
            if (stage.isAchieve[0])
            {
                if(puzzle.currentAttackLimit <= 0)
                {
                    GameManager.Instance.FinishGame(false);
                    return;
                }
            }

            if (stage.isAchieve[1])
            {
                if(currentTimeLimit <= 0)
                {
                    clockSound.Stop();
                    Destroy(clockSound.gameObject);

                    GameManager.Instance.FinishGame(false);
                    return;
                }

                if(Mathf.FloorToInt(currentTimeLimit) == 10
                    && timeLimitAnimator.GetBool("Timeout") == false)
                {
                    clockSound = SoundManager.Instance.GetSE(12);
                    clockSound.Play();
                    timeLimitAnimator.SetBool("Timeout", true);
                }

                if(GameManager.TimeStop == false)
                {
                    currentTimeLimit = Mathf.Lerp(currentTimeLimit, currentTimeLimit - 1, Time.deltaTime);
                    timeLimitText.text = string.Format("{0:F0}", currentTimeLimit);
                }
            }
        }
    }

    public override void Release()
    {
        if(clockSound != null)
        {
            clockSound.Stop();
            Destroy(clockSound.gameObject);
        }

        GameManager.Reinforce = 1;
        GameManager.TimeStop = false;

        puzzle.Clear();
        InputController.Instance.RemoveObservable(this);
    }

    public override void TouchBegan(Vector3 _touchPosition, int _index)
    {
        if(GameManager.Instance.IsPlaying)
        {
            if (puzzle.isMoved)
            {
                return;
            }

            int layerMask = 1 << raycastMask.value;

            RaycastHit2D hit = Physics2D.Raycast(_touchPosition, Vector2.zero, 0f, ~layerMask);

            if (hit.collider != null)
            {
                if(GameManager.TimeStop)
                {
                    GameManager.TimeStop = false;
                }

                int index = hit.collider.GetComponent<TileController>().Index;
                StartCoroutine(puzzle.MoveTile(index));
            }
        }
    }
    
    // UI 접근용
    public void PauseGame()
    {
        GameManager.Instance.PauseGame(true);
        settingView.Play("Setting_Show");
    }

    // UI 접근용
    public void ResumeGame()
    {
        GameManager.Instance.PauseGame(false);
        settingView.Play("Setting_Close");
    }

    public void ContinueGame()
    {
        if(stage.isAchieve[0])
        {
            puzzle.currentAttackLimit = stage.AttackLimit;
        }

        if (stage.isAchieve[1])
        {
            timeLimitAnimator.SetBool("Timeout", false);

            if(clockSound != null)
            {
                clockSound.Stop();
                Destroy(clockSound.gameObject);
            }

            currentTimeLimit = stage.TimeLimit;
        }

        GameManager.Instance.ChangeState(GameManager.PlayState.Play);
        StartCoroutine(puzzle.Shuffle("Tile_Respawn"));
    }
}
