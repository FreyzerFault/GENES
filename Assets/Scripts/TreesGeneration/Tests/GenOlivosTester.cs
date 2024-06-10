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

		public bool playing;

		public int initialSeed;
		private int iterations;

		private void Awake()
		{
			_generator = GetComponent<OliveGroveGenerator>();
			_generator.runOnStart = true;
			_generator.randSeed = initialSeed;

			iterations = 0;
		}

		private void Start()
		{
			_generator.Reset();
			_generator.GenerateSeeds();

			_testingCoroutine = StartCoroutine(TestCoroutine());
		}

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
				if (iterations > 0)
				{
					_generator.Reset();
					_generator.RandomizeSeeds();
				}

				yield return _generator.RunCoroutine();
				yield return new WaitForSeconds(1);
				yield return new WaitUntil(() => playing);
				iterations++;
			}
		}

		public void TogglePlaying(bool playing) => this.playing = playing;


		#region DEBUGGING

		[ExposedField]
		public string ToggleLabel => playing ? "PLAYING" : "STOP";

		#endregion
	}
}
