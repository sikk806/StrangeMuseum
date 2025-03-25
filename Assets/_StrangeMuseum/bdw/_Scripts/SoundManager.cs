using UnityEngine;

public class SoundManager : MonoBehaviour
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
