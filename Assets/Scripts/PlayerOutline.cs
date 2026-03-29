using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Renders a see-through outline around remote players using a two-camera approach.
/// Requires an "Outline" layer in Project Settings -> Tags and Layers.
/// </summary>
public class PlayerOutline : NetworkBehaviour
{
    [SerializeField] private Color outlineColor = Color.red;
    [SerializeField] private float outlineScale = 1.08f;
    [SerializeField] private Transform cameraHolder;

    private static int OutlineLayer = -1;

    private GameObject _outlineMesh;
    private GameObject _outlineCameraObj;

    private void Awake()
    {
        OutlineLayer = LayerMask.NameToLayer("Outline");
    }

    public override void OnStartLocalPlayer()
    {
        // Defer by one frame to ensure PlayerMovement has enabled the camera first
        StartCoroutine(SetupOutlineCameraNextFrame());
    }

    public override void OnStartClient()
    {
        if (isLocalPlayer) return;
        CreateOutlineMesh();
    }

    private IEnumerator SetupOutlineCameraNextFrame()
    {
        yield return null;
        SetupOutlineCamera();
    }

    private void SetupOutlineCamera()
    {
        if (OutlineLayer == -1)
        {
            Debug.LogError("PlayerOutline: 'Outline' layer not found.");
            return;
        }

        Camera mainCam = cameraHolder.GetComponentInChildren<Camera>();
        if (mainCam == null) return;

        // Stop main camera from rendering outline layer — outline camera handles it
        mainCam.cullingMask &= ~(1 << OutlineLayer);

        // Create outline camera as child of main camera so it moves with the player
        _outlineCameraObj = new GameObject("OutlineCamera");
        _outlineCameraObj.transform.SetParent(mainCam.transform, false);

        Camera outlineCam = _outlineCameraObj.AddComponent<Camera>();
        outlineCam.cullingMask = 1 << OutlineLayer;

        // URP Overlay — composites on top of main camera, depth cleared so outlines draw through walls
        var outlineCamData = _outlineCameraObj.AddComponent<UniversalAdditionalCameraData>();
        outlineCamData.renderType = CameraRenderType.Overlay;

        var mainCamData = mainCam.GetComponent<UniversalAdditionalCameraData>();
        if (mainCamData != null)
            mainCamData.cameraStack.Add(outlineCam);
    }

    private void CreateOutlineMesh()
    {
        if (OutlineLayer == -1) return;

        MeshFilter sourceMesh = GetComponentInChildren<MeshFilter>();
        if (sourceMesh == null) return;

        _outlineMesh = new GameObject("OutlineMesh");
        _outlineMesh.layer = OutlineLayer;
        _outlineMesh.transform.SetParent(sourceMesh.transform, false);
        _outlineMesh.transform.localScale = Vector3.one * outlineScale;

        MeshFilter mf = _outlineMesh.AddComponent<MeshFilter>();
        mf.mesh = sourceMesh.sharedMesh;

        MeshRenderer mr = _outlineMesh.AddComponent<MeshRenderer>();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        mat.SetColor("_BaseColor", outlineColor);
        mat.SetInt("_Cull", (int)CullMode.Front);
        mr.material = mat;
    }

    private void OnDestroy()
    {
        if (_outlineMesh != null) Destroy(_outlineMesh);
        if (_outlineCameraObj != null) Destroy(_outlineCameraObj);
    }
}
