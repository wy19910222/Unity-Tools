using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// This is an example of the usage of Easy Spline Path 2D.
/// In this example we use the function 'AddSegment' to get dynamically add
/// nodes to the spline.
/// </summary>
public class DynamicPath : FollowPath
{
    private Vector3 stageDimensions;
    private float lenght;

    protected float offset = 1;
	
    protected override void Start()
	{
        // Get the stage dimensions so we allways choose a point inside the screen
        stageDimensions = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0));
        AddPoints();
        base.Start();
	}

	protected override void Move()
	{
		base.Move();
        GeneratePath();
	}

    virtual protected void GeneratePath()
    {
        // When approaching the last spline lenght we spawn more nodes
        if (dist >= lenght - offset)
        {
            AddPoints();
        }
    }
    // Function to dynamically add nodes to the SplinePath2D
    private void AddPoints()
    {
        // Get the current lenght of the spline to check in the Move() method
        lenght = spline2D.GetLenght();
        // We add two nodes because the auto node type adapts any adjacent nodes of type Auto when created
        AddPoint();
        AddPoint();
        // When we modify the spline we need to call this function to update the values in the EasySplinePath2D script
        spline2D.SetUp();
    }

    virtual protected void AddPoint()
    {
        spline2D.AddSegment(new Vector2(Random.Range(-stageDimensions.x, stageDimensions.x), Random.Range(-stageDimensions.y, stageDimensions.y)));
    }
    // Override the rotate funtion to add smoothness to the movement
	protected override void RotateToAlign(float angle)
	{
        transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.AngleAxis(angle, Vector3.forward), Time.deltaTime * 3);
	}

}
