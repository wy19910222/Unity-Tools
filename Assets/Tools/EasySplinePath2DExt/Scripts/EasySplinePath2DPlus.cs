using System.Collections.Generic;
using UnityEngine;

public class EasySplinePath2DPlus : MonoBehaviour {
	[SerializeField, HideInInspector] public SplinePath2D path;
	[HideInInspector] public float displayScale = 1;
	[HideInInspector] public bool hideControlPoints;
	[HideInInspector] public bool alwaysVisible;
	[HideInInspector] public bool autoUpdate;
	[HideInInspector] public SplinePath2D.AnchorStatus defaultControlType = SplinePath2D.AnchorStatus.LOCK;
	[HideInInspector] public Color lineColor = new Color(0.84f, 0.96f, 1);
	[HideInInspector] public Color handleLineColor = new Color(0.16f, 0.5f, 1);
	[HideInInspector] public Color highlightColor = new Color(1, 0.8f, 0);

	// Distance between points in the evenly spaced calculated points on the curve (The lower this value the larger amount of points)
	[HideInInspector]
	public float spacing = 0.1f;

	[HideInInspector] public float resolution = 1f;

	// ReSharper disable once NotAccessedField.Global
	[HideInInspector] public bool started;
	[SerializeField, HideInInspector] public Vector2[] points;
	// ReSharper disable once NotAccessedField.Global
	[HideInInspector] public bool selected = true;
	[HideInInspector] public Vector2 offset;

	// ReSharper disable once FieldCanBeMadeReadOnly.Global
	protected List<float> distances = new List<float>();
	protected float length;
	protected bool shouldSetUp = true;

	[HideInInspector] public float anchorIconSize = 0.35f;
	[HideInInspector] public float controlIconSize = 0.12f;
	[HideInInspector] public float buttonIconSize = 0.19f;
	[HideInInspector] public float addIconSize = 0.2f;
	[HideInInspector] public float anchorDiameter = 0.17f;
	[HideInInspector] public float buttonDiameter = 0.09f;
	[HideInInspector] public float controlDiameter = 0.059f;
	[HideInInspector] public float snapSizeX = 1f;
	[HideInInspector] public float snapSizeY = 1f;

	/// <summary>
	/// Initialize the SplinePath2D
	/// </summary>
	public void CreatePath() {
		path = new SplinePath2D(new Vector2(), defaultControlType);
	}

	/// <summary>
	/// Reset the Spline and updates values.
	/// </summary>
	public void Reset() {
		CreatePath();
		SetUp();
	}

	/// <summary>
	/// Calculates the points of the curve and the total length.
	/// Use it whenever you change the Spline shape.
	/// </summary>
	public void SetUp() {
		points = path.GetEquidistancePoints(spacing, resolution);
		length = 0;
		distances.Clear();
		distances.Add(length);
		for (int i = 1; i < points.Length; i++) {
			length += Vector2.Distance(points[i], points[i - 1]);
			distances.Add(length);
		}
		started = true;
	}

	protected Vector2 FindClosest(int from, int to, float val) {
		while (from != to - 1) {
			int i = from + (to - from) / 2;
			if (val > distances[i]) {
				from = i;
			} else {
				to = i;
			}
		}
		float t = (val - distances[from]) / (distances[to] - distances[from]);
		return Vector2.Lerp(points[from], points[to], t);
	}

	/// <summary>
	/// Gets the point on the curve corresponding to distance 'dist' in the 
	/// specified space.
	/// </summary>
	/// <returns> The point on the curve corresponding to distance 'dist'.
	/// If loop is set to true it will start at the beginning when it gets to the 
	/// end.</returns>
	/// <param name="dist">Distance along the curve.</param>
	/// <param name="loop">If set to <c>true</c> loop.</param>
	/// <param name="space">If set to Space.Self it will return the local position.
	/// If set to Space.World ot will return the world position.</param>
	public Vector3 GetPointByDistance(float dist, bool loop = false, Space space = Space.World) {
		if (shouldSetUp) {
			shouldSetUp = false;
			SetUp();
		}
		float distAux = dist;
		if (dist > length) {
			if (loop) {
				distAux = ((dist / length) - (int) (dist / length)) * length;
			} else {
				distAux = length;
			}
		} else if (dist < 0) {
			if (loop) {
				distAux = ((dist / length) - (int) (dist / length) + 1) * length;
			} else {
				distAux = 0;
			}
		}
		Vector3 ret = FindClosest(0, points.Length - 1, distAux);
		if (space == Space.World) {
			ret = transform.TransformPoint(ret);
		}
		return ret;
	}

	/// <summary>
	/// Gets the point on the curve corresponding to percent 'percent' in the 
	/// specified space.
	/// </summary>
	/// <returns> The point on the curve corresponding to distance 'dist'.
	/// If loop is set to true it will start at the beginning when it gets to the 
	/// end.</returns>
	/// <param name="percent">Percent of the curve. Range from 0 to 1.</param>
	/// <param name="loop">If set to <c>true</c> loop.</param>
	/// <param name="space">If set to Space.Self it will return the local position
	/// if the point. If set to Space.World ot will return the world position.</param>
	public Vector3 GetPointByPercent(float percent, bool loop = false, Space space = Space.World) {
		if (shouldSetUp) {
			shouldSetUp = false;
			SetUp();
		}
		float dist = length * percent;
		return GetPointByDistance(dist, loop, space);
	}

	/// <summary>
	/// Given a percent it returns the corresponding 
	/// distance along the curve.
	/// </summary>
	/// <returns>Distance corresponding to the percent.</returns>
	/// <param name="percent">Percent of the curve (0, 1.0).</param>
	public float PercentToDistance(float percent) {
		return length * percent;
	}

	/// <summary>
	/// Given a distance, it returns the corresponding 
	/// percent of the curve.
	/// </summary>
	/// <returns>Percent of the curve corresponding to the distance.</returns>
	/// <param name="dist">Distance along the curve.</param>
	public float DistanceToPercent(float dist) {
		return dist / length;
	}

	public float GetLength() {
		return length;
	}

	/// <summary>
	/// Adds a new anchor point in the specified location and connects it to the 
	/// last anchor point in the Spline.
	/// </summary>
	/// <param name="anchorPos">Position of the new anchor point, in world space.</param>
	/// <param name="space">If set to Space.Self anchorPos is the local position.
	/// If set to Space.World anchorPos is the world position.</param>
	public void AddSegment(Vector3 anchorPos, Space space = Space.World) {
		path.AddSegment(space == Space.World ? transform.InverseTransformPoint(anchorPos) : anchorPos, defaultControlType);
		if (autoUpdate) {
			SetUp();
		}
	}

	/// <summary>
	/// Adds a new anchor point in the specified position and connects it to the 
	/// anchor points corresponding to the segment.
	/// </summary>
	/// <param name="anchorPos">Position of the new anchor point, in world space.</param>
	/// <param name="segmentIndex">Index of the segment to split.</param>
	/// <param name="space">If set to Space.Self anchorPos is the local position.
	/// If set to Space.World anchorPos is the world position.</param>
	public void SplitSegment(Vector3 anchorPos, int segmentIndex, Space space = Space.World) {
		path.SplitSegment(space == Space.World ? transform.InverseTransformPoint(anchorPos) : anchorPos, segmentIndex, defaultControlType);
		if (autoUpdate) {
			SetUp();
		}
	}

	/// <summary>
	/// Get the control points of the spline corresponding to the segment.
	/// </summary>
	/// <returns>An array containing the control points of the segment.</returns>
	/// <param name="segmentIndex">The index of the segment.</param>
	/// <param name="space">If set to Space.Self it will return the local position.
	/// If set to Space.World it will return the world position.</param>
	public Vector3[] GetPointsInSegment(int segmentIndex, Space space = Space.World) {
		List<Vector3> segmentPoints = new List<Vector3>();
		Vector2[] segmentPointsArray = path.GetPointsInSegment(segmentIndex);
		if (segmentPointsArray != null) {
			foreach (Vector2 p in segmentPointsArray) {
				segmentPoints.Add(space == Space.World ? transform.TransformPoint(p) : (Vector3) p);
			}
			return segmentPoints.ToArray();
		}
		return null;
	}

	/// <summary>
	/// Get the position of the specified control point.
	/// </summary>
	/// <returns>The position of the control point in the specified space</returns>
	/// <param name="pointIndex">The index.</param>
	/// <param name="space">If set to Space.Self it will return the local position.
	/// If set to Space.World it will return the world position.</param>
	public Vector3 GetPoint(int pointIndex, Space space = Space.World) {
		if (space == Space.World) {
			return transform.TransformPoint(path[pointIndex]);
		}
		return path[pointIndex];
	}

	/// <summary>
	/// Move the specified control point to the a position. If force is set to 
	/// false all affected points will be updated.
	/// </summary>
	/// <param name="pointIndex">The index of the control point to be moved.</param>
	/// <param name="position">New position of the point.</param>
	/// <param name="force">If set to <c>true</c> other points will not be affected.</param>
	/// <param name="space">If set to Space.Self position is the local position.
	/// If set to Space.World position is the world position.</param>
	public void MovePoint(int pointIndex, Vector3 position, bool force = false, Space space = Space.World) {
		path.MovePoint(pointIndex, space == Space.World ? transform.InverseTransformPoint(position) : position, force);
		if (autoUpdate) {
			SetUp();
		}
	}

	/// <summary>
	/// Moves the handles corresponding to the specified segment along the vector 
	/// 'movement', the origin vector is used to calculate how much each vector moves.
	/// </summary>
	/// <param name="segmentIndex">Index of tne segment.</param>
	/// <param name="movement">Movement vector to be applied.</param>
	/// <param name="origin">Position from where the segment is being dragged.</param>
	/// <param name="space">If set to Space.Self, movement and origin is the local position.
	/// If set to Space.World, movement and origin is the world position.</param>
	public void MoveSegment(int segmentIndex, Vector3 movement, Vector3 origin, Space space = Space.World) {
		if (space == Space.World) {
			movement = transform.InverseTransformPoint(movement);
			origin = transform.InverseTransformPoint(origin);
		}
		path.MoveSegment(segmentIndex, movement, origin);
		if (autoUpdate) {
			SetUp();
		}
	}

	/// <summary>
	/// Used to predict the shape of the curve if a node where added in the specified position.
	/// </summary>
	/// <returns>The anchor control points positions.</returns>
	/// <param name="previewPos">Preview position.</param>
	/// <param name="segmentIndex">Index of the segment to test.</param>
	/// <param name="space">If set to Space.Self previewPos is the local position.
	/// If set to Space.World previewPos is the world position.</param>
	public Vector3[] CalculateAnchorControlPointsPositions(Vector3 previewPos, int segmentIndex, Space space = Space.World) {
		if (space == Space.World) {
			previewPos = transform.InverseTransformPoint(previewPos);
		}
		Vector2[] p2s = path.CalculateAnchorControlPointsPositions(previewPos, segmentIndex);
		int count = p2s.Length;
		Vector3[] p3s = new Vector3[count];
		for (int i = 0; i < count; ++i) {
			p3s[i] = p2s[i];
		}
		return p3s;
	}

	/// <summary>
	/// Gets the position of all control points in the given space. By default the space is Space.World
	/// </summary>
	/// <returns>An array containing the positions of all control points, in the requested space.</returns>
	/// <param name="space">Space of the position vector (local or world).</param>
	public Vector3[] GetControlPoints(Space space = Space.World) {
		List<Vector3> controlPoints = new List<Vector3>();
		for (int i = 0; i < path.PointCount; i++) {
			Vector3 pos = GetPoint(i, space);
			controlPoints.Add(pos);
		}
		return controlPoints.ToArray();
	}

	/// <summary>
	/// Get the position of all anchor points in the given space. By default the space is Space.World
	/// </summary>
	/// <param name="space">The space (local or world) in witch point positions will be returned</param>
	/// <returns>An array of Vector3 containing the positions of all Anchor Control Points, in the required space.</returns>
	public Vector3[] GetAnchorControlPoints(Space space = Space.World) {
		List<Vector3> controlPoints = new List<Vector3>();
		for (int i = 0; i < path.PointCount; i += 3) {
			Vector3 pos = GetPoint(i, space);
			controlPoints.Add(pos);
		}
		return controlPoints.ToArray();
	}

	/// <summary>
	/// Change the type of a given anchor (AUTO, CORNER, FREE or LOCK). If 'autoUpdate' or if the param 'setUp' is set to true it will call 'SetUp()'.
	/// </summary>
	/// <param name="index">The index of the anchor as given by the function 'GetAnchorControlPoints'.</param>
	/// <param name="type">The new type of the Anchor.</param>
	/// <param name="setUp">Whether to call SetUp afterwards.</param>
	public void ChangeAnchorType(int index, SplinePath2D.AnchorStatus type, bool setUp = false) {
		path.SetAnchorStatus(index * 3, type);
		if (autoUpdate || setUp) {
			SetUp();
		}
	}

	/// <summary>
	/// Get the given Anchor's type.
	/// </summary>
	/// <param name="index">The index of the anchor as given by the function 'GetAnchorControlPoints'</param>
	/// <returns>The type of the anchor (AUTO, CORNER, FREE or LOCK).</returns>
	public SplinePath2D.AnchorStatus GetAnchorType(int index) {
		return path.GetAnchorStatus(index * 3);
	}

	/// <summary>
	/// Calculates and returns the closest spline point to the give world point. (On)
	/// </summary>
	/// <param name="point">A world point to compare to.</param>
	/// <param name="space">If set to Space.Self point is the local position and it will return the local position.
	/// If set to Space.World point is the world position and it will return the world position.</param>
	/// <returns>The closest point of the spline to the given point.</returns>
	public Vector3 GetClosestSplinePoint(Vector3 point, Space space = Space.World) {
		int min = 0;
		if (points.Length == 0) {
			SetUp();
		}
		float minDist = Vector3.Distance(transform.TransformPoint(points[0]), point);
		for (int i = 1; i < points.Length; i++) {
			float dist = Vector3.Distance(transform.TransformPoint(points[i]), point);
			if (dist < minDist) {
				minDist = dist;
				min = i;
			}
		}
		return transform.TransformPoint(points[min]);
	}
}
