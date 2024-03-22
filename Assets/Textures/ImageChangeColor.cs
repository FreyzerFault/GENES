using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ImageChangeColor : MonoBehaviour
    {
        [SerializeField] private Image image;
        [SerializeField] private Color initialColor;
        [SerializeField] private Color secondaryColor;

        private void Awake()
        {
            image ??= GetComponent<Image>();
            initialColor = image.color;
        }

        public void ChangeColor()
        {
            if (image.color == initialColor)
                image.color = secondaryColor;
            else
                SetBaseColor();
        }

        public void SetBaseColor()
        {
            image.color = initialColor;
        }
    }
}