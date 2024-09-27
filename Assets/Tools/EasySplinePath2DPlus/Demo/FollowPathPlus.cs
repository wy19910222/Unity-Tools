using UnityEngine;

/// <summary>
/// This is an example of the usage of Easy Spline Path 2D Plus.
/// In this example we use the function 'GetPointByDistance' to get the position a point in 
/// the curve corresponding to a distance measured along the curve starting from the first Node.
/// </summary>
public class FollowPathPlus : MonoBehaviour {
	// A link to the GameObject containing the EasySplinePath2DPlus script (provided by user in the editor)
	public EasySplinePath2DPlus spline2D;

	// The speed of the object in Units per second
	public float speed = 5;

	// Should the object align to the movement (the X axis is used as forward)
	public bool align = false;

	// Set the position to the curve position at 'dist' distance and calculate the next distance at current speed.
	public float dist = 0;

	private void Update() {
		transform.position = spline2D.GetPointByDistance(dist, true);
		dist += speed * Time.deltaTime;
		if (align) {
			transform.LookAt(spline2D.GetPointByDistance(dist, true), -spline2D.transform.forward);
		}
	}

#if UNITY_EDITOR
	[Header("Editor")]
	public bool moveOnValidate;
	public bool alignOnValidate;

	private void OnValidate() {
		if (spline2D) {
			spline2D.SetUp();
			dist = Mathf.Repeat(dist, spline2D.GetLength());
			if (moveOnValidate) {
				Move();
			}
			if (alignOnValidate) {
				Align();
			}
		}
	}
	
	[ContextMenu("Move")]
	private void Move() {
		transform.position = spline2D.GetPointByDistance(dist, true);
	}
	
	[ContextMenu("Align")]
	private void Align() {
		float dist1, dist2;
		float length = spline2D.GetLength();
		if (dist < length) {
			dist1 = dist;
			dist2 = Mathf.Min(dist + 0.00001F, length);
		} else {
			dist1 = Mathf.Max(length - 0.00001F, 0);
			dist2 = length;
		}
		Vector3 pos1 = spline2D.GetPointByDistance(dist1, true);
		Vector3 pos2 = spline2D.GetPointByDistance(dist2, true);
		transform.LookAt(transform.position + (pos2 - pos1), -spline2D.transform.forward);
	}
#endif
}
