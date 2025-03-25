using UnityEngine;

public class MouseLockedHover : MonoBehaviour
{
    [SerializeField] private LayerMask interactableLayer; // 감지할 물리 레이어 (Raycast)
    [SerializeField] private uint outlineRenderingLayer = 1u; // 적용할 렌더링 레이어 (예: Layer 3)

    private GameObject lastHoveredObject;
    private Renderer[] lastHoveredRenderers; //  자식 오브젝트 렌더러 저장
    private uint[] originalLayerMasks; //  원래의 렌더링 레이어 저장

    void Update()
    {
        if (gameObject.CompareTag("Statue")) return;
        Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, interactableLayer))
        {
            GameObject hoveredObject = hit.collider.gameObject;

            if (hoveredObject != lastHoveredObject)
            {
                if (lastHoveredObject != null) SetOutline(lastHoveredObject, false);
                SetOutline(hoveredObject, true);
                lastHoveredObject = hoveredObject;
            }
        }
        else
        {
            if (lastHoveredObject != null)
            {
                SetOutline(lastHoveredObject, false);
                lastHoveredObject = null;
            }
        }
    }

    private void SetOutline(GameObject obj, bool enable)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>(); //  모든 자식의 Renderer 찾기

        if (renderers.Length == 0) return;

        if (enable)
        {
            // 🔹 원래의 렌더링 레이어를 저장하고 Outline Layer 추가
            lastHoveredRenderers = renderers;
            originalLayerMasks = new uint[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                originalLayerMasks[i] = renderers[i].renderingLayerMask;
                renderers[i].renderingLayerMask |= outlineRenderingLayer; //  Outline Layer 추가
            }
        }
        else
        {
            //  원래의 렌더링 레이어 복원
            if (lastHoveredRenderers != null)
            {
                for (int i = 0; i < lastHoveredRenderers.Length; i++)
                {
                    lastHoveredRenderers[i].renderingLayerMask = originalLayerMasks[i];
                }
            }
        }
    }
}
