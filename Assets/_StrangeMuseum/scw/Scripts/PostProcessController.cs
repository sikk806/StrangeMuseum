using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PostProcessController : MonoBehaviour
{
    public Volume globalVolume;

    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;

    private ColorAdjustments colorAdjustments;

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
    }

    void Update()
    {
        // 크로마틱 어베레이션 세기 조절 (0~1)
        if (chromaticAberration != null)
        {
            //chromaticAberration.intensity.value = Mathf.PingPong(Time.time * 0.2f, 1f);
        }

        // 렌즈 왜곡 효과 조절 (-1~1)
        if (lensDistortion != null)
        {
            lensDistortion.intensity.value = Mathf.Sin(Time.time) * 0.1f;
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
