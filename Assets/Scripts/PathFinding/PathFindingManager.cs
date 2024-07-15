using DavidUtils;

namespace GENES.PathFinding
{
	public class PathFindingManager : Singleton<PathFindingManager>
	{
		public PathFindingGenerator mainPathFindingGenerator;

		private void Start()
		{
			if (mainPathFindingGenerator == null)
				mainPathFindingGenerator = FindObjectOfType<PathFindingGenerator>();
		}
	}
}
