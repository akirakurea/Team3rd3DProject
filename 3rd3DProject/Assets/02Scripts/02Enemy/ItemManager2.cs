using System.Collections.Generic;
using UnityEngine;

public class ItemManager2 : MonoBehaviour
{
    public static ItemManager2 Instance { get; private set; }
    [SerializeField] private List<GameObject> remainingItems = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public List<GameObject> GetRemainingItems() => remainingItems;
    public void RemoveItem(Transform item) => remainingItems.Remove(item.gameObject);
}
