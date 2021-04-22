using System;
using UnityEngine;

namespace Common
{
	[RequireComponent(typeof(Camera))]
	public class BlitMonoCtrl : MonoBehaviour
	{
		public bool enableBlit = false;
		public int rtID { get; set; } = -1;
		public RenderTexture rt { get; set; } = null;

	}
}