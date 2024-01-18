using UnityEngine;
using UnityEngine.Events;

namespace Map.Markers
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
            marker.onStateChange += HandleOnStateChange;
            marker.onPositionChange += HandleOnPositionChange;
        }

        private void OnDestroy()
        {
            marker.onStateChange -= HandleOnStateChange;
            marker.onPositionChange -= HandleOnPositionChange;
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
            UpdateState();
        }

        private void HandleOnStateChange(object sender, MarkerState state)
        {
            UpdateState();
        }

        private void HandleOnPositionChange(object sender, Vector2 pos)
        {
            UpdatePosition();
        }

        private void UpdateState()
        {
            coin.SetActive(false);
            waypointModel.SetActive(false);
            arrowModel.SetActive(false);
            checkedModel.SetActive(false);
            switch (marker.State)
            {
                case MarkerState.Unchecked:
                    waypointModel.SetActive(true);
                    coin.SetActive(true);
                    break;
                case MarkerState.Checked:
                    checkedModel.SetActive(true);
                    break;
                case MarkerState.Next:
                    arrowModel.SetActive(true);
                    coin.SetActive(true);
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