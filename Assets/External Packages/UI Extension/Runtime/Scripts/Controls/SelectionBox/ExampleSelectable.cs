/// Original Credit Korindian
/// Sourced from - http://forum.unity3d.com/threads/rts-style-drag-selection-box.265739/
/// Updated Credit BenZed
/// Sourced from - http://forum.unity3d.com/threads/color-picker.267043/

using TMPro;

namespace UnityEngine.UI.Extensions
{
    public class ExampleSelectable : MonoBehaviour, IBoxSelectable
    {
        #region Implemented members of IBoxSelectable

        public bool selected { get; set; } = false;

        public bool preSelected { get; set; } = false;

        #endregion

        //We want the test object to be either a UI element, a 2D element or a 3D element, so we'll get the appropriate components
        private SpriteRenderer spriteRenderer;
        private Image image;
#if UNITY_2022_1_OR_NEWER
        private TMP_Text text;
#else
        Text text;
#endif

        private void Start()
        {
            spriteRenderer = transform.GetComponent<SpriteRenderer>();
            image = transform.GetComponent<Image>();
#if UNITY_2022_1_OR_NEWER
            text = transform.GetComponent<TMP_Text>();
#else
            text = transform.GetComponent<Text>();
#endif
        }

        private void Update()
        {
            //What the game object does with the knowledge that it is selected is entirely up to it.
            //In this case we're just going to change the color.

            //White if deselected.
            var color = Color.white;

            if (preSelected)
                //Yellow if preselected
                color = Color.yellow;
            if (selected)
                //And green if selected.
                color = Color.green;

            //Set the color depending on what the game object has.
            if (spriteRenderer)
                spriteRenderer.color = color;
            else if (text)
                text.color = color;
            else if (image)
                image.color = color;
            else if (GetComponent<Renderer>()) GetComponent<Renderer>().material.color = color;
        }
    }
}