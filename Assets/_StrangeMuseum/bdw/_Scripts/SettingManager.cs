using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingManager : MonoBehaviour
{
    public static SettingManager Instance { get; private set; }

    [SerializeField]
    private Slider bgmVolumeSlider;
    [SerializeField]
    private Slider sfxVolumeSlider;

    public AudioClip ButtonSfx;

    private void Awake()
    {
        gameObject.SetActive(true);

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (SceneManager.GetActiveScene().name == "PlayScene")
        {
            SoundManager.Instance.PlaySfx(ButtonSfx);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        bgmVolumeSlider.value = PlayerPrefs.GetFloat("BgmVolume", 0.5f);
        sfxVolumeSlider.value = PlayerPrefs.GetFloat("SfxVolume", 0.5f);
    }

    private void OnDisable()
    {
        SoundManager.Instance.PlaySfx(ButtonSfx);
        if (SceneManager.GetActiveScene().name == "PlayScene")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void SaveSettings()
    {
        SoundManager.Instance.PlaySfx(ButtonSfx);
        PlayerPrefs.SetFloat("BgmVolume", bgmVolumeSlider.value);
        PlayerPrefs.SetFloat("SfxVolume", sfxVolumeSlider.value);

        SoundManager.Instance.ApplyBgmVolume();

        gameObject.SetActive(false);
    }

    public void Quit()
    {
        SoundManager.Instance.PlaySfx(ButtonSfx);
        Application.Quit();
    }
}
