using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessController : NetworkBehaviour
{
    private static PostProcessController instance;
    public static PostProcessController Instance
    {
        get
        {
            return instance;
        }
    }
    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // instance 초기화
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    public bool isNearby = false;

    public Volume globalVolume;

    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;
    private ColorAdjustments colorAdjustments;
    private Vignette vignette;

    private float timer;
    private float weirdInterval = 10f; // 몇 초 간격으로 기괴해질지
    private bool isWeird = false;
    private float weirdDuration = 1.5f;

    void Start()
    {
        // VolumeProfile에서 컴포넌트 추출
        if (globalVolume.profile.TryGet(out chromaticAberration))
        {
            chromaticAberration.active = true; // 효과 활성화
        }

        if (globalVolume.profile.TryGet(out lensDistortion))
        {
            lensDistortion.active = true; // 효과 활성화
        }

        if (globalVolume.profile.TryGet(out colorAdjustments))
        {
            colorAdjustments.active = true;
        }

        if (globalVolume.profile.TryGet(out vignette))
        {
            vignette.active = true;
        }
    }

    void Update()
    {
        if (NetworkClient.localPlayer == null) return;

        SecurityController localPc = NetworkClient.localPlayer.GetComponent<SecurityController>();
        if (localPc == null) return;

        if (localPc.isNearby)
        {
            // 크로마틱 어베레이션 세기 조절 (0~1)
            if (chromaticAberration != null)
            {
                //chromaticAberration.intensity.value = Mathf.PingPong(Time.time * 0.2f, 1f);
                chromaticAberration.intensity.value = 1.0f;
            }

            // 렌즈 왜곡 효과 조절 (-1~1)
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = Mathf.Sin(Time.time) * 0.05f;
                //lensDistortion.intensity.value = Mathf.PingPong(Time.time * 0.5f, 0.1f * 2) - 0.1f;
            }

            if (vignette != null)
            {
                vignette.intensity.value = 0.4f;
            }

            timer += Time.deltaTime;

            if (!isWeird && timer >= weirdInterval)
            {
                StartWeirdEffect();
            }

            if (isWeird && timer >= weirdDuration)
            {
                EndWeirdEffect();
            }
        }
        else
        {
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = 0f;
            }

            // 렌즈 왜곡 효과 조절 (-1~1)
            if (lensDistortion != null)
            {
                lensDistortion.intensity.value = 0f;
            }

            if (vignette != null)
            {
                vignette.intensity.value = 0f;
            }
        }
    }

    void StartWeirdEffect()
    {
        isWeird = true;
        timer = 0f;

        // 기괴한 값 설정
        colorAdjustments.contrast.value = Random.Range(-100f, 100f);
        colorAdjustments.saturation.value = Random.Range(-100f, 100f);
        colorAdjustments.hueShift.value = Random.Range(-180f, 180f);
        colorAdjustments.colorFilter.value = new Color(Random.value, Random.value, Random.value);
    }

    void EndWeirdEffect()
    {
        isWeird = false;
        timer = 0f;

        // 정상 값으로 복원
        colorAdjustments.contrast.value = 0f;
        colorAdjustments.saturation.value = 0f;
        colorAdjustments.hueShift.value = 0f;
        colorAdjustments.colorFilter.value = Color.white;

        // 다음 기괴 타이밍 설정
        weirdInterval = Random.Range(5f, 15f);
    }
}
