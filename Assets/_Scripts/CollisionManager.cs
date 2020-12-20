using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CollisionManager : MonoBehaviour
{
    public CubeBehaviour[] cubes;
    public BulletBehaviour[] spheres;
    [HideInInspector]
    public RigidBody3D[] rigidbodies;

    private static Vector3[] faces;

    // Start is called before the first frame update
    void Start()
    {
        cubes = FindObjectsOfType<CubeBehaviour>();
        rigidbodies = FindObjectsOfType<RigidBody3D>();

        faces = new Vector3[]
        {
            Vector3.left, Vector3.right,
            Vector3.down, Vector3.up,
            Vector3.back , Vector3.forward
        };
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        foreach (var rigidbody in rigidbodies)
        {
            rigidbody.RigindBodyUpdate();
        }
        foreach (var cube in cubes)
        {
            cube.CubeBehaviourUpdate();
        }

        spheres = FindObjectsOfType<BulletBehaviour>();

        for (int i = 0; i < cubes.Length; ++i)
        {
            // let everything fall initially and use collision to support them up
            if (cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC ||
                cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.KINETIC)
            {
                cubes[i].gameObject.GetComponent<RigidBody3D>().isFalling = true;
                cubes[i].isGrounded = false;
            }
        }

        // check each AABB with every other AABB in the scene
        for (int i = 0; i < cubes.Length; i++)
        {
            for (int j = i + 1; j < cubes.Length; j++)
            {
                if (i != j)
                {
                    if (CheckAABBs(cubes[i], cubes[j]))
                    {
                        if (cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.KINETIC && 
                            cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC)
                        {
                            PlayerVsDynamic(cubes[j], cubes[i]);
                        }
                        else if (cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.KINETIC &&
                            cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC)
                        {
                            PlayerVsDynamic(cubes[i], cubes[j]);
                        }
                        else if (cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC &&
                            cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.STATIC)
                        {
                            DynamicVsStatic(cubes[i], cubes[j]);
                        }
                        else if (cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.STATIC &&
                            cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.DYNAMIC)
                        {
                            DynamicVsStatic(cubes[j], cubes[i]);
                        }
                        else if (cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.KINETIC &&
                            cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.STATIC)
                        {
                            PlayerVsStatic(cubes[j], cubes[i]);
                        }
                        else if (cubes[j].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.KINETIC &&
                            cubes[i].gameObject.GetComponent<RigidBody3D>().bodyType == BodyType.STATIC)
                        {
                            PlayerVsStatic(cubes[i], cubes[j]);
                        }
                    }
                }
            }
        }

        // Check each sphere against each AABB in the scene
        foreach (var sphere in spheres)
        {
            foreach (var cube in cubes)
            {
                if (cube.name != "Player")
                {
                    CheckSphereAABB(sphere, cube);
                }                
            }
        }
    }

    private void PlayerVsDynamic(CubeBehaviour dynamicObject, CubeBehaviour player)
    {
        Vector3 directionPlayerToDynamic = (dynamicObject.transform.position - player.transform.position).normalized;
        Contact contactDynamicToPlayer = player.contacts.Find(x => x.cube.gameObject.name == dynamicObject.gameObject.name);

        dynamicObject.transform.position += directionPlayerToDynamic * 
            (contactDynamicToPlayer.penetration / Vector3.Dot(contactDynamicToPlayer.face, directionPlayerToDynamic));
    }

    private void PlayerVsStatic(CubeBehaviour staticObject, CubeBehaviour player)
    {        
        Contact contactPlayerToStatic = staticObject.contacts.Find(x => x.cube.gameObject.name == player.gameObject.name);

        player.transform.position += (contactPlayerToStatic.penetration + 0.02f) * (contactPlayerToStatic.face);
        for (int i = 0; i < 3; ++i)
        {
            if (contactPlayerToStatic.face[i] != 0)
            {
                player.GetComponent<RigidBody3D>().velocity[i] = 0;
            }
        }
    }

    private void DynamicVsStatic(CubeBehaviour dynamicObject, CubeBehaviour staticObject)
    {
        Vector3 directionStaticToDynamic = (dynamicObject.transform.position - staticObject.transform.position).normalized;
        Contact contactDynamicToStatic = staticObject.contacts.Find(x => x.cube.gameObject.name == dynamicObject.gameObject.name);

        dynamicObject.transform.position += directionStaticToDynamic *
            (contactDynamicToStatic.penetration / Vector3.Dot(contactDynamicToStatic.face, directionStaticToDynamic));
        dynamicObject.GetComponent<RigidBody3D>().velocity *= -1;
    }

    public static void CheckSphereAABB(BulletBehaviour s, CubeBehaviour b)
    {
        // get box closest point to sphere center by clamping
        var x = Mathf.Max(b.min.x, Mathf.Min(s.transform.position.x, b.max.x));
        var y = Mathf.Max(b.min.y, Mathf.Min(s.transform.position.y, b.max.y));
        var z = Mathf.Max(b.min.z, Mathf.Min(s.transform.position.z, b.max.z));

        var distance = Math.Sqrt((x - s.transform.position.x) * (x - s.transform.position.x) +
                                 (y - s.transform.position.y) * (y - s.transform.position.y) +
                                 (z - s.transform.position.z) * (z - s.transform.position.z));

        if ((distance < s.radius) && (!s.isColliding))
        {
            // determine the distances between the contact extents
            float[] distances = {
                (b.max.x - s.transform.position.x),
                (s.transform.position.x - b.min.x),
                (b.max.y - s.transform.position.y),
                (s.transform.position.y - b.min.y),
                (b.max.z - s.transform.position.z),
                (s.transform.position.z - b.min.z)
            };

            float penetration = float.MaxValue;
            Vector3 face = Vector3.zero;

            // check each face to see if it is the one that connected
            for (int i = 0; i < 6; i++)
            {
                if (distances[i] < penetration)
                {
                    // determine the penetration distance
                    penetration = distances[i];
                    face = faces[i];
                }
            }

            s.penetration = penetration;
            s.collisionNormal = face;
            //s.isColliding = true;

            
            Reflect(s);
        }

    }
    
    // This helper function reflects the bullet when it hits an AABB face
    private static void Reflect(BulletBehaviour s)
    {
        if ((s.collisionNormal == Vector3.forward) || (s.collisionNormal == Vector3.back))
        {
            s.direction = new Vector3(s.direction.x, s.direction.y, -s.direction.z);
        }
        else if ((s.collisionNormal == Vector3.right) || (s.collisionNormal == Vector3.left))
        {
            s.direction = new Vector3(-s.direction.x, s.direction.y, s.direction.z);
        }
        else if ((s.collisionNormal == Vector3.up) || (s.collisionNormal == Vector3.down))
        {
            s.direction = new Vector3(s.direction.x, -s.direction.y, s.direction.z);
        }
    }

    public static bool CheckAABBs(CubeBehaviour a, CubeBehaviour b)
    {
        Contact bInListOfA = a.contacts.Find(x => x.cube.gameObject.name == b.gameObject.name);
        Contact aInListOfB = b.contacts.Find(x => x.cube.gameObject.name == a.gameObject.name);        

        if ((a.min.x <= b.max.x && a.max.x >= b.min.x) &&
            (a.min.y <= b.max.y && a.max.y >= b.min.y) &&
            (a.min.z <= b.max.z && a.max.z >= b.min.z))
        {
            bool result = false;

            // determine the distances between the contact extents
            float[] distances = {
                (b.max.x - a.min.x),
                (a.max.x - b.min.x),
                (b.max.y - a.min.y),
                (a.max.y - b.min.y),
                (b.max.z - a.min.z),
                (a.max.z - b.min.z)
            };

            float penetration = float.MaxValue;
            Vector3 face = Vector3.zero;

            // check each face to see if it is the one that connected
            for (int i = 0; i < 6; i++)
            {
                if (distances[i] < penetration)
                {
                    // determine the penetration distance
                    penetration = distances[i];
                    face = faces[i];
                }
            }

            // check if contact does not exist
            if (bInListOfA == null)
            {
                // remove any contact that matches the name but not other parameters
                //for (int i = a.contacts.Count - 1; i > -1; i--)
                //{
                //    if (a.contacts[i].cube.name.Equals(contactB.cube.name))
                //    {
                //        a.contacts.RemoveAt(i);
                //    }
                //}
                bInListOfA = new Contact(b);
                aInListOfB = new Contact(a);

                // set the contact properties
                bInListOfA.face = face;
                aInListOfB.face = -face;
                bInListOfA.penetration = aInListOfB.penetration = penetration;

                // add the new contact
                a.contacts.Add(bInListOfA);
                a.isColliding = true;
                b.contacts.Add(aInListOfB);
                b.isColliding = true;

                result = true;
            }
            else
            {
                // set the contact properties
                bInListOfA.face = face;
                aInListOfB.face = -face;
                bInListOfA.penetration = aInListOfB.penetration = penetration;

                result = false;
            }

            if (bInListOfA.face == Vector3.down)
            {
                a.gameObject.GetComponent<RigidBody3D>().isFalling = false;
                a.isGrounded = true;
            }
            if (aInListOfB.face == Vector3.down)
            {
                b.gameObject.GetComponent<RigidBody3D>().isFalling = false;
                b.isGrounded = true;
            }
            return result;
        }
        else
        {
            if (bInListOfA != null)
            {
                a.contacts.Remove(bInListOfA);
                a.isColliding = false;
                b.contacts.Remove(aInListOfB);
                b.isColliding = false;                
            }
            return false;
        }
    }
}
