using System.Collections;
using DavidUtils.DevTools.Reflection;
using TreesGeneration;
using UnityEngine;

namespace GENES.TreesGeneration.Tests
{
	[RequireComponent(typeof(OliveGroveGenerator))]
	public class GenOlivosTester : MonoBehaviour
	{
		private OliveGroveGenerator _generator;
		private Coroutine _testingCoroutine;

		[ExposedField]
		public bool playing;

		[ExposedField]
		public int seed;

		private void Awake()
		{
			_generator = GetComponent<OliveGroveGenerator>();
			_generator.runOnStart = true;
		}

		private void Start() => _testingCoroutine = StartCoroutine(TestCoroutine());

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space)) StopTest();
		}

		private void StopTest()
		{
			playing = false;
			_generator.Reset();
		}

		private IEnumerator TestCoroutine()
		{
			playing = true;
			while (playing)
			{
				_generator.Reset();
				_generator.RandomizeSeeds(seed);
				yield return _generator.RunCoroutine();
				yield return new WaitForSeconds(1);
			}
		}


		#region DEBUGGING

		public string ToggleLabel => playing ? "PLAY" : "STOP";

		#endregion
	}
}
