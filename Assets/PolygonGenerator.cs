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

    [SerializeField][Min(0)]
    private float innerRadius;  // 다각형 내부가 뚤려있을때 원점부터 뚤린 외곽까지의 반지름
    
    [SerializeField][Min(1)]
    private int repeatCount = 1;    // 텍스쳐 반복 횟수
    
    private Mesh mesh;
    private Vector3[] vertices; // 다각형의 정점 정보 배열
    private int[] indices;  // 정점을 잇는 폴리곤 정보 배열
    private Vector2[] uv;   // 이미지 출력을 위한 각 정점의 uv 정보 배열

    private EdgeCollider2D edgeCollider2D;
    
    private void Awake()
    {
        mesh = new Mesh();
        
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        edgeCollider2D = gameObject.AddComponent<EdgeCollider2D>();
    }

    private void Update()
    {
        // innerRadius가 outerRadius보다 클 수 없음.
        innerRadius = innerRadius > outerRadius ? outerRadius - 0.1f : innerRadius;
        
        if(innerRadius == 0)
            DrawFilled(polygonPoints, outerRadius);
        else
        {
            DrawHollow(polygonPoints, outerRadius, innerRadius);
        }
    }

    private void DrawFilled(int sides, float radius)
    {
        // 정점 정보
        vertices = GetCircumferencePoints(sides, radius);
        //정점을 잇는 폴리곤 정보
        indices = DrawFilledIndices(vertices);
        // 각 정점의 uv 정보
        uv = GetUVPoints(vertices, radius, repeatCount);
        // 메시 생성
        GeneratePolygon(vertices, indices, uv);
        
        // EdgeCollider2D에 정점 정보를 넣어줌
        edgeCollider2D.points = GetEdgePoints(vertices);
    }

    /// <summary>
    /// 원의 가장자리에 있는 정점 정보들을 기반으로 폴리곤 영역
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
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

        // 0, 2, 1 / 0, 3, 2 / 0, 4, 3... 식으로 연속된 삼각형의 배열을 리턴.
        return indices.ToArray();
    }

    private void DrawHollow(int sides, float outerRadius, float innerRadius)
    {
        Vector3[] outerPoints = GetCircumferencePoints(sides, outerRadius);
        Vector3[] innerPoints = GetCircumferencePoints(sides, innerRadius);

        List<Vector3> points = new List<Vector3>();
        points.AddRange(outerPoints);
        points.AddRange(innerPoints);
        
        // 정점 정보
        vertices = points.ToArray();
        // 폴리곤 정보
        indices = DrawHollowIndices(sides);
        // 정점의 uv정보
        uv = GetUVPoints(vertices, outerRadius, repeatCount);
        
        GeneratePolygon(vertices, indices, uv);

        // 정점 정보를 바탕으로 충돌 범위 생성.
        List<Vector2> edgePoints = new List<Vector2>();
        edgePoints.AddRange(GetEdgePoints(outerPoints));    // 바깥쪽 둘레 충돌 범위
        edgePoints.AddRange(GetEdgePoints(innerPoints));    // 안쪽 둘레 충돌 범위
        edgeCollider2D.points = edgePoints.ToArray();
    }

    private int[] DrawHollowIndices(int sides)
    {
        List<int> indices = new List<int>();

        for (int i = 0; i < sides; ++i)
        {
            int outerIndex = i;
            int innerIndex = i + sides;
            
            indices.Add(outerIndex);
            indices.Add(innerIndex);
            indices.Add((outerIndex + 1) % sides);
            
            indices.Add(innerIndex);
            indices.Add(sides + ((innerIndex + 1) % sides));
            indices.Add((outerIndex + 1) % sides);
        }

        return indices.ToArray();
    }
    
    private void GeneratePolygon(Vector3[] vertices, int[] indices, Vector2[] uv)
    {
        // 점, 반지름 정보에 따라 Update에서 지속적으로 업데이트 하기 때문에 기존 mesh 정보를 초기화
        mesh.Clear();
        
        // 정점, 폴리곤, uv 설정
        mesh.vertices = vertices;
        mesh.triangles = indices;
        mesh.uv = uv;
        
        // Bounds, Normal 재연산
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }
    
    /// <summary>
    /// 반지름이 radius인 원의 둘레에 있는 side 개수만큼의 정점 위치 정보 반환
    /// </summary>
    /// <param name="sides">정점 갯수</param>
    /// <param name="radius">다각형크기 원의 반지름</param>
    /// <returns></returns>
    private Vector3[] GetCircumferencePoints(int sides, float radius)
    {
        Vector3[] points = new Vector3[sides];
        // 360도를 정점 갯수로 나누어 각도 당 얼마만큼의 각도를 가지는지 계산
        // 원의 둘레(2 * PI)(radian) / 정점 갯수 = 각도 당 각도
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

    private Vector2[] GetUVPoints(Vector3[] vertices, float outerRadius, int repeatCount)
    {
        Vector2[] points = new Vector2[vertices.Length];

        for (int i = 0; i < vertices.Length; ++i)
        {
            Vector2 point = Vector2.zero;

            // -outerRadius ~ outerRadius 사이의 값을 0 ~ 1 사이의 값으로 변환
            point.x = vertices[i].x / outerRadius * 0.5f + 0.5f;
            point.y = vertices[i].y / outerRadius * 0.5f + 0.5f;
            
            // 텍스쳐 반복 출력
            point *= repeatCount;
            points[i] = point;
        }

        return points;
    }

    /// <summary>
    /// Vector3[] 정점 정보를 Vector2[] 배열로 변경하고, 첫 번째 정점 정보를 추가해 충돌 범위가 닫힌 형태가 되도록 설정
    /// </summary>
    /// <param name="vertices"></param>
    /// <returns></returns>
    private Vector2[] GetEdgePoints(Vector3[] vertices)
    {
        Vector2[] points = new Vector2[vertices.Length + 1];
        for (int i = 0; i < vertices.Length; ++i)
        {
            points[i] = vertices[i];
        }

        points[points.Length - 1] = vertices[0];

        return points;
    }
}