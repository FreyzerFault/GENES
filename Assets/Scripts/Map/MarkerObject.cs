using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    public class MarkerObject : MonoBehaviour
    {
        private static readonly int PickUp = Animator.StringToHash("PickUp");
        [SerializeField] private GameObject waypointModel;
        [SerializeField] private GameObject coin;
        [SerializeField] private GameObject arrowModel;
        [SerializeField] private GameObject checkedModel;

        public UnityEvent<Marker> OnPlayerPickUp;

        [SerializeField] private Marker marker;

        private GameObject _player;

        public GUID Id => marker.id;

        public Marker Data
        {
            get => marker;
            set
            {
                marker = value;
                Initialize();
            }
        }

        private void Start()
        {
            Initialize();
        }


        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("OnTriggerEnter");
            if (other.CompareTag("Player") && marker.IsNext) OnPlayerPickUp.Invoke(marker);
        }

        private void Initialize()
        {
            OnPlayerPickUp = new UnityEvent<Marker>();
            OnPlayerPickUp.AddListener(_ => PickUpAnimation());
            OnPlayerPickUp.AddListener(_ => marker.State = MarkerState.Checked);

            UpdateState(marker.State);
        }

        private void UpdateState(MarkerState state)
        {
            waypointModel.SetActive(false);
            arrowModel.SetActive(false);
            checkedModel.SetActive(false);
            switch (state)
            {
                case MarkerState.Checked:
                    waypointModel.SetActive(true);
                    break;
                case MarkerState.Unchecked:
                    checkedModel.SetActive(true);
                    break;
                case MarkerState.Next:
                    arrowModel.SetActive(true);
                    break;
            }
        }


        private void PickUpAnimation()
        {
            coin.GetComponent<Animator>().SetTrigger(PickUp);
            coin.GetComponentInChildren<ParticleSystem>().Play();
        }
    }
}