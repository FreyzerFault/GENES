using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    public class MapMarkerObject : MonoBehaviour
    {
        private static readonly int PickUp = Animator.StringToHash("PickUp");
        [SerializeField] private GameObject waypointIcon;
        [SerializeField] private GameObject coinIcon;
        [SerializeField] private GameObject arrowIcon;

        public UnityEvent<GUID> OnPlayerPickUp = new();

        public GUID id;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("OnTriggerEnter");
            if (other.CompareTag("Player"))
            {
                OnPlayerPickUp.Invoke(id);
                PickUpAnimation();
            }
        }

        private void PickUpAnimation()
        {
            coinIcon.GetComponent<Animator>().SetTrigger(PickUp);
            coinIcon.GetComponentInChildren<ParticleSystem>().Play();
        }
    }
}