using UnityEngine;
using System.Collections;

public class SceneInitJumper : MonoBehaviour
{
	static bool jumped;

	void Awake()
	{
		if (!jumped)
		{
			jumped = true;
			UnityEngine.SceneManagement.SceneManager.LoadScene ("Initialize");
		}
	}
}
