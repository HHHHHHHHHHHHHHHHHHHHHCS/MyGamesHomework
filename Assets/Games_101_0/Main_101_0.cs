using Unity.Mathematics;
using UnityEngine;

namespace Games_101_0
{
	public class Main_101_0 : MonoBehaviour
	{
		private void Awake()
		{
			//二维点  带齐次坐标
			double3 p = new double3(1, 2, 1);

			Debug.Log(p);

			double angle = math.radians(45.0);

			double3x3 r = new double3x3(math.cos(angle), -math.sin(angle), 0, math.sin(angle), math.cos(angle), 0, 0, 0, 1);

			p = math.mul(r, p);

			Debug.Log(p);

			double3x3 t = double3x3.identity;
			t.c2.x = 1;
			t.c2.y = 2;

			p = math.mul(t, p);

			Debug.Log(p);
		}
	}
}