using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshCreator : MonoBehaviour
{
	public MeshFilter filter;

	Mesh mesh;

	void Start() {
		mesh = new Mesh();
		mesh.MarkDynamic();
		filter.mesh = mesh;
	}

	public void CreateMesh(RaycastEndpointData epd) {
		Vector3[] vertices = new Vector3[epd.total+1];
		Vector2[] uv = new Vector2[epd.total+1];
		vertices[0] = Vector3.zero;
		uv[0] = new Vector2(0.5f, 0.5f);

		for (int i=0; i<epd.total; i++){
			vertices[i+1] = epd.end_points[i]-transform.position;
			float dist = vertices[i+1].magnitude/30f;
			if (dist>0.6f)
				print ("error");
			uv[i+1] = new Vector2(0.5f-dist, 0.5f-dist);
		}

		int[] triangles = new int[3*epd.total];
		for (int i=0; i<epd.total-1; i++) {
			triangles[3*i + 0] = 0;
			triangles[3*i + 1] = i+1;
			triangles[3*i + 2] = i+2;
		}
		triangles[3*epd.total-3] = 0;
		triangles[3*epd.total-2] = epd.total;
		triangles[3*epd.total-1] = 1;

		mesh.Clear();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.RecalculateNormals();
		mesh.uv = uv;
	}
}
