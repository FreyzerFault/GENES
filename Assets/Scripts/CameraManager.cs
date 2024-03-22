using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera[] _cams;
    private ICinemachineCamera _activeCam;
    private int _currentCamIndex;

    private void Awake()
    {
        var brain = GetComponent<CinemachineBrain>();
        _activeCam = brain.ActiveVirtualCamera;
        if (_cams.Length == 0) _cams = FindObjectsOfType<CinemachineVirtualCamera>();
    }

    private void Start()
    {
        ResetCamPriority();
        ChangeToCam(0);
    }

    private void ResetCamPriority()
    {
        foreach (var cam in _cams) cam.Priority = 10;
    }

    private void ChangeToCam(int index)
    {
        if (index >= _cams.Length) return;

        _cams[_currentCamIndex].Priority = 10;
        _currentCamIndex = index;
        _cams[index].Priority = 100;
    }

    private void OnNextCam()
    {
        ChangeToCam(_currentCamIndex + 1);
    }

    private void OnCam1()
    {
        ChangeToCam(0);
    }

    private void OnCam2()
    {
        ChangeToCam(1);
    }

    private void OnCam3()
    {
        ChangeToCam(2);
    }

    private void OnCam4()
    {
        ChangeToCam(3);
    }

    private void OnCam5()
    {
        ChangeToCam(4);
    }

    private void OnCam6()
    {
        ChangeToCam(5);
    }
}