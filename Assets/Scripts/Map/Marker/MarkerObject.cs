using System;
using UnityEngine;
using UnityEngine.Events;

namespace Map
{
    public class MarkerObject : MonoBehaviour
    {
        private static readonly int PickUpAnimationId = Animator.StringToHash("PickUp");

        [SerializeField] private GameObject coin;

        [SerializeField] private GameObject waypointModel;
        [SerializeField] private GameObject arrowModel;
        [SerializeField] private GameObject checkedModel;

        [SerializeField] private Marker marker;

        public UnityEvent<Marker> onPlayerPickUp;

        private GameObject _player;

        public Guid Id => marker.id;

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
            if (!other.CompareTag("Player") || !marker.IsNext) return;

            // ANIMACION
            PickUpAnimation();

            // ESTADO -> CHECKED
            marker.State = MarkerState.Checked;

            // EVENTO PICK UP
            onPlayerPickUp.Invoke(marker);
        }

        private void Initialize()
        {
            UpdateState(marker.State);

            marker.onStateChange.AddListener(UpdateState);
            marker.onPositionChange.AddListener(_ => UpdatePosition());
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

        private void UpdatePosition()
        {
            transform.position = marker.WorldPosition;
        }


        private void PickUpAnimation()
        {
            coin.GetComponent<Animator>().SetTrigger(PickUpAnimationId);
            coin.GetComponentInChildren<ParticleSystem>().Play();
        }
    }
}