using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// This is an example of the usage of Easy Spline Path 2D Plus.
/// In this example we use the function 'GetPointByDistance' to get the position a point in 
/// the curve corresponding to a distance messured along the curve starting from 
/// the first Node.
/// </summary>
public class FollowPathPlus : MonoBehaviour {
	// A link to the GameObject containing the EasySplinePath2DPlus script (provided by user in the editor)
	public EasySplinePath2DPlus spline2D;

	// The speed of the object in Units per second
	public float speed = 5;

	// Should the object align to the movement (the X axis is used as forward)
	public bool align = false;

	// Set the position to the curve position at 'dist' distance and calculate the next distance at current speed.
	protected float dist = 0;

	private void Update() {
		Transform trans = transform;
		Transform splineTrans = spline2D.transform;
		trans.position = spline2D.GetPointByDistance(dist, true);
		dist += speed * Time.deltaTime;
		if (align) {
			Vector3 dir = spline2D.GetPointByDistance(dist, true) - transform.position;
			float angle = Vector3.SignedAngle(splineTrans.right, dir, splineTrans.forward);
			trans.rotation = splineTrans.rotation * Quaternion.AngleAxis(angle, Vector3.forward);
		}
	}
}
