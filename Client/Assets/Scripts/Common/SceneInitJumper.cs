using UnityEngine;
using System.Collections;

public class SceneInitJumper : MonoBehaviour
{
	static bool jumped;
	public bool StartScene;

	void Awake()
	{
		if (StartScene)
		{
			jumped = true;
		}
		else if (!jumped)
		{
			jumped = true;
			UnityEngine.SceneManagement.SceneManager.LoadScene ("Initialize");
		}
	}

}
