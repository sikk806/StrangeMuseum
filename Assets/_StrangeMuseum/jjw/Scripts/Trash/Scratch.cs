using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scratch : MonoBehaviour
{
    public GameObject MiniGameUI;
    public RawImage ScratchArea;
    public GameObject Player;
    public TMP_Text progressText;
    public TMP_Text completionMessage; // 완료 메시지 출력
    public float interactionDistance = 3f;
    private bool isMiniGameActive = false;
    private MoveTest playerMovement;
    private RenderTexture scratchTexture;
    public Texture2D brushTexture;
    public float BrushSize = 100f;
    private float progress = 0f;

    void Start()
    {
        MiniGameUI.SetActive(false);
        completionMessage.gameObject.SetActive(false); // 완료 메시지 비활성화
        playerMovement = Player.GetComponent<MoveTest>();

        int width = (int)ScratchArea.rectTransform.rect.width;
        int height = (int)ScratchArea.rectTransform.rect.height;
        scratchTexture = new RenderTexture(width, height, 0);
        scratchTexture.format = RenderTextureFormat.ARGB32;
        scratchTexture.graphicsFormat = UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_UNorm;
        scratchTexture.Create();
        ScratchArea.texture = scratchTexture;

        ClearMask();
    }

    void Update()
    {
        if (isMiniGameActive && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelMiniGame();
        }

        if (Input.GetKeyDown(KeyCode.G))
        {
            float distance = Vector3.Distance(Player.transform.position, transform.position);
            if (distance <= interactionDistance)
            {
                OpenMiniGame();
            }
            else
            {
                Debug.Log("너무 멀어서 미니게임을 시작할 수 없습니다.");
            }
        }

        if (isMiniGameActive && Input.GetMouseButton(0))
        {
            Vector2 mousePos = Input.mousePosition;
            RectTransform rt = ScratchArea.rectTransform;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, mousePos, null, out localPoint);

            float normalizedX = (localPoint.x - rt.rect.x) / rt.rect.width;
            float normalizedY = (localPoint.y - rt.rect.y) / rt.rect.height;

            Vector2 renderTexturePoint = new Vector2(
                normalizedX * scratchTexture.width,
                (1 - normalizedY) * scratchTexture.height
            );

            ScratchCard(renderTexturePoint);
            UpdateProgress();
        }
    }

    public void OpenMiniGame()
    {
        MiniGameUI.SetActive(true);
        isMiniGameActive = true;
        completionMessage.gameObject.SetActive(false); // 완료 메시지 숨기기
        if (playerMovement != null) playerMovement.enabled = false;
        progress = 0f;
        UpdateProgress();
    }

    public void CloseMiniGame()
    {
        MiniGameUI.SetActive(false);
        isMiniGameActive = false;
        if (playerMovement != null) playerMovement.enabled = true;
    }

    public void CancelMiniGame()
    {
        ClearMask();
        CloseMiniGame();
    }

    public void ClearMask()
    {
        RenderTexture.active = scratchTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = null;
        progress = 0f;
        UpdateProgress();
    }

    public void ScratchCard(Vector2 position)
    {
        // RenderTexture를 Texture2D로 변환
        Texture2D tempTexture = new Texture2D(scratchTexture.width, scratchTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = scratchTexture;
        tempTexture.ReadPixels(new Rect(0, 0, scratchTexture.width, scratchTexture.height), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        int brushRadius = Mathf.RoundToInt(BrushSize / 2);
        int centerX = Mathf.RoundToInt(position.x);
        int centerY = Mathf.RoundToInt(position.y);

        // 특정 영역만 투명하게 변경
        for (int x = -brushRadius; x <= brushRadius; x++)
        {
            for (int y = -brushRadius; y <= brushRadius; y++)
            {
                int pixelX = centerX + x;
                int pixelY = centerY + y;

                if (pixelX >= 0 && pixelX < tempTexture.width && pixelY >= 0 && pixelY < tempTexture.height)
                {
                    Color pixelColor = tempTexture.GetPixel(pixelX, pixelY);
                    pixelColor.a = 0f; // 완전히 투명하게 설정
                    tempTexture.SetPixel(pixelX, pixelY, pixelColor);
                }
            }
        }

        tempTexture.Apply();

        // 변경된 텍스처를 RenderTexture에 적용
        RenderTexture.active = scratchTexture;
        Graphics.Blit(tempTexture, scratchTexture);
        RenderTexture.active = null;

        Destroy(tempTexture);
    }

    public void UpdateProgress()
    {
        Texture2D tempTexture = new Texture2D(scratchTexture.width, scratchTexture.height, TextureFormat.ARGB32, false);
        RenderTexture.active = scratchTexture;
        tempTexture.ReadPixels(new Rect(0, 0, scratchTexture.width, scratchTexture.height), 0, 0);
        tempTexture.Apply();
        RenderTexture.active = null;

        int totalPixels = scratchTexture.width * scratchTexture.height;
        int erasedPixels = 0;

        Color[] pixels = tempTexture.GetPixels();
        foreach (Color pixel in pixels)
        {
            if (pixel.a < 0.9f)
            {
                erasedPixels++;
            }
        }

        progress = ((float)erasedPixels / totalPixels) * 100f;

        if (progressText != null)
        {
            progressText.text = $"진행도: {progress:F1}%";
        }
        else
        {
            Debug.LogError("progressText가 설정되지 않았습니다!");
        }

        if (progress >= 100f)
        {
            completionMessage.gameObject.SetActive(true);
            completionMessage.text = "미니게임 완료!";
            Debug.Log("미니게임 완료!");
        }
    


    /*Debug.Log("UpdateProgress() 호출됨");

    Texture2D tempTexture = new Texture2D(scratchTexture.width, scratchTexture.height, TextureFormat.ARGB32, false);

    RenderTexture.active = scratchTexture;
    tempTexture.ReadPixels(new Rect(0, 0, scratchTexture.width, scratchTexture.height), 0, 0);
    tempTexture.Apply();
    RenderTexture.active = null;

    Debug.Log("RenderTexture에서 픽셀 데이터 읽기 완료");

    int totalPixels = scratchTexture.width * scratchTexture.height;
    int erasedPixels = 0;

    Color[] pixels = tempTexture.GetPixels();
    Debug.Log($"총 픽셀 개수: {pixels.Length}");

    foreach (Color pixel in pixels)
    {
        if (pixel.a < 1.0f) // 투명도가 OO 이하인 경우 지워진 것으로 간주
        {
            erasedPixels++;
        }
    }

    Debug.Log($"전체 픽셀: {totalPixels}, 지워진 픽셀: {erasedPixels}, 진행도: {progress}%");

    // 픽셀 값이 정상적으로 변하고 있는지 확인
    Debug.Log($"픽셀 샘플값 (첫 번째 픽셀): R={pixels[0].r}, G={pixels[0].g}, B={pixels[0].b}, A={pixels[0].a}");


    progress = ((float)erasedPixels / totalPixels) * 100f;

    Debug.Log($"전체 픽셀: {totalPixels}, 지워진 픽셀: {erasedPixels}, 진행도: {progress}%");

    if (progressText != null)
    {
        progressText.text = $"진행도: {progress:F1}%";
        Debug.Log($"진행도 업데이트 완료: {progress:F1}%");
    }
    else
    {
        Debug.LogError("progressText가 설정되지 않았습니다!");
    }

    if (progress >= progressThreshold)
    {
        Debug.Log("미니게임 완료!");
        CloseMiniGame();
    }*/
}

    /* public GameObject MiniGameUI;  // 미니게임 UI 패널
     public RawImage ScratchArea;   // 긁을 영역 (RenderTexture 사용)
     public GameObject Player;      // 플레이어
     public float interactionDistance = 3f; // 상호작용 거리
     private bool isMiniGameActive = false;
     private MoveTest playerMovement;
     private RenderTexture scratchTexture;
     public Texture2D brushTexture; // 투명한 브러시 텍스처
     public float BrushSize = 50f;

     void Start()
     {
         MiniGameUI.SetActive(false);
         playerMovement = Player.GetComponent<MoveTest>();

         // RenderTexture 동적 생성
         scratchTexture = new RenderTexture(1920, 1080, 0);
         scratchTexture.format = RenderTextureFormat.ARGB32;
         scratchTexture.Create();
         ScratchArea.texture = scratchTexture;

         // 초기화 (검은색으로 덮기)
         ClearMask();

         // brushTexture 디버깅
         if (brushTexture == null)
         {
             Debug.LogError("BrushTexture가 설정되지 않았습니다!");
         }
         else
         {
             Debug.Log($"BrushTexture 확인: Format={brushTexture.format}, Size=({brushTexture.width}x{brushTexture.height})");

         }
     }

     void Update()
     {
         // ESC 키를 누르면 미니게임 종료
         if (isMiniGameActive && Input.GetKeyDown(KeyCode.Escape))
         {
             CloseMiniGame();
         }

         // G 버튼을 눌렀을 때, 일정 거리 내에서만 미니게임 시작 가능
         if (Input.GetKeyDown(KeyCode.G))
         {
             float distance = Vector3.Distance(Player.transform.position, transform.position);
             if (distance <= interactionDistance)
             {
                 OpenMiniGame();
             }
             else
             {
                 Debug.Log("너무 멀어서 미니게임을 시작할 수 없습니다.");
             }
         }

         // 마우스 클릭 시 스크래치 동작
         if (isMiniGameActive && Input.GetMouseButton(0))
         {
             Vector2 mousePos = Input.mousePosition;


             // UI 좌표를 RenderTexture 좌표로 변환
             RectTransform rt = ScratchArea.rectTransform;
             Vector2 localPoint;
             RectTransformUtility.ScreenPointToLocalPointInRectangle(ScratchArea.rectTransform, mousePos, null, out localPoint);

             // RenderTexture 크기에 맞게 좌표 변환
             float normalizedX = (localPoint.x - rt.rect.x) / rt.rect.width;
             float normalizedY = (localPoint.y - rt.rect.y) / rt.rect.height;

             Vector2 renderTexturePoint = new Vector2(
                 normalizedX * scratchTexture.width,
                 (1 - normalizedY) * scratchTexture.height // Y축 반전
             );

             ScratchCard(renderTexturePoint);//localPoint);
         }
     }

     public void OpenMiniGame()
     {
         MiniGameUI.SetActive(true);
         isMiniGameActive = true;
         if (playerMovement != null) playerMovement.enabled = false; // 플레이어 움직임 제한
     }

     public void CloseMiniGame()
     {
         MiniGameUI.SetActive(false);
         isMiniGameActive = false;
         if (playerMovement != null) playerMovement.enabled = true; // 플레이어 움직임 다시 활성화
     }

     public void ClearMask()
     {
         // RenderTexture 초기화 (검은색으로 덮기)
         RenderTexture.active = scratchTexture;
         GL.Clear(true, true, Color.black);
         RenderTexture.active = null;
         Debug.Log("RenderTexture가 검은색으로 초기화되었습니다.");
     }

     public void ScratchCard(Vector2 position)
     {
         // RenderTexture에 마스크 적용 (마우스 드래그 시 실행)
         RenderTexture.active = scratchTexture;


         GL.PushMatrix();
         GL.LoadPixelMatrix(0, scratchTexture.width, scratchTexture.height, 0);

         // 브러시를 여러 번 찍어서 넓은 영역을 자연스럽게 지우기
         int step = (int)(BrushSize / 4); // 브러시 크기에 따라 반복 횟수 결정
         for (int x = -step; x <= step; x += 5)
         {
             for (int y = -step; y <= step; y += 5)
             {
                 Vector2 brushPos = new Vector2(position.x + x, position.y + y);
                 Graphics.DrawTexture(new Rect(brushPos.x - BrushSize / 2, brushPos.y - BrushSize / 2, BrushSize, BrushSize), brushTexture);
             }
         }
         GL.PopMatrix();
         RenderTexture.active = null;

         Debug.Log("테스트용 렌더링 완료." + position);
     }*/

    /* public GameObject MaskPrefab;
     bool isPressed = false;

     void Update()
     {
         var mousePoint = Input.mousePosition;
         mousePoint.z = 2;
         mousePoint = Camera.main.ScreenToWorldPoint(mousePoint);

         if (isPressed == true)
         {
             GameObject maskSprite = Instantiate(MaskPrefab, mousePoint, Quaternion.identity);
             maskSprite.transform.parent = gameObject.transform;
         }

         if (Input.GetMouseButtonDown(0))
         {
             Invoke("Reveal", 10);
             isPressed = true;
         }
         else if (Input.GetMouseButtonUp(0))
         {
             isPressed = false;
         }
     }

     void Reveal()
     {
         Destroy(this.gameObject);
     }*/

}
