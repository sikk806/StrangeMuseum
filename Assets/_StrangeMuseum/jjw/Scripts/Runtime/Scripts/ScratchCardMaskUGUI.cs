using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ScratchCard
{
    [RequireComponent(typeof(RawImage))]
    [RequireComponent(typeof(RectTransform))]
    public class ScratchCardMaskUGUI : ScratchCardMask, IBeginDragHandler, IDragHandler
    {
        //  ----- 완료 이벤트 추가 -----
       
        public event System.Action OnWipeComplete;

        private float wipeProgress = 0f;
        private const float RequiredWipeProgress = 0.9f; // 90% 닦으면 완료

        private bool isComplete = false; // 수행 여부 체크

        // 완료 팝업
        public GameObject MissionCanvas; 
        public GameObject CompleteCanvas; 

        private void Update()
        {
            if (grid.Valid)
            {
                wipeProgress = GetRevealProgress(); //  닦은 진행도 업데이트
                if (wipeProgress >= RequiredWipeProgress)
                {
                    CompleteMission();
                }
            }
        }

        private void CompleteMission()
        {
            if (isComplete) return; // 이미 완료된 경우 실행 X

            Debug.Log("미니게임 완료!");
            isComplete = true; // 완료 처리

            OnWipeComplete?.Invoke(); //  이벤트 호출

            //  UI 직접 관리해서 비활성화
            if (MissionCanvas != null) MissionCanvas.SetActive(false);
            if (CompleteCanvas != null) CompleteCanvas.SetActive(false);
        }


        // 여기까지 추가 부분



        private static readonly int OffsetScalePropertyId = Shader.PropertyToID("_OffsetScale");
        
        internal RawImage Image 
        {
            get
            {
                if (image == null)
                {
                    image = GetComponent<RawImage>();
                }

                return image;
            }
        }

        internal ScratchCardGrid Grid 
        {
            get
            {
                return grid;
            }
        }
        
        private RectTransform RectTransform 
        {
            get 
            {
                if (rectTransform == null)
                {
                    rectTransform = GetComponent<RectTransform>();
                }

                return rectTransform;
            }
        }

        [Header("Brush")] 
        [SerializeField] 
        private Material brushMaterial;
        [SerializeField]
        private Vector2 brushScale = new Vector2(0.25f, 0.25f);
        [SerializeField]
        private int interpolationMaxCount = 4;
        [SerializeField]
        private int interpolationMinDistance = 8;
        
        [Header("Mask Texture")]
        [SerializeField] 
        private float maskTextureScale = 1.0f;
        [SerializeField] 
        private float maxMaskTextureSize = 512;

        [Header("Grid")]
        [SerializeField]
        [Range(1, 4)]
        private int gridScale = 2;

        private bool baseTextureSaved;
        private Material brush;
        private ScratchCardGrid grid;
        private RectTransform rectTransform;
        private RenderTexture targetTexture;
        private Texture baseTexture;
        private RawImage image;
        private Vector2 prevDrawPosition;

        private void Awake()
        {
            SaveBaseTexture();
        }

        private void OnEnable()
        {
            Reload();
        }

        private void OnDisable()
        {
            DisposeTargetTexture();
            DisposeBrush();

            if (grid.Valid)
            {
                grid.ClearVisited();
            }
        }
        
        private void OnRectTransformDimensionsChange()
        {
            SaveBaseTexture();
            Reload();
        }
        
        private void Reload()
        {
            var rect = RectTransform.rect;
            if (rect.width <= 0 || rect.height <= 0)
            {
                return;
            }
            
            grid = ScratchCardGrid.Generate(rect, brushScale, Mathf.Max(1, gridScale), grid);
            
            AcquireTargetTexture(rect);
            CreateBrush();
        }

        private void SaveBaseTexture()
        {
            if (!baseTextureSaved)
            {
                if (Image.texture != null)
                {
                    baseTexture = Image.texture;
                }

                baseTextureSaved = true;
            }
        }

        private void CreateBrush()
        {
            if (brushMaterial == null)
            {
                brushMaterial = Resources.Load<Material>("Materials/ScratchCard_DefaultBrush");
            }

            DisposeBrush();
            
            brush = new Material(brushMaterial);
        }

        private void DisposeBrush()
        {
            if (brush != null)
            {
                Destroy(brush);
                brush = null;
            }
        }

        private void AcquireTargetTexture(Rect rect)
        {
            var aspect = rect.height / rect.width;
            var width = (int)Mathf.Min(maxMaskTextureSize, rect.width * maskTextureScale);
            var height = (int)(width * aspect);

            if (targetTexture != null && (targetTexture.width != width || targetTexture.height != height))
            {
                DisposeTargetTexture();
            }

            if (targetTexture == null)
            {
                targetTexture = RenderTexture.GetTemporary(width, height, -1, RenderTextureFormat.ARGB32);
                Image.texture = targetTexture;
                
                ClearTexture();
            }
        }

        private void DisposeTargetTexture()
        {
            if (targetTexture != null)
            {
                RenderTexture.ReleaseTemporary(targetTexture);
                targetTexture = null;
            }
        }

        private void ClearTargetWithColor(Color color)
        {
            var tmp = RenderTexture.active;
            RenderTexture.active = targetTexture;
            GL.Clear(true, true, color);
            RenderTexture.active = tmp;
        }

        private void ClearTexture()
        {
            if (baseTexture != null)
            {
                ClearTargetWithColor(Color.clear);
                Graphics.Blit(baseTexture, targetTexture, Image.material);
            }
            else
            {
                ClearTargetWithColor(Color.white);
            }
        }

        public override float GetRevealProgress()
        {
            return grid.GetVisitedPercent();
        }

        [ContextMenu("Restore")]
        public void Restore()
        {
            grid.ClearVisited();
            ClearTexture();
            OnRevealProgressChanged();
        }

        void IBeginDragHandler.OnBeginDrag(PointerEventData eventData)
        {
            //추가 부분
            if (isComplete) return; // 수행 후 드래그 방지
            //요기까지

            prevDrawPosition = eventData.position;
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            //추가 부분
            if (isComplete) return; // 수행 후 드래그 방지
            //요기까지

            if (targetTexture == null || brush == null)
            {
                return;
            }

            var drawPosition = eventData.position;
            var progressChanged = false;
            
            var delta = drawPosition - prevDrawPosition;
            if (delta != drawPosition)
            {
                var distance = delta.magnitude;
                var count = Mathf.Min(interpolationMaxCount, (int)distance / interpolationMinDistance);

                for (int i = 1; i < count; ++i)
                {
                    progressChanged |= DrawAt(prevDrawPosition + delta * ((float)i / count));
                }
            }
        
            progressChanged |= DrawAt(drawPosition);
            prevDrawPosition = drawPosition;

            if (progressChanged)
            {
                OnRevealProgressChanged();
            }
        }
        
        private bool DrawAt(Vector2 screenPoint)
        {
            var point = ScreenToGraphicLocalPoint(Image, screenPoint);
            var rect = RectTransform.rect;
            var cell = grid.LocalPointToCell(rect, point);
            var changed = grid.SetVisited(cell.x, cell.y, true);
            
            brush.SetVector(OffsetScalePropertyId, CalculateOffsetScale(RectTransform, point, brushScale));
            Graphics.Blit(brush.mainTexture, targetTexture, brush);

            return changed;
        }
        
        internal static Vector2 ScreenToGraphicLocalPoint(Graphic graphic, Vector2 screenPoint)
        {
            var canvas = graphic.canvas.rootCanvas;
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            var rt = graphic.rectTransform;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, screenPoint, camera, out var result) ? result : default;
        }

        internal static Vector4 CalculateOffsetScale(RectTransform rt, Vector2 localPoint, Vector2 scale)
        {
            var rect = rt.rect;
            localPoint += rt.pivot * rect.size;
        
            var aspect = rect.height / rect.width;
            var sx = scale.x * aspect;
            var sy = scale.y;
            var px = (localPoint.x / rect.width) - (0.5f * sx);
            var py = (localPoint.y / rect.height) - (0.5f * sy);

            return new Vector4(px, py, 1.0f / sx, 1.0f / sy);
        }
    }
}