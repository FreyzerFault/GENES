using UnityEngine;

public class WaterReflection : MonoBehaviour
{
    [Tooltip(
        "The plane where the camera will be reflected, the water plane or any object with the same position and rotation"
    )]
    public Transform reflectionPlane;

    [Tooltip("The texture used by the Water shader to display the reflection")]
    public RenderTexture outputTexture;

    // parameters
    public bool copyCameraParamerers;
    public float verticalOffset;

    private bool isReady;

    // referenses
    private Camera mainCamera;

    // cache
    private Transform mainCamTransform;
    private Camera reflectionCamera;
    private Transform reflectionCamTransform;

    public void Awake()
    {
        mainCamera = Camera.main;

        reflectionCamera = GetComponent<Camera>();

        Validate();
    }

    private void Update()
    {
        if (isReady) RenderReflection();
    }

    private void RenderReflection()
    {
        // take main camera directions and position world space
        var cameraDirectionWorldSpace = mainCamTransform.forward;
        var cameraUpWorldSpace = mainCamTransform.up;
        var cameraPositionWorldSpace = mainCamTransform.position;

        cameraPositionWorldSpace.y += verticalOffset;

        // transform direction and position by reflection plane
        var cameraDirectionPlaneSpace = reflectionPlane.InverseTransformDirection(cameraDirectionWorldSpace);
        var cameraUpPlaneSpace = reflectionPlane.InverseTransformDirection(cameraUpWorldSpace);
        var cameraPositionPlaneSpace = reflectionPlane.InverseTransformPoint(cameraPositionWorldSpace);

        // invert direction and position by reflection plane
        cameraDirectionPlaneSpace.y *= -1;
        cameraUpPlaneSpace.y *= -1;
        cameraPositionPlaneSpace.y *= -1;

        // transform direction and position from reflection plane local space to world space
        cameraDirectionWorldSpace = reflectionPlane.TransformDirection(cameraDirectionPlaneSpace);
        cameraUpWorldSpace = reflectionPlane.TransformDirection(cameraUpPlaneSpace);
        cameraPositionWorldSpace = reflectionPlane.TransformPoint(cameraPositionPlaneSpace);

        // apply direction and position to reflection camera
        reflectionCamTransform.position = cameraPositionWorldSpace;
        reflectionCamTransform.LookAt(cameraPositionWorldSpace + cameraDirectionWorldSpace, cameraUpWorldSpace);
    }

    private void Validate()
    {
        if (mainCamera != null)
        {
            mainCamTransform = mainCamera.transform;
            isReady = true;
        }
        else
        {
            isReady = false;
        }

        if (reflectionCamera != null)
        {
            reflectionCamTransform = reflectionCamera.transform;
            isReady = true;
        }
        else
        {
            isReady = false;
        }

        if (isReady && copyCameraParamerers)
        {
            copyCameraParamerers = !copyCameraParamerers;
            reflectionCamera.CopyFrom(mainCamera);

            reflectionCamera.targetTexture = outputTexture;
        }
    }
}