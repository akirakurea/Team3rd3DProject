using System;
using Unity.VisualScripting;
using UnityEngine;

public class ThiefShotReceiver : MonoBehaviour, IShotReceiver
{
    [SerializeField] private ThiefController thief;
    [SerializeField] private float stunDuration = 1.5f;
  
    public void ReceiveShot(ShotHit hit)
    {
        thief.TakeStun(stunDuration);
    }
}
