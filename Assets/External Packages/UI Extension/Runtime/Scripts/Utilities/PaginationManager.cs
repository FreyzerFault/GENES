using System.Collections.Generic;
using System.Linq;

/// Credit Brogan King (@BroganKing)
/// Original Sourced from - https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/issues/158/pagination-script

namespace UnityEngine.UI.Extensions
{
    [AddComponentMenu("UI/Extensions/Pagination Manager")]
    public class PaginationManager : ToggleGroup
    {
        [SerializeField] private ScrollSnapBase scrollSnap;

        private bool isAClick;
        private List<Toggle> m_PaginationChildren;

        protected PaginationManager()
        {
        }

        public int CurrentPage => scrollSnap.CurrentPage;


        // Use this for initialization
        protected override void Start()
        {
            base.Start();

            if (scrollSnap == null)
            {
                Debug.LogError("A ScrollSnap script must be attached");
                return;
            }

            // do not want the scroll snap pagination
            if (scrollSnap.Pagination) scrollSnap.Pagination = null;

            // set scroll snap listeners
            scrollSnap.OnSelectionPageChangedEvent.AddListener(SetToggleGraphics);
            scrollSnap.OnSelectionChangeEndEvent.AddListener(OnPageChangeEnd);

            ResetPaginationChildren();
        }

        /// <summary>
        ///     Remake the internal list of child toggles (m_PaginationChildren).
        ///     Used after adding/removing a toggle.
        /// </summary>
        public void ResetPaginationChildren()
        {
            // add selectables to list
            m_PaginationChildren = GetComponentsInChildren<Toggle>().ToList();
            for (var i = 0; i < m_PaginationChildren.Count; i++)
            {
                m_PaginationChildren[i].onValueChanged.AddListener(ToggleClick);
                m_PaginationChildren[i].group = this;
                m_PaginationChildren[i].isOn = false;
            }

            // set toggles on start
            SetToggleGraphics(CurrentPage);

            // warn user that they have uneven amount of pagination toggles to page count
            if (m_PaginationChildren.Count != scrollSnap._scroll_rect.content.childCount)
                Debug.LogWarning("Uneven pagination icon to page count");
        }

        /// <summary>
        ///     Calling from other scripts if you need to change screens programmatically
        /// </summary>
        /// <param name="pageNo"></param>
        public void GoToScreen(int pageNo)
        {
            scrollSnap.GoToScreen(pageNo, true);
        }


        /// <summary>
        ///     Calls GoToScreen() based on the index of toggle that was pressed
        /// </summary>
        /// <param name="target"></param>
        private void ToggleClick(Toggle target)
        {
            if (!target.isOn)
            {
                isAClick = true;
                GoToScreen(m_PaginationChildren.IndexOf(target));
            }
        }

        private void ToggleClick(bool toggle)
        {
            if (toggle)
                for (var i = 0; i < m_PaginationChildren.Count; i++)
                    if (m_PaginationChildren[i].isOn && !scrollSnap._suspendEvents)
                    {
                        GoToScreen(i);
                        break;
                    }
        }

        /// <summary>
        ///     Calls GoToScreen() based on the index of toggle that was pressed
        /// </summary>
        /// <param name="target"></param>
        private void ToggleClick(int target)
        {
            isAClick = true;
            GoToScreen(target);
        }

        private void SetToggleGraphics(int pageNo)
        {
            if (!isAClick) m_PaginationChildren[pageNo].isOn = true;
            //for (int i = 0; i < m_PaginationChildren.Count; i++)
            //{
            //    m_PaginationChildren[i].isOn = pageNo == i ? true : false;
            //}
        }

        private void OnPageChangeEnd(int pageNo)
        {
            isAClick = false;
        }
    }
}