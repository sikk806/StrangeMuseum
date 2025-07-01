using Mirror;
using UnityEngine;

public class SoundManager : NetworkBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField]
    private AudioSource bgmAudioSource;
    [SerializeField]
    private AudioSource[] sfxAudioSources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlaySfx(AudioClip clip)
    {
        foreach (AudioSource audio in sfxAudioSources)
        {
            if (!audio.isPlaying)
            {
                audio.volume = PlayerPrefs.GetFloat("SfxVolume", 0.5f);
                audio.PlayOneShot(clip);
                return;
            }
        }

    }

    [Command(requiresAuthority = false)]
    private void CmdPlaySound(string clipName, Vector3 point)
    {
        PlayPointSoundClientRpc(clipName, point); // 모든 클라이언트에 전달
    }
    [ClientRpc]
    private void PlayPointSoundClientRpc(string clipName, Vector3 point)
    {
        AudioClip clip = Resources.Load<AudioClip>("SFX/" + clipName);

        foreach (AudioSource audio in sfxAudioSources)
        {
            if (!audio.isPlaying)
            {
                audio.volume = PlayerPrefs.GetFloat("SfxVolume", 0.5f);

                AudioSource.PlayClipAtPoint(clip, point);

                return;
            }
        }
    }
    public void PlayerAtPointSfx(string clipName, GameObject objectPoint)
    {
        Vector3 point = objectPoint.transform.position;
        CmdPlaySound(clipName, point);

  
    }

    public void ApplyBgmVolume()
    {
        bgmAudioSource.volume = PlayerPrefs.GetFloat("BgmVolume", 0.5f);
    }

    public void PlayBgm(AudioClip clip)
    {
        bgmAudioSource.clip = clip;
        bgmAudioSource.loop = true;
        ApplyBgmVolume();
        bgmAudioSource.Play();
    }

    public void StopBgm()
    {
        bgmAudioSource.Stop();
    }

    public void PauseBgm()
    {
        bgmAudioSource.Pause();
    }

    public void ResumeBgm()
    {
        bgmAudioSource.Play();
    }
}
