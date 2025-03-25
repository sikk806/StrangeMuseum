using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Vivox;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ChannelInit
{
    [SerializeField]
    private int audibleDistance = 30; // 들을 수 있는 거리

    [SerializeField]
    private int reduceRange = 1; // 소리가 줄어들기 시작하는 거리

    [SerializeField]
    private float soundFadeInDistance = 1.0f; // 자연스럽게 줄어드는 강도 (감쇠)

    [SerializeField]
    private AudioFadeModel audioFadeModel = AudioFadeModel.InverseByDistance;

    public Channel3DProperties GetChannel3DProperties()
    {
        return new Channel3DProperties(audibleDistance, reduceRange, soundFadeInDistance, audioFadeModel);
    }
}

public class VivoxController : MonoBehaviour
{
    public static VivoxController Instance { get; private set; }
    public Image MICMute;

    public GameObject PlayerObject;

    [SerializeField]
    private ChannelInit channelInit;

    [SerializeField]
    private float audioPositionUpdateRate = 0.5f;

    [SerializeField]
    private Sprite MicOn;

    [SerializeField]
    private Sprite MicOff;

    private void Awake()
    {
        Debug.Log("AwakeVivox");
        if (Instance == null)
        {
            DontDestroyOnLoad(gameObject);
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // 채널 입장을 어떤 식으로 처리해아하는지 고민해야함.
    private async void Start()
    {
        Debug.Log("Start");
        try
        {
            await LoginAsync();
        }
        catch (Exception ex)
        {
            Debug.LogError($"Vivox 초기화 중 오류 발생: {ex.Message}");
        }
    }

    private async Task LoginAsync()
    {
        Debug.Log("Vivox 초기화 시작...");
        await VivoxService.Instance.InitializeAsync();
        Debug.Log("Vivox 초기화 완료");

        LoginOptions options = new LoginOptions();
        options.DisplayName = Guid.NewGuid().ToString();
        await VivoxService.Instance.LoginAsync();

    }

    public async Task WaitForVivoxLogin()
    {
        int retryCount = 0;
        while (!VivoxService.Instance.IsLoggedIn && retryCount < 15)
        {
            Debug.Log($"Vivox 채널 입장 전, 로그인 확인 중... (시도 {retryCount + 1}/15)");
            await Task.Delay(500);
            retryCount++;
        }

        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogError("Vivox 로그인 실패! 채널 참가 불가능.");
            return;
        }

        Debug.Log("Vivox 로그인 완료!");
    }

    public async Task JoinVoiceChannel(string channelName, GameObject go)
    {
        await WaitForVivoxLogin();

        // 로그인이 제대로 되었는지 체크
        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.LogError($"Vivox에 로그인되지 않았습니다. 채널 참가 불가능.");
            return;
        }

        // 이미 참가된 채널인지 확인 후 중복 참가 방지
        Debug.Log($"V_Controller : Channel Name : {channelName}");
        if (VivoxService.Instance.ActiveChannels.ContainsKey(channelName))
        {
            Debug.Log($"V_Controller : 이미 참가한 채널 '{channelName}'.");
            return;
        }

        // 채널 참가 시도. 채널 키 값이 두개 이상이면 오류
        Debug.Log($"V_Controller : 채널 참가 시도: {channelName}");
        try
        {
            await VivoxService.Instance.JoinPositionalChannelAsync(channelName, ChatCapability.TextAndAudio, channelInit.GetChannel3DProperties());
            StartCoroutine(UpdatePosition(channelName, go));
            //await VivoxService.Instance.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);
            Debug.Log($"V_Controller : 채널 '{channelName}' 참가 완료!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"V_Controller : Vivox 채널 참가 실패: {ex.Message}");
        }

        foreach (var channel in VivoxService.Instance.ActiveChannels)
        {
            Debug.Log($"현재 활성화 된 Vivxo 채널 이름 : {channel.Key}");
            Debug.Log($"현재 활성화 된 Vivxo 채널 클라이언트 수 : {channel.Value.Count}");
        }
    }

    private IEnumerator UpdatePosition(string channelName, GameObject go)
    {
        while (true)
        {
            //VivoxService.Instance.Set3DPosition(go, channelName);
            yield return new WaitForSeconds(audioPositionUpdateRate);
        }
    }

    public bool ToggleMicrophone(bool voiceOn)
    {
        if (!VivoxService.Instance.IsLoggedIn)
        {
            Debug.Log("Vivox에 로그인 필요");
            throw new InvalidOperationException("Vivox에 로그인되지 않았습니다.");
        }

        var activeChannel = VivoxService.Instance.ActiveChannels.FirstOrDefault().Value;
        if (activeChannel == null)
        {
            Debug.Log("활성화 된 Vivox 채널이 없음.");
        }

        if (voiceOn)
        {
            VivoxService.Instance.MuteInputDevice();
            MICMute.sprite = MicOff;
        }
        else
        {
            VivoxService.Instance.UnmuteInputDevice();
            MICMute.sprite = MicOn;
        }

        voiceOn = !voiceOn;

        return voiceOn;
    }

    //세션 해제하는 코드.
    //Vivox 채널에 아무도 없는 경우 해지하기 위해서 만듦.
    public async Task LeaveVoiceChannel()
    {
        if (VivoxService.Instance.ActiveChannels.Count > 0)
        {
            Debug.Log("기존 Vivox 채널 세션 해제 시작...");
            foreach (var channel in VivoxService.Instance.ActiveChannels)
            {
                await VivoxService.Instance.LeaveChannelAsync(channel.Key);
            }
            Debug.Log("기존 Vivox 채널 세션 해제 시도 중...");
        }

        int retryCount = 0;
        while (VivoxService.Instance.ActiveChannels.Count > 0 && retryCount < 10)
        {
            Debug.Log($"V_Controller : 기존 세션 해제가 완료되지 않아 대기 중... ({retryCount + 1}/10)");
            await Task.Delay(1000);
            retryCount++;
        }
        Debug.Log($"Vivox 채널 수 체크 : {VivoxService.Instance.ActiveChannels.Count}");
        if (VivoxService.Instance.ActiveChannels.Count > 0)
        {
            Debug.LogError("V_Controller : 기존 세션이 완전히 해제되지 않아 새 채널 참가 불가능. 로그아웃 후 재시도.");
            await VivoxService.Instance.LogoutAsync();
            await Task.Delay(2000);
            await VivoxService.Instance.LoginAsync();
        }

        Debug.Log("V_Controller : 기존 Vivox 채널 세션 해제 완료");
    }
}
