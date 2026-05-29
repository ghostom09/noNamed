using System;
using UnityEngine;

public class CameraTest : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSmooth = 1f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);

    private void LateUpdate()
    {
        if (target == null) return;
        
        transform.position = Vector3.Lerp(transform.position, 
            target.position + offset, 
            moveSmooth);
    }
}
