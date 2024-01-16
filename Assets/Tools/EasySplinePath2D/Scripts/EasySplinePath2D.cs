using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SplinePrinter))]
public class EasySplinePath2D : MonoBehaviour
{

    [SerializeField, HideInInspector]
    public SplinePath2D path;
    [HideInInspector]
    public float displayScale = 1;
    [HideInInspector]
	public bool hideControlPoints = false;
    [HideInInspector]
    public bool allwaysVisible = false;
    [HideInInspector]
    public bool autoUpdate = false;
    [HideInInspector]
	public SplinePath2D.AnchorStatus defaultControlType = SplinePath2D.AnchorStatus.LOCK;
    [HideInInspector]
    public Color lineColor = new Color(0.84f, 0.96f, 1);
    [HideInInspector]
    public Color handleLineColor = new Color(0.16f, 0.5f, 1);
    [HideInInspector]
    public Color highlightColor = new Color(1, 0.8f, 0);
    [HideInInspector]
    public float spacing = 0.1f;   // Distance between points in the evenly spaced calculated points on the curve (The lower this value the larger amount of points)
    [HideInInspector]
	public float resolution = 1f;

    [HideInInspector]
    public bool started = false;
    [SerializeField, HideInInspector]
    public Vector2[] points;
    [HideInInspector]
    public bool selected = true;
	[HideInInspector]
	public Vector2 offset;

    protected List<float> distances = new List<float>();
    protected float lenght = 0;
    protected bool shouldSetUp = true;

    [HideInInspector]
    public float anchorIconSize = 0.35f;
    [HideInInspector]
    public float controlIconSize = 0.12f;
    [HideInInspector]
    public float buttonIconSize = 0.19f;
    [HideInInspector]
    public float addIconSize = 0.2f;
    [HideInInspector]
    public float anchorDiameter = 0.17f;
    [HideInInspector]
    public float buttonDiameter = 0.09f;
    [HideInInspector]
    public float controlDiameter = 0.059f;
    [HideInInspector]
    public Vector3 lastScale = new Vector3(1, 1, 1);
    [HideInInspector]
    public Vector3 lastRotation = new Vector3(0, 0, 0);
    [HideInInspector]
    public bool saveOnPlay = false;
    [HideInInspector]
    public float snapSizeX = 1f;
    [HideInInspector]
    public float snapSizeY = 1f;
    [HideInInspector]
    public bool hideTool = false;

    /// <summary>
    /// Initialize the SplinePath2D
    /// </summary>
    public void CreatePath()
    {
        path = new SplinePath2D(new Vector2(), defaultControlType);
    }
    /// <summary>
    /// Reset the Spline and updates values.
    /// </summary>
    public void Reset(bool relative = false)
    {
        CreatePath();
        SetUp();
    }

    /// <summary>
    /// Calculates the points of the curve and the total lenght.
    /// Use it whenever you change the Spline shape.
    /// </summary>
    public void SetUp()
    {
        points = path.GetEquidistancePoints(spacing, resolution);
        lenght = 0;
        distances.Clear();
        distances.Add(lenght);
        for (int i = 1; i < points.Length; i++)
        {
            lenght += Vector2.Distance(points[i], points[i - 1]);
            distances.Add(lenght);
        }
        started = true;
    }

	private Vector2 FindClosest(int from, int to, float val)
    {
        if (from == to - 1)
        {
            float t = (val - distances[from]) / (distances[to] - distances[from]);
            return Vector2.Lerp(points[from], points[to], t);
        }
        else
        {
            int i = from + (to - from) / 2;
            if (val > distances[i])
            {
                return FindClosest(i, to, val);
            }
            else
            {
                return FindClosest(from, i, val);
            }
        }
    }

    /// <summary>
    /// Gets the point on the curve corresponding to distance 'dist' in the 
    /// specified space.
    /// </summary>
    /// <returns> The point on the curve corresponding to distance 'dist'.
    /// If loop is set to true it will start at the begining when it gets to the 
    /// end.</returns>
    /// <param name="dist">Distance along the curve.</param>
    /// <param name="loop">If set to <c>true</c> loop.</param>
    /// <param name="space">If set to Space.Self it will return the local position
    /// if the point. If set to Space.World ot will return the world position.</param>
    public Vector2 GetPointByDistance(float dist, bool loop = false, Space space = Space.World)
    {
        if (shouldSetUp)
        {
            shouldSetUp = false;
			SetUp();
        }

        float distAux = dist;
        if (dist > lenght)
        {
            if (loop)
            {
                distAux = ((dist / lenght) - (int)(dist / lenght)) * lenght;
            }
            else
            {
                distAux = lenght;
            }
        }

        Vector2 ret = FindClosest(0, points.Length - 1, distAux);

        if (space == Space.World)
            ret += (Vector2)transform.position;
        
        return ret;
    }

    /// <summary>
    /// Gets the point on the curve corresponding to percent 'percent' in the 
    /// specified space.
    /// </summary>
    /// <returns> The point on the curve corresponding to distance 'dist'.
    /// If loop is set to true it will start at the begining when it gets to the 
    /// end.</returns>
    /// <param name="percent">Percent of the curve. Range from 0 to 1.</param>
    /// <param name="loop">If set to <c>true</c> loop.</param>
    /// <param name="space">If set to Space.Self it will return the local position
    /// if the point. If set to Space.World ot will return the world position.</param>
    public Vector2 GetPointByPercent(float percent, bool loop = false, Space space = Space.World)
    {
        if (shouldSetUp)
        {
            shouldSetUp = false;
            SetUp();
        }

        if (percent > 1)
        {
            percent -= (int)percent;
        }
        float dist = lenght * percent;

        return GetPointByDistance(dist, loop, space);
    }
    /// <summary>
    /// Given a percent it returns the corresponding 
    /// distance along the curve.
    /// </summary>
    /// <returns>Distance corresponding to the percent.</returns>
    /// <param name="percent">Percent of the curve (0, 1.0).</param>
    public float PercentToDistance(float percent)
    {
        return lenght * percent;
    }
    /// <summary>
    /// Given a distance, it returns the corresponding 
    /// percent of the curve.
    /// </summary>
    /// <returns>Percent of the curve corresponding to the distance.</returns>
    /// <param name="dist">Distance along the curve.</param>
    public float DistanceToPercent(float dist)
    {
        return dist / lenght;
    }

    public float GetLenght()
    {
        return lenght;
    }
    /// <summary>
    /// Adds a new anchor point in the specified location and connects it to the 
    /// last anchor point in the Spline.
    /// </summary>
    /// <param name="anchorPos">Position of the new anchor point, in world space.</param>
    public void AddSegment(Vector2 anchorPos)
    {
        path.AddSegment(anchorPos - (Vector2)transform.position, defaultControlType);
        if (autoUpdate)
            SetUp();
    }

    /// <summary>
    /// Adds a new anchor point in the specified position and connects it to the 
    /// anchor points corresponding to the segment.
    /// </summary>
    /// <param name="anchorPos">Position of the new anchor point, in world space.</param>
    /// <param name="segmentIndex">Index of the segment to split.</param>
    public void SplitSegment(Vector2 anchorPos, int segmentIndex)
    {
        path.SplitSegment(anchorPos - (Vector2)transform.position, segmentIndex, defaultControlType);
        if (autoUpdate)
            SetUp();
    }
    /// <summary>
    /// Get the control points of the spline corresponding to the segment.
    /// </summary>
    /// <returns>An array containing the control points of the segment.</returns>
    /// <param name="segmentIndex">The index of the segment.</param>
    public Vector2[] GetPointsInSegment(int segmentIndex)
    {
        List<Vector2> segmentPoints = new List<Vector2>();
        Vector2[] segmentPointsArray = path.GetPointsInSegment(segmentIndex);
        if (segmentPointsArray != null)
        {
			foreach (Vector2 p in segmentPointsArray)
			{
				segmentPoints.Add(p + (Vector2)transform.position);
			}
			return segmentPoints.ToArray();
        } else {
            return null;
        }

    }
    /// <summary>
    /// Get the position of the specified control point.
    /// </summary>
    /// <returns>The position of the control point in the specified space</returns>
    /// <param name="pointIndex">The index.</param>
    /// <param name="space">The index.</param>
    public Vector2 GetPoint(int pointIndex, Space space = Space.World)
    {
        Vector2 pos = path[pointIndex];
        if (space == Space.World)
            pos += (Vector2)transform.position;
        return pos;
    }
    /// <summary>
    /// Move the specified control point to the a position. If force is set to 
    /// false all affected points will be updated.
    /// </summary>
    /// <param name="pointIndex">The index of the control point to be moved.</param>
    /// <param name="position">New position of the point.</param>
    /// <param name="force">If set to <c>true</c> other points will not be affected.</param>
    public void MovePoint(int pointIndex, Vector2 position, bool force = false)
    {
        path.MovePoint(pointIndex, position - (Vector2)transform.position, force);
        if (autoUpdate)
            SetUp();
    }
    /// <summary>
    /// Moves the handles corresponding to the specified segment along the vector 
    /// 'movement', the origin vector is used to calculate how much each vector moves.
    /// </summary>
    /// <param name="segmentIndex">Index of tne segment.</param>
    /// <param name="movement">Movement vector to be applied.</param>
    /// <param name="origin">Position from where the segment is being dragged.</param>
    public void MoveSegment(int segmentIndex, Vector2 movement, Vector2 origin)
    {
        path.MoveSegment(segmentIndex, movement, origin - (Vector2)transform.position);
        if (autoUpdate)
            SetUp();
    }
    /// <summary>
    /// Used to predict the shape of the curve if a node where added in the specified position.
    /// </summary>
    /// <returns>The anchor control points positions.</returns>
    /// <param name="previewPos">Preview position.</param>
    /// <param name="segmentIndex">Index of the segment to test.</param>
    public Vector2[] CalculateAnchorControlPointsPositions(Vector2 previewPos, int segmentIndex)
    {
        return path.CalculateAnchorControlPointsPositions(previewPos - (Vector2)transform.position, segmentIndex);
    }
    /// <summary>
    /// Gets the position of all control points in the given space. By default the space is Space.World
    /// </summary>
    /// <returns>An array containing the positions of all control points, in the requested space.</returns>
    /// <param name="space">Space of the position vector (local or world).</param>
    public Vector2[] GetControlPoints(Space space = Space.World)
    {
        List<Vector2> controlPoints = new List<Vector2>();
        for (int i = 0; i < path.PointCount; i++)
        {
            Vector2 pos = GetPoint(i, space);
            controlPoints.Add(pos);
        }

        return controlPoints.ToArray();
    }

    /// <summary>
    /// Get the position of all anchor points in the given space. By default the space is Space.World
    /// </summary>
    /// <param name="space">The space (local or world) in wich point positions will be returned</param>
    /// <returns>An array of Vector2 containing the positions of all Anchor Control Points, in the required space.</returns>
    public Vector2[] GetAnchorControlPoints(Space space = Space.World)
    {
        List<Vector2> controlPoints = new List<Vector2>();
        for (int i = 0; i < path.PointCount; i += 3)
        {
            Vector2 pos = GetPoint(i, space);
            controlPoints.Add(pos);
        }

        return controlPoints.ToArray();
    }

    /// <summary>
    /// Change the type of a given anchor (AUTO, CORNER, FREE or LOCK). If 'autoUpdate' or if the param 'setUp' is set to true it will call 'SetUp()'.
    /// </summary>
    /// <param name="indx">The index of the anchor as given by the function 'GetAnchorControlPoints'.</param>
    /// <param name="type">The new type of the Anchor.</param>
    /// <param name="setUp">Whether to call SetUp afterwards.</param>
    public void ChangeAnchorType(int indx, SplinePath2D.AnchorStatus type, bool setUp = false)
    {
        path.SetAnchorStatus(indx * 3, type);

        if (autoUpdate || setUp)
            SetUp();
    }

    /// <summary>
    /// Get the given Anchor's type.
    /// </summary>
    /// <param name="indx">The index of the anchor as given by the function 'GetAnchorControlPoints'</param>
    /// <returns>The type of the anchor (AUTO, CORNER, FREE or LOCK).</returns>
    public SplinePath2D.AnchorStatus GetAnchorType(int indx)
    {
        return path.GetAnchorStatus(indx * 3);
    }

    /// <summary>
    /// Calculates and returns the closest spline point to the givel world point. (On)
    /// </summary>
    /// <param name="point">A world point to compare to.</param>
    /// <returns>The closest point of the spline to the given point.</returns>
    public Vector3 GetClosestSplinePoint(Vector2 point)
    {
        int min = 0;
        if (points.Length == 0)
            SetUp();

        float minDist = Vector2.Distance(points[0] + (Vector2)transform.position, point);
        float distAux;

        for (int i = 1; i < points.Length; i++)
        {
            distAux = Vector2.Distance(points[i] + (Vector2)transform.position, point);

            if (distAux < minDist)
            {
                minDist = distAux;
                min = i;
            }
        }

        return (Vector3)points[min] + transform.position;
    }

}
