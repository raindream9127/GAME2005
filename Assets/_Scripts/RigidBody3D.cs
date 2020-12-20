using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BodyType
{
    STATIC,
    DYNAMIC,
    KINETIC
}

[System.Serializable]
public class RigidBody3D : MonoBehaviour
{
    [Header("Gravity Simulation")]
    public float mass;
    public BodyType bodyType;
    //public bool isFalling;

    [Header("Attributes")]
    public Vector3 velocity;
    public Vector3 acceleration;
    public bool isFalling;
    public float gravity = 5.0f;

    // Start is called before the first frame update
    void Start()
    {
        velocity = acceleration = Vector3.zero;        
    }

    // Update is called once per frame
    public void RigindBodyUpdate()
    {
        if (bodyType == BodyType.DYNAMIC || bodyType == BodyType.KINETIC)
        {
            if (isFalling)
            {
                velocity += new Vector3(0.0f, -gravity, 0.0f) * Time.deltaTime;
            }
            velocity += acceleration * Time.deltaTime;
            if (velocity.sqrMagnitude > 0.0005f)
            {
                transform.position += velocity;
            }
            //transform.position += velocity;
        }
    }
}
