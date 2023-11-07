using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PolygonGenerator : MonoBehaviour
{
    [SerializeField][Range(3, 100)]
    private int polygonPoints = 3;  // 다각형 점 갯수(3~100개)

    [SerializeField][Min(0.1f)]
    private float outerRadius = 3;  // 다각형의 원점부터 외곽 둘레까지의 반지름 (최소 값 0.1)

    private Mesh mesh;
    private Vector3[] vertices; // 다각형의 정점 정보 배열
    private int[] indices;  // 정점을 잇는 폴리곤 정보 배열

    private void Awake()
    {
        mesh = new Mesh();
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;
    }

    private void Update()
    {
        DrawFilled(polygonPoints, outerRadius);
    }

    private void DrawFilled(int sides, float radius)
    {
        // 정점 정보
        vertices = GetCircumferencePoints(sides, radius);
        //정점을 잇는 폴리곤 정보
        indices = DrawFilledIndices(vertices);
        // 메시 생성
        GeneratePolygon(vertices, indices);
    }

    private int[] DrawFilledIndices(Vector3[] vertices)
    {
        int triangleCount = vertices.Length - 2;
        List<int> indices = new List<int>();

        for (int i = 0; i < triangleCount; ++i)
        {
            indices.Add(0);
            indices.Add(i+2);
            indices.Add(i+1);
        }

        return indices.ToArray();
    }
    
    private void GeneratePolygon(Vector3[] vertices, int[] indices)
    {
        // 점, 반지름 정보에 따라 Update에서 지속적으로 업데이트 하기 때문에 기존 mesh 정보를 초기화
        mesh.Clear();
        
        // 정점, 폴리곤 설정
        mesh.vertices = vertices;
        mesh.triangles = indices;
        // Bounds, Normal 재연산
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    
    /// <summary>
    /// 반지름이 radius인 원의 둘레에 있는 side 개수만큼의 정점 위치 정보 반환
    /// </summary>
    /// <param name="sides"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private Vector3[] GetCircumferencePoints(int sides, float radius)
    {
        Vector3[] points = new Vector3[sides];
        float anglePerStep = 2 * Mathf.PI * ((float)1 / sides);

        for (int i = 0; i < sides; ++i)
        {
            Vector2 point = Vector2.zero;
            float angle = anglePerStep * i;

            point.x = Mathf.Cos(angle) * radius;
            point.y = Mathf.Sin(angle) * radius;

            points[i] = point;
        }

        return points;
    }
}