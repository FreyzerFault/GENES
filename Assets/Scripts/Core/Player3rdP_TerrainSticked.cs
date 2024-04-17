using DavidUtils.PlayerControl;
using UnityEngine;

namespace Core
{
	public class Player3rdP_TerrainSticked : Player3rdP
	{
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

		#region TERRAIN

		private Terrain _terrain;

		private void StickToTerrainHeight()
		{
			Vector3 position = transform.position;
			float height = _terrain.SampleHeight(position);
			position.y = height + 1;
			transform.position = position;
		}

		#endregion

		private void OnCollisionEnter(Collision other)
		{
			Debug.Log("Collision with " + other.gameObject.name, this);
		}
	}
}
