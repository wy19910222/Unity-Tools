using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is an example of the usage of Easy Spline Path 2D.
/// In this example we use the function 'GetPointByDistance' to get the position a point in 
/// the curve corresponding to a distance messured along the curve starting from 
/// the first Node.
/// 
/// </summary>
public class FollowPath : MonoBehaviour
{
    // A link to the GameObject containing the EasySlinePath2D script (provided by user in the editor)
    public EasySplinePath2D spline2D;
    // The speed of the object in Units per second
    public float speed = 5;
    // Should the object align to the movement (the X axis is used as forward)
    public bool align = false;
    // Set the position to the curve position at 'dist' distance and calculate the next distance at current speed.
    protected float dist = 0;

    virtual protected void Start()
    {
        StartCoroutine(MoveForwardRoutine());
    }

    // Coroutine to move the card each tick
    IEnumerator MoveForwardRoutine()
    {
        dist = 0;
        while (true)
        {
            Move();
            // Rotate the object towards the movement direction (current position to next position)
            if (align)
            {
                Vector2 dir = (spline2D.GetPointByDistance(dist, true) - (Vector2)transform.position).normalized;
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                RotateToAlign(angle);

            }
            yield return null;
        }
    }

    virtual protected void RotateToAlign(float angle)
    {
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
    }
    // Function to rotate the object to face the direction of the movement
    virtual protected void Move()
    {
        transform.position = spline2D.GetPointByDistance(dist, true);
        dist += speed * Time.deltaTime;
    }

}
