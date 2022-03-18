using UnityEngine;

namespace Games_103_2
{
	public class CubeMotion : MonoBehaviour
	{
		private bool pressed = false;
		private bool cubeMove = false;
		private Vector3 offset;


		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				pressed = true;
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude < 0.8f)
				{
					cubeMove = true;
				}
				else
				{
					cubeMove = false;
				}
				offset = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
			}

			if (Input.GetMouseButtonUp(0))
			{
				pressed = false;
				cubeMove = false;
			}

			if (pressed)
			{
				if (cubeMove)
				{
					Vector3 mouse = Input.mousePosition;
					mouse -= offset;
					mouse.z = Camera.main.WorldToScreenPoint(transform.position).z;
					Vector3 p = Camera.main.ScreenToWorldPoint(mouse);
					p.y = transform.position.y;
					transform.position = p;
				}
				else
				{
					//float h = 2.0f * Input.GetAxis("Mouse X");
					//Camera.main.transform.RotateAround(new Vector3(0, 0, 0), Vector3.up, h);
				}
			}
		}
	}
}