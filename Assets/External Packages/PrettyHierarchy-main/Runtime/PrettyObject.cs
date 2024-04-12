using UnityEditor;
using UnityEngine;

namespace PrettyHierarchy
{
	[DisallowMultipleComponent]
	public class PrettyObject : MonoBehaviour
	{
#if UNITY_EDITOR
		//[Header("Background")]
		[SerializeField]
		private bool useDefaultBackgroundColor;
		[SerializeField]
		private Color32 backgroundColor = new(0, 0, 0, 255);
		//[Header("Text")]
		[SerializeField]
		private bool useDefaultTextColor;
		[SerializeField]
		private Color32 textColor = new(255, 255, 255, 255);
		[SerializeField]
		private Font font;
		[SerializeField]
		private int fontSize = 14;
		[SerializeField]
		private FontStyle fontStyle = FontStyle.Bold;
		[SerializeField]
		private TextAnchor alignment = TextAnchor.MiddleCenter;
		[SerializeField]
		private bool textDropShadow;

		public bool UseDefaultBackgroundColor => useDefaultBackgroundColor;
		public Color32 BackgroundColor => new(backgroundColor.r, backgroundColor.g, backgroundColor.b, 255);

		public bool UseDefaultTextColor => useDefaultTextColor;
		public Color32 TextColor => textColor;
		public Font Font => font;
		public int FontSize => fontSize;
		public FontStyle FontStyle => fontStyle;
		public TextAnchor Alignment => alignment;
		public bool TextDropShadow => textDropShadow;

		private void OnValidate() => EditorApplication.RepaintHierarchyWindow();
#endif
	}
}
