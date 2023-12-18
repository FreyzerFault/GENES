using Cinemachine;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private ICinemachineCamera _activeCam;
    private CinemachineVirtualCamera[] _cams;
    private int _currentCamIndex;

    private void Awake()
    {
        var brain = GetComponent<CinemachineBrain>();
        _activeCam = brain.ActiveVirtualCamera;
        _cams = FindObjectsOfType<CinemachineVirtualCamera>();

        ResetCamPriority();
        ChangeToCam(1);
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
        ChangeToCam(1);
    }

    private void OnCam2()
    {
        ChangeToCam(2);
    }

    private void OnCam3()
    {
        ChangeToCam(3);
    }

    private void OnCam4()
    {
        ChangeToCam(4);
    }

    private void OnCam5()
    {
        ChangeToCam(5);
    }

    private void OnCam6()
    {
        ChangeToCam(6);
    }
}