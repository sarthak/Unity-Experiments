using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateTimesTable : MonoBehaviour
{
	public float tilt = 0.0f;
	public float radius;
	public int n;
	public int points;

	public LineRenderer line_prefab;
	public LineRenderer circle;

	private Vector3 PointOnCircle (int k) {
		float theta = k * 2 * Mathf.PI/points + Mathf.Deg2Rad * (tilt);
		return new Vector3 (radius * Mathf.Sin(theta), -radius * Mathf.Cos(theta), 0);
	}

	public IEnumerator DrawLines() {
		LineRenderer[] lines = new LineRenderer[points];
		Vector3[] circle_points = new Vector3[points];

		for (int i=0; i<points; i++)
			circle_points[i] = PointOnCircle(i);

		for (int i=0; i<points; i++) {
			LineRenderer line = Instantiate<LineRenderer>(line_prefab);
			line.positionCount = 2;
			line.SetPositions(new Vector3[] {circle_points[i], circle_points[(n*i)%points]});
			lines[i] = line;

			circle.positionCount = i+1;
			circle.SetPositions(circle_points);
			yield return null;
		}
		circle.loop = true;

		bool state = false;
		while (true) {
			for (int i=0; i<points; i++) {
				lines[i].gameObject.SetActive(state);
				yield return null;
			}
			state = !state;
		}
	}

	void Update() {
		if (Input.GetKeyDown(KeyCode.Space)) {
			StartCoroutine(DrawLines());
		}
	}
}
