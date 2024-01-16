using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class SplinePrinter : MonoBehaviour
{
	#if UNITY_EDITOR
    EasySplinePath2D esp2D;

    private bool movingAssets = false;
    private void Awake()
    {
        if (AssetDatabase.IsValidFolder("Assets/EasySplinePath2D/Editor Default Resources/EasySplinePath2D") && !AssetDatabase.IsValidFolder("Assets/Editor Default Resources/EasySplinePath2D") && !movingAssets)
        {
	        movingAssets = true;

            if (!AssetDatabase.IsValidFolder("Assets/Editor Default Resources"))
                AssetDatabase.CreateFolder("Assets", "Editor Default Resources");
            
            FileUtil.MoveFileOrDirectory("Assets/EasySplinePath2D/Editor Default Resources/EasySplinePath2D", "Assets/Editor Default Resources/EasySplinePath2D");
            FileUtil.DeleteFileOrDirectory("Assets/EasySplinePath2D/Editor Default Resources");
            AssetDatabase.Refresh();
        }
    }

	private void Start()
	{
        if (Application.isEditor)
        {
			esp2D = GetComponent<EasySplinePath2D>();
            init();
            StartCoroutine(ScaleRoutine());
        }
	}

    public void init()
    {
        SceneView.duringSceneGui += delegate (SceneView sv)
        {
            OnSceneDelegate(sv);
        };
    }

    void OnSceneDelegate(SceneView sv)
    {
        if (esp2D != null && esp2D.allwaysVisible && !esp2D.selected && esp2D.isActiveAndEnabled)
        {
            for (int i = 0; i < esp2D.path.SegmentCount; i++)
            {
                Vector2[] points = esp2D.GetPointsInSegment(i);
                Color segmentCol = esp2D.lineColor;
                Handles.DrawBezier(points[0], points[3], points[1], points[2], segmentCol, null, 2);
            }
        }
    }

    IEnumerator ScaleRoutine()
    {
        while (true)
        {
			if (transform.hasChanged)
			{
                if (transform.lossyScale != esp2D.lastScale)
				{
					Vector2 scaleMult = new Vector2(1, 1);
					
					if (transform.lossyScale.x != 0)
					{
                        scaleMult.x = transform.lossyScale.x / esp2D.lastScale.x;
                        esp2D.lastScale.x = transform.lossyScale.x;
					}
					if (transform.lossyScale.y != 0)
					{
                        scaleMult.y = esp2D.transform.lossyScale.y / esp2D.lastScale.y;
                        esp2D.lastScale.y = esp2D.transform.lossyScale.y;
					}
					
					for (int i = 0; i < esp2D.path.PointCount; i++)
					{
						esp2D.path.points[i] = new Vector2(esp2D.path.points[i].x * scaleMult.x, esp2D.path.points[i].y * scaleMult.y);
					}
					Undo.RecordObject(esp2D, "Change Scale");
				}

                if (transform.eulerAngles != esp2D.lastRotation && esp2D.path != null)
                {
                    float angulo = Mathf.Deg2Rad * (transform.eulerAngles.z - esp2D.lastRotation.z);

                	Vector2 matrizX = new Vector2(Mathf.Cos(angulo), -Mathf.Sin(angulo));
                    Vector2 matrizY = new Vector2(Mathf.Cos(angulo), Mathf.Sin(angulo));
                	
                	for (int i = 0; i < esp2D.path.PointCount; i++)
                	{
                    	Vector2 point = esp2D.path.points[i];
                    	esp2D.path.points[i] = new Vector2(point.x * matrizX.x + point.y * matrizX.y, point.y * matrizY.x + point.x * matrizY.y);
                	}
                    esp2D.lastRotation = transform.eulerAngles;

                	Undo.RecordObject(esp2D, "Rotation Change");
                }

				transform.hasChanged = false;
			}
            yield return null;
        }
    }


    #endif
}
