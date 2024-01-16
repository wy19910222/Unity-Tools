using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class for internal use of the script. It stores the model of the spline, 
/// and performs necesary calculations
/// </summary>

[System.Serializable]
public class SplinePath2D
{
    public enum AnchorStatus
    {
        AUTO,
        LOCK,
        FREE,
        CORNER,
    }

    [SerializeField, HideInInspector]
    public List<Vector2> points;
    [SerializeField, HideInInspector]
    bool closed = false;
    [SerializeField, HideInInspector]
    bool setControlPointsAutomatically;
    [SerializeField]
    public Vector2 offset = new Vector2();
    [SerializeField, HideInInspector]
    List<AnchorStatus> lockStatus = new List<AnchorStatus>();

    public SplinePath2D(Vector2 centre, AnchorStatus status)
    {
        if (status == AnchorStatus.CORNER)
        {
            points = new List<Vector2>
            {
                centre + new Vector2(-1, 0),
                centre + new Vector2(-1, 0),
                centre + new Vector2(1, 0),
                centre + new Vector2(1, 0)
            };
        } else {
			points = new List<Vector2>
			{
				centre + new Vector2(-1, 0),
				centre + new Vector2(-1, 1) * 0.5f,
				centre + new Vector2(-1, -1) * 0.5f,
				centre + new Vector2(1, 0)
			};
        }
        lockStatus = new List<AnchorStatus> { status, status };
    }

    public Vector2 this[int i]
    {
        get
        {
            return points[i];
        }
    }

    public bool IsClosed
    {
        get
        {
            return closed;
        }
        set
        {
            if (closed != value)
            {
                closed = value;

                if (closed)
                {
                    points.Add(points[points.Count - 1] * 2 - points[points.Count - 2]);
                    points.Add(points[0] * 2 - points[1]);
                    if (setControlPointsAutomatically)
                    {
                        AutoSetAnchorControlPoints(0);
                        AutoSetAnchorControlPoints(points.Count - 3);
                    }
                    ChainAutoSet(0);
                    ChainAutoSet(points.Count - 3);
                }
                else
                {
                    points.RemoveRange(points.Count - 2, 2);
                    if (setControlPointsAutomatically)
                    {
                        AutoSetStartAndEndControls();
                    }
                }
            }
        }
    }

    public bool AutoSetControlPoints
    {
        get
        {
            return setControlPointsAutomatically;
        }
        set
        {
            if (setControlPointsAutomatically != value)
            {
                setControlPointsAutomatically = value;
                if (setControlPointsAutomatically)
                {
                    AutoSetAllControlPoints();
                }
            }
        }
    }

    public int PointCount
    {
        get
        {
            return points.Count;
        }
    }

    public int SegmentCount
    {
        get
        {
            return points.Count / 3;
        }
    }

    public void AddSegment(Vector2 anchorPos, AnchorStatus status)
    {
        int lastAnchor = points.Count - 1;
        int lastControl = points.Count - 2;

        if (GetAnchorStatus(lastAnchor) == AnchorStatus.CORNER)
        {
            points.Add(points[lastAnchor]);
        } else {
            points.Add(points[lastAnchor] * 2 - points[lastControl]);
        }

        if (status == AnchorStatus.CORNER)
        {
            points.Add(anchorPos);
        } else {
            points.Add((points[lastAnchor] + anchorPos) * 0.5f);
        }
		
        points.Add(anchorPos);

        if (GetAnchorStatus(lastAnchor) == AnchorStatus.CORNER && status != AnchorStatus.CORNER)
        {
            AutoSetAnchorControlPoints(points.Count - 1);
        }

        if (setControlPointsAutomatically)
        {
            AutoSetAllAffectedControlPoints(points.Count - 1);
        }
        lockStatus.Add(status);
        ChainAutoSet(points.Count - 1);
    }

    public void SplitSegment(Vector2 anchorPos, int segmentIndex, AnchorStatus status)
    {
        if (status == AnchorStatus.CORNER)
        {
            points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { anchorPos, anchorPos, anchorPos });
        } else {
            points.InsertRange(segmentIndex * 3 + 2, new Vector2[] { Vector2.zero, anchorPos, Vector2.zero });
        }

        lockStatus.Insert(segmentIndex + 1, status);

        if (setControlPointsAutomatically)
        {
            AutoSetAllAffectedControlPoints(segmentIndex * 3 + 3);
        }
        else
        {
            if (status != AnchorStatus.CORNER)
                AutoSetAnchorControlPoints(segmentIndex * 3 + 3);
        }
        ChainAutoSet(segmentIndex * 3 + 3);
    }

    public void DeleteSegment(int anchorIndex)
    {
        if (SegmentCount > 2 || !closed && SegmentCount > 1)
        {
            if (anchorIndex == 0)
            {
                if (closed)
                {
                    points[points.Count - 1] = points[2];
                }
                points.RemoveRange(0, 3);
            }
            else if (anchorIndex == points.Count - 1 && !closed)
            {
                points.RemoveRange(anchorIndex - 2, 3);
            }
            else
            {
                points.RemoveRange(anchorIndex - 1, 3);
            }
            lockStatus.RemoveAt(anchorIndex/3);
            ChainAutoSet(anchorIndex);
        }

    }

    public Vector2[] GetPointsInSegment(int i)
    {
        if (i < SegmentCount)
        {
            return new Vector2[] { points[i * 3], points[i * 3 + 1], points[i * 3 + 2], points[LoopIndex(i * 3 + 3)] };
        } else {
            return null;
        }
    }

    public AnchorStatus GetAnchorStatus(int anchorIndex)
    {
        return lockStatus[anchorIndex / 3];
    }

    public void SetAnchorStatus(int anchorIndex, AnchorStatus status, bool force = false)
    {
        if (!force)
        {
            switch (status)
            {
                case AnchorStatus.AUTO:
                    AutoSetAnchorControlPoints(anchorIndex);
                    break;
                case AnchorStatus.CORNER:
                    int controlA = (PointCount + anchorIndex - 1) % PointCount;
                    int controlB = (anchorIndex + 1) % PointCount;

                    if (anchorIndex > 0 || closed)
                    {
                        points[controlA] = points[anchorIndex];
                    }
                    if (anchorIndex < PointCount - 1 || closed)
                    {
                        points[controlB] = points[anchorIndex];
                    }

                    break;
                case AnchorStatus.FREE:
                    if (GetAnchorStatus(anchorIndex) == AnchorStatus.CORNER)
                    {
                        AutoSetAnchorControlPoints(anchorIndex);
                    }
                    break;
                case AnchorStatus.LOCK:
                    if (GetAnchorStatus(anchorIndex) == AnchorStatus.CORNER || GetAnchorStatus(anchorIndex) == AnchorStatus.FREE)
                    {
                        AutoSetAnchorControlPoints(anchorIndex);
                    }
                    break;
            }
        }
        ChainAutoSet(anchorIndex);
        lockStatus[anchorIndex / 3] = status;
    }

    public void MovePoint(int i, Vector2 pos, bool force = false)
    {
        Vector2 deltaMove = pos - points[i];

        if (force)
        {
            points[i] = pos;
        } else {
			if (i % 3 == 0 || !setControlPointsAutomatically)
			{
				points[i] = pos;
				
				if (setControlPointsAutomatically)
				{
					AutoSetAllAffectedControlPoints(i);
				}
				else
				{
					if (i % 3 == 0)
					{
						if (i + 1 < points.Count || closed)
						{
							points[LoopIndex(i + 1)] += deltaMove;
						}
						if (i - 1 >= 0 || closed)
						{
							points[LoopIndex(i - 1)] += deltaMove;
						}

                        if (GetAnchorStatus(i) == AnchorStatus.AUTO)
                        {
							AutoSetAnchorControlPoints(i);
                        }
						ChainAutoSet(i);
					}
					else
					{
						int correspondingControlIndex, anchorIndex;

						if ((i + 1) % 3 == 0)
						{
                            if (i + 1 < points.Count || !closed)
                            {
                                correspondingControlIndex = i + 2;
                                anchorIndex = i + 1;
                            } else {
								correspondingControlIndex = 1;
                                anchorIndex = 0;
                            }
						}
						else
						{
							anchorIndex = i - 1;
                            if (i > 1 || !closed)
                            {
                                correspondingControlIndex = i - 2;
                            } else {
                                correspondingControlIndex = points.Count - 1;
                            }
						}

                        if (GetAnchorStatus(anchorIndex) == AnchorStatus.LOCK)
                        {
							if (correspondingControlIndex >= 0 && correspondingControlIndex < points.Count)
							{
								float dist = (points[LoopIndex(anchorIndex)] - points[LoopIndex(correspondingControlIndex)]).magnitude;
								Vector2 dir = (points[LoopIndex(anchorIndex)] - pos).normalized;
								points[LoopIndex(correspondingControlIndex)] = points[LoopIndex(anchorIndex)] + dir * dist;
							}
                        }
					}
				}
            }
        }
    }

    public void ChainAutoSet(int anchor)
    {
        for (int j = anchor - 3; j >= 0; j -= 3)
        {
            if (GetAnchorStatus(j) == AnchorStatus.AUTO)
            {
                AutoSetAnchorControlPoints(j);
            }
            else
            {
                break;
            }
        }

        for (int j = anchor + 3; j < PointCount; j += 3)
        {
            if (GetAnchorStatus(j) == AnchorStatus.AUTO)
            {
                AutoSetAnchorControlPoints(j);
            }
            else
            {
                break;
            }
        }
    }

    public Vector2[] GetEquidistancePoints(float spacing, float resolution = 1)
    {
        List<Vector2> equidistancePoints = new List<Vector2>();
        equidistancePoints.Add(points[0] + offset);
        Vector2 lasstPoint = points[0];
        float distSinceLastEDPoint = 0;

        for (int segmentI = 0; segmentI < SegmentCount; segmentI++)
        {
            Vector2[] p = GetPointsInSegment(segmentI);
            float controlNetLenght = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
            float estimatedCurveLength = Vector2.Distance(p[0], p[3]) + controlNetLenght / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);
            float t = 0;
            while (t <= 1)
            {
                t += 1f / divisions;
                Vector2 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                distSinceLastEDPoint += Vector2.Distance(lasstPoint, pointOnCurve);

                while (distSinceLastEDPoint >= spacing)
                {
                    float overshootDist = distSinceLastEDPoint - spacing;
                    Vector2 newEvenlySpacedPoint = pointOnCurve + (lasstPoint - pointOnCurve).normalized * overshootDist;
                    equidistancePoints.Add(newEvenlySpacedPoint + offset);
                    distSinceLastEDPoint = overshootDist;
                    lasstPoint = newEvenlySpacedPoint;
                }

                lasstPoint = pointOnCurve;
            }
        }

        return equidistancePoints.ToArray();
    }

    void AutoSetAllAffectedControlPoints(int updatedAnchorIndex)
    {
        for (int i = updatedAnchorIndex - 3; i < updatedAnchorIndex + 3; i += 3)
        {
            if (i >= 0 && i < points.Count || closed)
            {
                AutoSetAnchorControlPoints(LoopIndex(i));
            }
        }

        AutoSetStartAndEndControls();
    }

    void AutoSetAllControlPoints()
    {
        for (int i = 0; i < points.Count; i += 3)
        {
            AutoSetAnchorControlPoints(i);
        }
        AutoSetStartAndEndControls();
    }

    public void AutoSetAnchorControlPoints(int anchorIndex)
    {
        Vector2 anchorPos = points[anchorIndex];
        Vector2 dir = new Vector2();
        float[] neighbourDistances = new float[2];

        if (anchorIndex - 3 >= 0 || closed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
        if (anchorIndex + 3 >= 0 || closed)
        {
            Vector2 offset = points[LoopIndex(anchorIndex + 3)] - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();

        for (int i = 0; i < 2; i++)
        {
            int controlIndex = anchorIndex + i * 2 - 1;
            if (controlIndex >= 0 && controlIndex < points.Count || closed)
            {
                points[LoopIndex(controlIndex)] = anchorPos + dir * neighbourDistances[i] * 0.5f;
            }
        }

    }

    public Vector2[] CalculateAnchorControlPointsPositions(Vector2 posAux, int segment)
    {
        Vector2 anchorPos = posAux;
        Vector2 dir = new Vector2();
        float[] neighbourDistances = new float[2];

        if (segment * 3 - 3 >= 0 || closed)
        {
            Vector2 offset = points[LoopIndex(segment * 3 - 3)] - anchorPos;
            dir += offset.normalized;
            neighbourDistances[0] = offset.magnitude;
        }
        if (segment * 3 + 3 >= 0 || closed)
        {
            Vector2 offset = points[LoopIndex(segment * 3 + 3)] - anchorPos;
            dir -= offset.normalized;
            neighbourDistances[1] = -offset.magnitude;
        }

        dir.Normalize();


        List<Vector2> ret = new List<Vector2>();
        for (int i = 0; i < 2; i++)
        {
            ret.Add(anchorPos + dir * neighbourDistances[i] * 0.5f);
        }
        return ret.ToArray();
    }

    void AutoSetStartAndEndControls()
    {
        if (!closed)
        {
            points[1] = (points[0] + points[2]) * 0.5f;
            points[points.Count - 2] = (points[points.Count - 1] + points[points.Count - 3]) * 0.5f;
        }
    }

    int LoopIndex(int i)
    {
        return (i + points.Count) % points.Count;
    }

    public void translate(Vector3 mov)
    {
        for (int i = 0; i < points.Count; i++)
        {
            points[i] += (Vector2)mov;
        }
    }

    public float[] pointDistances(float spacing, float resolution = 1)
    {
        List<float> distancias = new List<float>();
        Vector2 previousPoint = points[0];
		float distanciaSegmento = 0;

        for (int segmentIndex = 0; segmentIndex < SegmentCount; segmentIndex++)
        {
            Vector2[] p = GetPointsInSegment(segmentIndex);
            float controlNetLenght = Vector2.Distance(p[0], p[1]) + Vector2.Distance(p[1], p[2]) + Vector2.Distance(p[2], p[3]);
            float estimatedCurveLength = Vector2.Distance(p[0], p[3]) + controlNetLenght / 2f;
            int divisions = Mathf.CeilToInt(estimatedCurveLength * resolution * 10);
            
            float t = 1f / divisions;
            while (t < 1)
            {
                Vector2 pointOnCurve = Bezier.EvaluateCubic(p[0], p[1], p[2], p[3], t);
                distanciaSegmento += Vector2.Distance(previousPoint, pointOnCurve);
                previousPoint = pointOnCurve;
				t += 1f / divisions;
            }
            distancias.Add(distanciaSegmento);
        }

        return distancias.ToArray();
    }

    public void MoveSegment(int segment, Vector2 movement, Vector2 origen)
    {
        float distToA = (points[segment * 3 + 1] - origen).magnitude;
        float distToB = (points[segment * 3 + 2] - origen).magnitude;

        if (GetAnchorStatus(segment * 3) == AnchorStatus.CORNER)
            SetAnchorStatus(segment * 3, AnchorStatus.FREE, true);

        if (GetAnchorStatus(segment * 3) == AnchorStatus.AUTO)
            SetAnchorStatus(segment * 3, AnchorStatus.LOCK);

        if (GetAnchorStatus((segment * 3 + 3) % points.Count) == AnchorStatus.CORNER)
            SetAnchorStatus((segment * 3 + 3) % points.Count, AnchorStatus.FREE, true);

        if (GetAnchorStatus((segment * 3 + 3) % points.Count) == AnchorStatus.AUTO)
            SetAnchorStatus((segment * 3 + 3) % points.Count, AnchorStatus.LOCK);

        MovePoint(segment * 3 + 1, points[segment * 3 + 1] + movement * distToB / (distToA + distToB));
        MovePoint(segment * 3 + 2, points[segment * 3 + 2] += movement * distToA / (distToA + distToB));
    }

}
