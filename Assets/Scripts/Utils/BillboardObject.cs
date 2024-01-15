using ExtensionMethods;
using UnityEngine;

public class BillboardObject : MonoBehaviour
{
    private GameObject _player;

    private void Awake()
    {
        _player = GameObject.FindWithTag("Player");
    }

    private void Update()
    {
        transform.Billboard(_player.transform);
    }
}