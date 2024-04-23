using DavidUtils.PlayerControl;
using UnityEngine;

namespace Core
{
	public class SimplePlayer3P_TerrainSticked : SimplePlayer3P
	{
		private Terrain _terrain;
		
		protected override void Awake()
		{
			base.Awake();
			_terrain = FindObjectOfType<Terrain>();

			if (_terrain == null) return;
			StickToTerrainHeight();
		}


		protected override void FixedUpdate()
		{
			base.FixedUpdate();

			if (_terrain == null) return;
			StickToTerrainHeight();
		}
		
		private void StickToTerrainHeight()
		{
			Vector3 position = transform.position;
			float height = _terrain.SampleHeight(position);
			position.y = height + 1;
			transform.position = position;
		}
	}
}
