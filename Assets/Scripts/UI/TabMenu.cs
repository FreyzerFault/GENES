using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TabMenu : MonoBehaviour
    {
        [SerializeField] private Button[] tabBtns;
        [SerializeField] private RectTransform[] tabMenus;

        [SerializeField] public int initialTab;

        [SerializeField] private Color tabActiveColor;
        [SerializeField] private Color tabInactiveColor;
        private int _activeTab;

        // Start is called before the first frame update
        private void Start()
        {
            for (var i = 0; i < tabBtns.Length; i++)
            {
                var btnIndex = i;
                tabBtns[i].onClick.AddListener(() => OpenTab(btnIndex));
            }

            _activeTab = initialTab;
            OpenTab(initialTab);
        }

        private void OpenTab(int index)
        {
            _activeTab = index;
            tabMenus[_activeTab].SetAsLastSibling();

            for (var i = 0; i < tabBtns.Length; i++)
                tabBtns[i].image.color = i == _activeTab ? tabActiveColor : tabInactiveColor;
        }
    }
}