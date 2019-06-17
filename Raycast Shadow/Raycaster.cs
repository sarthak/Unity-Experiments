using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaycastEndpointData {
	public int total;
	public Vector3[] end_points;
}

public class Raycaster : MonoBehaviour
{
	public int TOTALRAYS;
	public float RANGE;
	public int SKIP_FACTOR;
	public int ITERATION_COUNT;
	public MeshCreator mcr;

	float delta_rad;
	RaycastEndpointData epd;

	Vector3 NO_NORMAL = Vector3.down;

	void Start() {
		delta_rad = 2*Mathf.PI/TOTALRAYS;
		epd = new RaycastEndpointData();
		epd.end_points = new Vector3[2*TOTALRAYS];
	}

	void DrawLines() {
		/* for (int i=0; i<epd.total; i++) */
		/* 	Debug.DrawLine(transform.position, epd.end_points[i], Color.red); */
		mcr.CreateMesh(epd);
	}

	void Update() {
		RaycastHit info;
		int rays_cast = 0;
		float theta = 0.0f;
		Vector3 normal_before = Vector3.zero;
		Vector3 normal_now = Vector3.zero;
		Vector3 slope_before = NO_NORMAL;
		Vector3 slope_now = NO_NORMAL;
		int c = 0;
		int skips = 0;

		while (rays_cast < TOTALRAYS) {
			Vector3 end;
			Vector3 dir = new Vector3(Mathf.Sin(theta), 0, Mathf.Cos(theta));
			if (Physics.Raycast(transform.position, dir, out info, RANGE)) {
				end = info.point;
				normal_now = info.normal;
			}
			else {
				end = transform.position + dir * RANGE;
				normal_now = NO_NORMAL;
			}
			Debug.DrawLine(transform.position, end);
			rays_cast ++;

			if (rays_cast == 1) {
				epd.end_points[0] = end;
				normal_before = normal_now;
				continue;
			}

			slope_now = normal_now - normal_before;
			if (normal_now != normal_before) {
				//2 possibilities - Curved surface aur broken surface
				float _theta = theta - delta_rad;
				float theta_ = theta;
				for (int i=0; i<ITERATION_COUNT; i++) {
					float mid_theta = (_theta + theta_)/2;
					Vector3 mid_normal;
					Vector3 mid_end;
					Vector3 mid_dir = new Vector3(Mathf.Sin(mid_theta), 0, Mathf.Cos(mid_theta));
					if (Physics.Raycast(transform.position, mid_dir, out info, RANGE)) {
						mid_end = info.point;
						mid_normal = info.normal;
					} else {
						mid_end = transform.position + mid_dir * RANGE;
						mid_normal = NO_NORMAL;
					}

					if (mid_normal == normal_before) {
						epd.end_points[c] = mid_end;
						_theta = mid_theta;
					} else if (mid_normal == normal_now) {
						end = mid_end;
						theta_ = mid_theta;
					} else {
						epd.end_points[++c] = mid_end;
						break;
					}
				}
			}
			theta += delta_rad;
			if (slope_now != slope_before) {
				c++;
			}
			else if (normal_now == NO_NORMAL) {
				if (skips < SKIP_FACTOR)
					skips += 1;
				else {
					skips = 0;
					c++;
				}
			}
			slope_before = slope_now;
			normal_before = normal_now;
			epd.end_points[c] = end;
		}
		epd.total = c+1;
		DrawLines();
	}

}
