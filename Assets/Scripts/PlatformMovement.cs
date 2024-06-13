using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlatformMovement : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 30.0f;
    private Vector3 rotationCenter;
    
    void Start()
    {
        rotationCenter = transform.position + 10 * Vector3.back;
    }


    void Update()
    {
        transform.RotateAround(rotationCenter, Vector3.up, Time.deltaTime * rotationSpeed);
    }
}
