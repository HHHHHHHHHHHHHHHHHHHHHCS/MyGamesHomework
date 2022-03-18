using UnityEngine;

namespace Games_103_2
{
	public class BlockMotion : MonoBehaviour
	{
		private bool pressed = false;
		private bool block_move = false;
		private Vector3 offset;


		private void Update()
		{
			if (Input.GetMouseButtonDown(0))
			{
				pressed = true;
				Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
				if (Vector3.Cross(ray.direction, transform.position - ray.origin).magnitude < 0.8f) block_move = true;
				else block_move = false;
				offset = Input.mousePosition - Camera.main.WorldToScreenPoint(transform.position);
			}

			if (Input.GetMouseButtonUp(0))
			{
				pressed = false;
				block_move = false;
			}

			if (pressed)
			{
				if (block_move)
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
				}
			}
		}
	}
}