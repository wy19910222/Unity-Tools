using UnityEngine;

/// <summary>
/// Example script that uses EasySplinePath2DPlus to generate a mesh that follows the spline.
/// </summary>
[RequireComponent(typeof(EasySplinePath2DPlus))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class EasyRoadPlus : MonoBehaviour
{
    public float trackWidth = 3;
    [Range(.05f, 1.5f)]
    public float segmentLength = 1;
    public float tiling = 1;
	public bool liveUpdate;

	public void UpdateMesh()
    {
        SplinePath2D spline = GetComponent<EasySplinePath2DPlus>().path;
        Vector2[] points = spline.GetEquidistancePoints(segmentLength);
        GetComponent<MeshFilter>().mesh = CreateMesh(points, spline.IsClosed, trackWidth);
        int texture = Mathf.RoundToInt(tiling * points.Length * segmentLength * .05f);
        GetComponent<MeshRenderer>().sharedMaterial.mainTextureScale = new Vector2(1, texture);
    }

    /// <summary>
    /// Function to create the mesh of the road, from the points of the curve.
    /// </summary>
    public Mesh CreateMesh(Vector2[] points, bool closed, float roadWidth)
    {
        Vector3[] vertices = new Vector3[points.Length * 2];
        Vector2[] uvs = new Vector2[vertices.Length];
        int numTris = 2 * (points.Length - 1) + ((closed) ? 2 : 0);
        int[] tris = new int[numTris * 3];
        int vertIndex = 0;
        int triIndex = 0;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 forward = Vector2.zero;
            if (i < points.Length - 1 || closed)
            {
                forward += points[(i + 1) % points.Length] - points[i];
            }
            if (i > 0 || closed)
            {
                forward += points[i] - points[(i - 1 + points.Length) % points.Length];
            }
            forward.Normalize();
            Vector2 left = new Vector2(-forward.y, forward.x);

            vertices[vertIndex] = points[i] + left * roadWidth * .5f;
            vertices[vertIndex + 1] = points[i] - left * roadWidth * .5f;

            float completionPercent = i / (float)(points.Length - 1);
            float v = 1 - Mathf.Abs(2 * completionPercent - 1);
            uvs[vertIndex] = new Vector2(0, v);
            uvs[vertIndex + 1] = new Vector2(1, v);

            if (i < points.Length - 1 || closed)
            {
                tris[triIndex] = vertIndex;
                tris[triIndex + 1] = (vertIndex + 2) % vertices.Length;
                tris[triIndex + 2] = vertIndex + 1;

                tris[triIndex + 3] = vertIndex + 1;
                tris[triIndex + 4] = (vertIndex + 2) % vertices.Length;
                tris[triIndex + 5] = (vertIndex + 3) % vertices.Length;
            }

            vertIndex += 2;
            triIndex += 6;
        }

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.uv = uvs;

        return mesh;
    }
}
