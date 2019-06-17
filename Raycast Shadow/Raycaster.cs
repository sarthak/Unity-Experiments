using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Behaves like a struct to store array of end-points after batching and optimizations
public class RaycastEndpointData {
	public int total;
	public Vector3[] end_points;
}

public class Raycaster : MonoBehaviour
{
	[Tooltip("Total number of rays to fire 360deg")]
	public int TOTALRAYS;
	[Tooltip("Range of coverage")]
	public float RANGE;
	[Tooltip("How many rays to skip during batching of 'no-hit' rays")]
	public int SKIP_FACTOR;
	[Tooltip("How many iterations to do when finding the ray closest to corner")]
	public int ITERATION_COUNT;
	[Tooltip("Reference to MeshCreator object, should be child of this object")]
	public MeshCreator mcr;

	private float delta_rad;
	private RaycastEndpointData epd;

	//If a ray does not hit any point, it is said to have a normal of NO_NORMAL
	//NO_NORMAL is conventionally equal to down vector
	Vector3 NO_NORMAL = Vector3.down;

	void Start() {
		//Initialize
		delta_rad = 2*Mathf.PI/TOTALRAYS;
		epd = new RaycastEndpointData();
		//The line below could be optimized to reduce space, right now it is set to fail-safe
		epd.end_points = new Vector3[2*TOTALRAYS];
	}

	void DrawLines() {
		//Uncomment the lines below to draw gizmos lines
		/* for (int i=0; i<epd.total; i++) */
		/* 	Debug.DrawLine(transform.position, epd.end_points[i], Color.red); */
		mcr.CreateMesh(epd);
	}

	void Update() {
		//Some more local initialization
		
		RaycastHit info;
		//how many rays have been cast this frame
		int rays_cast = 0;
		//What angle to cast the next ray at, updates by delta_rad after each fire
		float theta = 0.0f;
		//normal vector of the point hit by the last ray
		Vector3 normal_before = Vector3.zero;
		//normal vector of the point hit by current ray
		Vector3 normal_now = Vector3.zero;
		//Slope vector of last ray
		Vector3 slope_before = NO_NORMAL;
		//Slope vector of current ray
		Vector3 slope_now = NO_NORMAL;
		//Just a counter to keep track of how many end points have been detected (after batching and optimization)
		int c = 0;
		//Just a counter to keep track of rays skipped during batching
		int skips = 0;

		while (rays_cast < TOTALRAYS) {
			//Point of ray termination
			Vector3 end;
			//Which direction to fire the ray?
			Vector3 dir = new Vector3(Mathf.Sin(theta), 0, Mathf.Cos(theta));
			//If rays hits, fetch the information of normal and end point
			if (Physics.Raycast(transform.position, dir, out info, RANGE)) {
				end = info.point;
				normal_now = info.normal;
			}
			else {
				//otherwise calculate end point and set normal to NO_NORMAL
				end = transform.position + dir * RANGE;
				normal_now = NO_NORMAL;
			}
			//Uncomment the line below to see gizmos line for each ray cast
			/* Debug.DrawLine(transform.position, end); */
			rays_cast ++;

			//Special treatment for first ray
			if (rays_cast == 1) {
				epd.end_points[0] = end;
				normal_before = normal_now;
				continue;
			}

			//Here is the slope formula, slope = change in normal
			slope_now = normal_now - normal_before;
			if (normal_now != normal_before) {
				//2 possibilities - Curved surface aur broken surface
				//_theta = theta before (the lesser one)
				float _theta = theta - delta_rad;
				//theta_ = theta now (the bigger one)
				float theta_ = theta;

				for (int i=0; i<ITERATION_COUNT; i++) {
					float mid_theta = (_theta + theta_)/2;
					//Same as before except the calculations are done for ray fired at midpoint
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
						//broken surface, optimize the ray
						epd.end_points[c] = mid_end;
						_theta = mid_theta;
					} else if (mid_normal == normal_now) {
						//broken surface, optimze the ray
						end = mid_end;
						theta_ = mid_theta;
					} else {
						//looks like a curved surface, add this and move on
						epd.end_points[++c] = mid_end;
						break;
					}
				}
			}

			theta += delta_rad;
			//Change in slope => Time to add this end point to our collection (otherwise it would be batched)
			if (slope_now != slope_before) {
				c++;
			}
			else if (normal_now == NO_NORMAL) {
				//Special batching for rays which don't hit any point
				if (skips < SKIP_FACTOR)
					skips += 1;
				else {
					skips = 0;
					c++;
				}
			}

			//update the before-values
			slope_before = slope_now;
			normal_before = normal_now;
			epd.end_points[c] = end;
		}
		epd.total = c+1;
		DrawLines();
	}
}
