using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// 이전부터 사용하던 오디오 매니저로, 딕셔너리 구조를 통해 오디오를 스트링 키값으로 손쉽게 호출할 수 있음.
// 오디오매니저는 DDOL로 매 씬마다 재사용되기에 Lobby에서만 오디오 클립을 관리하면 됨
public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("# Audio Mixer")]
    public AudioMixer audioMixer;
    public AudioMixerGroup bgmGroup;
    public AudioMixerGroup sfxGroup;
    public AudioMixerGroup ambientGroup;

    [System.Serializable]
    public class BGMPair
    {
        public string key;      // BGM 이름
        public AudioClip clip;  // 실제 클립
    }

    [Header("# BGM")]
    public List<BGMPair> BGMPairs = new List<BGMPair>();
    public float BGMVolume = 0.5f;
    private AudioSource BGMPlayer;
    private AudioHighPassFilter BGMEffet;
    private Dictionary<string, AudioClip> BGMDict;

    [System.Serializable]
    public class AmbientPair
    {
        public string key;
        public AudioClip clip;
    }

    [Header("# Ambient (Environment Loop)")]
    public List<AmbientPair> AmbientPairs = new List<AmbientPair>();
    public float AmbientVolume = 0.5f; // 기본 환경음 볼륨
    private AudioSource AmbientPlayer;
    private Dictionary<string, AudioClip> AmbientDict;

    [Header("# SFX")]
    public float SFXVolume = 0.5f;
    public int channels = 16;
    private AudioSource[] SFXPlayers;
    private int channelIndex;

    [Header("# 3D Sound Settings")]
    [Range(0f, 1f)] public float spatialBlend = 1f; // 1이면 완전한 3D
    public float minDistance = 1f;  // 소리가 최대인 거리
    public float maxDistance = 30f; // 소리가 안 들리게 되는 거리

    [Header("# Volume UI")]
    public Scrollbar BGMScrollbar;
    public Scrollbar SFXScrollbar;

    [System.Serializable]
    public class SFXPair
    {
        public string key;
        public AudioClip clip;
    }

    public List<SFXPair> SFXPairs = new List<SFXPair>();
    private Dictionary<string, AudioClip> SFXDict;

    // DDOL로 매 씬마다 AudioManager를 재사용함.
    // TODO = LeftRoom() 마다 출력 중인 BGM과 SFX를 종료해야함.
    // TODO = JoinedRoom() 마다 BGM을 변경해야함.
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        Init();

        // UI 연결
        if (BGMScrollbar != null)
        {
            BGMScrollbar.value = BGMVolume;
            BGMScrollbar.onValueChanged.AddListener(SetBGMVolume);
            BGMScrollbar.onValueChanged.AddListener(SetAmbVolume);
        }
        if (SFXScrollbar != null)
        {
            SFXScrollbar.value = SFXVolume;
            SFXScrollbar.onValueChanged.AddListener(SetSFXVolume);
        }

        // 딕셔너리 초기화
        BGMDict = new Dictionary<string, AudioClip>();
        foreach (var pair in BGMPairs)
        {
            if (!BGMDict.ContainsKey(pair.key)) BGMDict.Add(pair.key, pair.clip);
        }

        SFXDict = new Dictionary<string, AudioClip>();
        foreach (var pair in SFXPairs)
        {
            if (!SFXDict.ContainsKey(pair.key)) SFXDict.Add(pair.key, pair.clip);
        }

        AmbientDict = new Dictionary<string, AudioClip>();
        foreach (var pair in AmbientPairs)
        {
            if (!AmbientDict.ContainsKey(pair.key)) AmbientDict.Add(pair.key, pair.clip);
        }
    }

    void Init()
    {
        // BGM Player 설정
        GameObject BGMObject = new GameObject("BGMPlayer");
        BGMObject.transform.parent = transform;
        BGMPlayer = BGMObject.AddComponent<AudioSource>();
        BGMPlayer.playOnAwake = false;
        BGMPlayer.loop = true;
        BGMPlayer.volume = BGMVolume;
        BGMPlayer.priority = 0;
        if (bgmGroup != null) BGMPlayer.outputAudioMixerGroup = bgmGroup;

        BGMEffet = BGMObject.AddComponent<AudioHighPassFilter>();
        BGMEffet.enabled = false;

        // Emvient Player 설정
        GameObject AmbientObject = new GameObject("AmbientPlayer");
        AmbientObject.transform.parent = transform;
        AmbientPlayer = AmbientObject.AddComponent<AudioSource>();
        AmbientPlayer.playOnAwake = false;
        AmbientPlayer.loop = true;
        AmbientPlayer.volume = AmbientVolume;
        AmbientPlayer.priority = 50;

        // 전용 믹서 그룹이 있으면 연결, 없으면 sfxGroup이나 bgmGroup 땜빵 가능
        if (ambientGroup != null) AmbientPlayer.outputAudioMixerGroup = ambientGroup;
        else if (bgmGroup != null) AmbientPlayer.outputAudioMixerGroup = bgmGroup;

        // SFX Players 채널 설정
        SFXPlayers = new AudioSource[channels];
        for (int i = 0; i < channels; i++)
        {
            GameObject SFXObject = new GameObject("SFXPlayer_" + i);
            SFXObject.transform.parent = transform;
            SFXPlayers[i] = SFXObject.AddComponent<AudioSource>();
            SFXPlayers[i].playOnAwake = false;
            SFXPlayers[i].volume = SFXVolume;
            SFXPlayers[i].bypassListenerEffects = true;
            SFXPlayers[i].dopplerLevel = 0f; // ★ 추가: 투사체 이동 시 웽~ 하는 왜곡음 방지

            // --- 3D 기본 설정 ---
            SFXPlayers[i].spatialBlend = 0f; // 기본은 2D (UI 등)
            SFXPlayers[i].rolloffMode = AudioRolloffMode.Logarithmic;
            SFXPlayers[i].minDistance = minDistance;
            SFXPlayers[i].maxDistance = maxDistance;
            // --------------------

            if (sfxGroup != null) SFXPlayers[i].outputAudioMixerGroup = sfxGroup;
        }
    }

    /// <summary>
    /// 2D 효과음 출력 (UI, 시스템 사운드용)
    /// </summary>
    public void PlaySFX(string key)
    {
        InternalPlaySFX(key, Vector3.zero, false, 1f, 1f);
    }

    /// <summary>
    /// 일반 3D 효과음 출력 (월드 내 특정 위치 사운드용)
    /// </summary>
    public void PlaySFX(string key, Vector3 worldPosition)
    {
        InternalPlaySFX(key, worldPosition, true, 1f, 1f);
    }

    /// <summary>
    /// ★ 추가: 총소리, 폭발 등 매번 달라져야 하는 다이내믹 3D 효과음 출력
    /// </summary>
    public void PlayDynamicSFX(string key, Vector3 worldPosition, bool useEcho = true)
    {
        // 1. 메인 사운드: 피치와 볼륨을 살짝 비틀어서 출력
        float randomPitch = Random.Range(0.95f, 1.05f);
        float randomVolMult = Random.Range(0.8f, 1.0f);

        InternalPlaySFX(key, worldPosition, true, randomPitch, randomVolMult);

        // 2. 에코 사운드: 코루틴을 통해 약간의 딜레이 후 둔탁하게 한 번 더 출력
        if (useEcho)
        {
            StartCoroutine(EchoCoroutine(key, worldPosition));
        }
    }

    // 1개를 3가지 버전으로 랜덤하게 출력하는 함수 (ex. 공중 소리)
    public void PlaySingleClipVariants(string key, Vector3 worldPosition, int variantIndex)
    {
        if (SFXDict == null || !SFXDict.ContainsKey(key)) return;

        for (int i = 0; i < SFXPlayers.Length; i++)
        {
            int idx = (i + channelIndex) % SFXPlayers.Length;

            // 이미 재생 중이면 패스
            if (SFXPlayers[idx].isPlaying) continue;

            if (SFXDict.TryGetValue(key, out AudioClip clip))
            {
                // 3D 사운드 기본 설정
                SFXPlayers[idx].spatialBlend = spatialBlend;
                SFXPlayers[idx].transform.position = worldPosition;
                SFXPlayers[idx].minDistance = minDistance;
                SFXPlayers[idx].maxDistance = maxDistance;

                // 들어온 번호(1~3)에 따라 피치를 다르게 꼬아버림
                float finalPitch = 1f;
                if (variantIndex == 2) finalPitch = Random.Range(1.15f, 1.25f);     // 2번 걸음: 가볍고 높은 소리
                else if (variantIndex == 3) finalPitch = Random.Range(0.82f, 0.90f); // 3번 걸음: 낮고 묵직한 소리
                else finalPitch = Random.Range(0.96f, 1.04f);                       // 1번 걸음: 원본 대역 소리

                SFXPlayers[idx].pitch = finalPitch;
                SFXPlayers[idx].volume = SFXVolume; // 기존 볼륨 적용
                SFXPlayers[idx].clip = clip;

                SFXPlayers[idx].Play();

                float startTimeOffset = Random.Range(0.01f, Mathf.Min(0.04f, clip.length * 0.1f));
                SFXPlayers[idx].time = startTimeOffset;

                channelIndex = (idx + 1) % SFXPlayers.Length;
                break;
            }
        }
    }

    // 메아리(에코)를 위한 코루틴
    private IEnumerator EchoCoroutine(string key, Vector3 position)
    {
        yield return new WaitForSeconds(0.15f); // 0.15초 딜레이

        // 에코는 피치를 확 낮추고 볼륨을 작게 설정
        float echoPitch = Random.Range(0.7f, 0.8f);
        float echoVolMult = 0.3f;

        InternalPlaySFX(key, position, true, echoPitch, echoVolMult);
    }

    // 파라미터에 pitch와 volumeMult 추가
    private void InternalPlaySFX(string key, Vector3 position, bool is3D, float pitch, float volumeMult)
    {
        if (SFXDict == null || !SFXDict.ContainsKey(key)) return;

        for (int i = 0; i < SFXPlayers.Length; i++)
        {
            int idx = (i + channelIndex) % SFXPlayers.Length;

            // 이미 재생 중이면 다음 채널 확인
            if (SFXPlayers[idx].isPlaying) continue;

            if (SFXDict.TryGetValue(key, out AudioClip clip))
            {
                // 3D 여부에 따른 설정 변경
                if (is3D)
                {
                    SFXPlayers[idx].spatialBlend = spatialBlend; // 3D 설정
                    SFXPlayers[idx].transform.position = position; // 소리 발생 위치로 이동
                    SFXPlayers[idx].minDistance = minDistance;
                    SFXPlayers[idx].maxDistance = maxDistance;
                }
                else
                {
                    SFXPlayers[idx].spatialBlend = 0f; // 2D 설정
                }

                // 오디오 피치 및 최종 볼륨 적용 (다른 채널 재사용 시 꼬이지 않게 덮어쓰기)
                SFXPlayers[idx].pitch = pitch;
                SFXPlayers[idx].volume = SFXVolume * volumeMult;

                SFXPlayers[idx].clip = clip;
                SFXPlayers[idx].Play();

                // 다음 재생을 위해 인덱스 미리 이동
                channelIndex = (idx + 1) % SFXPlayers.Length;
                break;
            }
        }
    }

    public void PlayBGM(string key)
    {
        if (BGMDict != null && BGMDict.TryGetValue(key, out AudioClip clip))
        {
            BGMPlayer.clip = clip;
            BGMPlayer.Play();
        }
    }

    public void EffectBGM(bool isPlay)
    {
        if (BGMEffet != null) BGMEffet.enabled = isPlay;
    }

    public void SetBGMVolume(float volume)
    {
        BGMVolume = volume;
        if (BGMPlayer != null) BGMPlayer.volume = BGMVolume;
    }

    public void PlayAmb(string key)
    {
        if (AmbientDict != null && AmbientDict.TryGetValue(key, out AudioClip clip))
        {
            // 이미 같은 환경음이 재생 중이면 중복 재생 방지
            if (AmbientPlayer.clip == clip && AmbientPlayer.isPlaying) return;

            AmbientPlayer.clip = clip;
            AmbientPlayer.Play();
        }
    }

    public void StopAmb()
    {
        if (AmbientPlayer != null)
        {
            AmbientPlayer.Stop();
            AmbientPlayer.clip = null;
        }
    }

    public void SetAmbVolume(float volume)
    {
        AmbientVolume = volume;
        if (AmbientPlayer != null) AmbientPlayer.volume = AmbientVolume;
    }

    public void SetSFXVolume(float volume)
    {
        SFXVolume = volume;
        foreach (AudioSource sfx in SFXPlayers)
        {
            // 여기서 볼륨을 바꾸더라도 다음 재생 시 InternalPlaySFX에서 다시 덮어씌워짐
            if (sfx != null) sfx.volume = SFXVolume;
        }
    }

    public AudioClip GetSFXClip(string key)
    {
        if (SFXDict != null && SFXDict.TryGetValue(key, out AudioClip clip))
        {
            return clip;
        }
        return null;
    }

}