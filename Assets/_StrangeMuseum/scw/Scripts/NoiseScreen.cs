using UnityEngine;
using UnityEngine.UI;

public class NoiseScreen : MonoBehaviour
{
    public Sprite[] frames;          // 애니메이션에 사용할 스프라이트들
    public float frameRate = 10f;    // 초당 프레임 수

    private Image noiseImage;        // 자동으로 가져올 이미지 컴포넌트
    private float timer;
    private int currentFrame;

    void Awake()
    {
        noiseImage = GetComponent<Image>();
    }

    void Update()
    {
        if (frames.Length == 0 || !noiseImage) return;

        timer += Time.deltaTime;
        if (timer >= 1f / frameRate)
        {
            timer -= 1f / frameRate;
            currentFrame = (currentFrame + 1) % frames.Length;
            noiseImage.sprite = frames[currentFrame];
        }
    }
}
