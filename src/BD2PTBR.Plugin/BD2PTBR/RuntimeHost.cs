using UnityEngine;
using UnityEngine.SceneManagement;

namespace BD2PTBR;

internal sealed class RuntimeHost : MonoBehaviour
{
	private static RuntimeHost _instance;

	public static void EnsureCreated()
	{
		if (_instance != null)
		{
			return;
		}
		GameObject hostObject = new GameObject("BD2PTBR.RuntimeHost");
		hostObject.hideFlags = HideFlags.HideAndDontSave;
		Object.DontDestroyOnLoad(hostObject);
		_instance = hostObject.AddComponent<RuntimeHost>();
	}

	public static void DestroyIfCreated()
	{
		if (_instance == null)
		{
			return;
		}

		Object.Destroy(_instance.gameObject);
		_instance = null;
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void Update()
	{
		RuntimeCoordinator.Tick();
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		RuntimeCoordinator.NotifySceneLoaded();
	}
}
