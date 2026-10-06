using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// 고양이에 붙인다. F키: 근처에 아이템이 있으면 줍고, 없으면 선택한 칸의 아이템을 앞에 설치한다.
/// </summary>
public class ItemInteractor : MonoBehaviour
{
    [SerializeField] private float pickupRange = 1.5f;
    [SerializeField] private float placeDistance = 1f;

    private void Update()
    {
        if (Time.timeScale <= 0f || !FPressed()) return;

        var hb = HotbarManager.Instance;
        var pickup = FindPickup();
        if (pickup != null)
        {
            if (hb.TryAdd(pickup.Item)) Destroy(pickup.gameObject);
            return;
        }

        var item = hb.SelectedItem;
        if (item == null || item.placePrefab == null) return;
        Instantiate(item.placePrefab, transform.position + transform.forward * placeDistance, Quaternion.identity);
        hb.ConsumeSelected();
    }

    private ItemPickup FindPickup()
    {
        foreach (var c in Physics.OverlapSphere(transform.position, pickupRange))
            if (c.TryGetComponent(out ItemPickup p)) return p;
        return null;
    }

    private static bool FPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}