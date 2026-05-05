using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayFrame : MonoBehaviour
{
    public enum State
    {
        idle,
        playing,
        pause
    }

    public enum State1
    {
        once,
        loop
    }

    [Header("播放方式(循环、单次)")]//默认单次
    public State1 condition = State1.once;

    [Header("自动播放")]//默认不自动播放
    public bool PlayAwake = false;

    //播放状态(默认、播放中、暂停)
    private State playState;

    private Image manimg;

    [Header("每秒播放的帧数(整数)")]
    public float frame_number = 30;

    public Sprite[] texture_arr;

    //回调事件
    public UnityEvent onCompleteEvent;

    private int index;
    private float tim;
    private float waittim;
    private bool isplay;
    private bool playend;

    private void OnEnable()
    {
        manimg = GetComponent<Image>();
        tim = 0;
        index = 0;
        waittim = 1 / frame_number;
        playState = State.idle;
        isplay = false;
        playend = false;

        ToDisvisible();

        manimg.sprite = texture_arr[0];
        if (PlayAwake)
        {
            Play();
        }
    }

    private void Update()
    {
        UpMove();
    }

    #region 改进-播放显示，不播放隐藏

    /// <summary>
    /// 改进-播放显示，不播放隐藏
    /// </summary>
    private void ToVisible()
    {
        manimg.color = new Color(255, 255, 255, 255);
    }

    private void ToDisvisible()
    {
        //manimg.color = new Color(255, 255, 255, 0);
    }

    #endregion 改进-播放显示，不播放隐藏

    private void UpMove()
    {
        //单播
        if (condition == State1.once)
        {
            if (playState == State.idle && isplay)
            {
                playState = State.playing;
                index = 0;
                tim = 0;
            }
            if (playState == State.pause && isplay)
            {
                playState = State.playing;
                tim = 0;
            }
            if (playState == State.playing && isplay)
            {
                tim += Time.deltaTime;
                if (tim >= waittim)
                {
                    tim = 0;
                    index++;
                    if (index >= texture_arr.Length)
                    {
                        index = 0;
                        manimg.sprite = texture_arr[texture_arr.Length - 1];
                        isplay = false;
                        playState = State.idle;
                        ToDisvisible();
                        //此处可添加结束回调函数
                        if (onCompleteEvent != null)
                        {
                            onCompleteEvent.Invoke();
                            playend = true;
                            return;
                        }
                    }
                    manimg.sprite = texture_arr[index];
                }
            }
        }
        //循环播放
        if (condition == State1.loop)
        {
            if (playState == State.idle && isplay)
            {
                playState = State.playing;
                index = 0;
                tim = 0;
            }
            if (playState == State.pause && isplay)
            {
                playState = State.playing;
                tim = 0;
            }
            if (playState == State.playing && isplay)
            {
                tim += Time.deltaTime;
                if (tim >= waittim)
                {
                    tim = 0;
                    index++;
                    if (index >= texture_arr.Length)
                    {
                        index = 0;
                        //此处可添加结束回调函数
                    }
                    manimg.sprite = texture_arr[index];
                }
            }
        }
    }

    public bool get_PlayEnd()
    {
        return playend;
    }

    /// <summary>
    /// 播放
    /// </summary>
    public void Play()
    {
        ToVisible();
        isplay = true;
        playend = false;
    }

    /// <summary>
    /// 暂停
    /// </summary>
    public void Pause()
    {
        isplay = false;
        playState = State.pause;
    }

    /// <summary>
    /// 停止
    /// </summary>
    public void Stop()
    {
        isplay = false;
        playState = State.idle;
        index = 0;
        tim = 0;
        if (manimg == null)
        {
            Debug.LogWarning("Image为空，请赋值");
            return;
        }
        manimg.sprite = texture_arr[index];
    }

    /// <summary>
    /// 重播
    /// </summary>
    public void Replay()
    {
        isplay = true;
        playend = false;
        playState = State.playing;
        index = 0;
        tim = 0;
    }
}