using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

public class CreateDynamicScrollSnap : MonoBehaviour
{
    [SerializeField] private GameObject ScrollSnapPrefab;

    [SerializeField] private GameObject ScrollSnapContent;

    [SerializeField] private int startingPage;

    private HorizontalScrollSnap hss;

    private bool isInitialized;

    // Start is called before the first frame update
    private void Start()
    {
        hss = Instantiate(ScrollSnapPrefab, transform).GetComponent<HorizontalScrollSnap>();
        hss.ChangePage(0);
    }

    // Update is called once per frame
    private void Update()
    {
        if (!isInitialized && hss != null && Input.GetKeyDown(KeyCode.K))
        {
            AddHSSChildren();
            isInitialized = true;
        }
    }

    private void AddHSSChildren()
    {
        var contentGO = hss.transform.Find("Content");
        if (contentGO != null)
        {
            for (var i = 0; i < 10; i++)
            {
                var item = Instantiate(ScrollSnapContent);
                SetHSSItemTest(item, $"Page {i}");
                hss.AddChild(item);
            }

            hss.StartingScreen = startingPage;
            hss.UpdateLayout(true);
        }
        else
        {
            Debug.LogError("Content not found");
        }
    }

    private void SetHSSItemTest(GameObject prefab, string value)
    {
        prefab.gameObject.name = value;
        prefab.GetComponentInChildren<Text>().text = value;
    }
}