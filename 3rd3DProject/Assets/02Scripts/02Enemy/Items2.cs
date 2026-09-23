using UnityEngine;

public class Items2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(ItemManager2.Instance != null)
            ItemManager2.Instance.AddItem(gameObject);
    }

    private void OnDestroy()
    {
        if (ItemManager2.Instance != null)
            ItemManager2.Instance.RemoveItem(transform);
    }
}
