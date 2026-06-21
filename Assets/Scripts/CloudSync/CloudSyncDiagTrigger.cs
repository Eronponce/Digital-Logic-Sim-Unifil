#if UNITY_EDITOR || DEVELOPMENT_BUILD
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Press F12 in Play Mode to dump cloud sync state to Desktop/cloud_sync_diag.log.
	/// Active only in Editor and Development Builds — stripped from release builds.
	/// Auto-injects itself at runtime; no scene setup needed.
	/// </summary>
	public class CloudSyncDiagTrigger : MonoBehaviour
	{
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		static void AutoInject()
		{
			GameObject go = new GameObject("[CloudSyncDiagTrigger]");
			go.AddComponent<CloudSyncDiagTrigger>();
			DontDestroyOnLoad(go);
			Debug.Log("[CloudDiag] DiagTrigger active — press F12 any time to dump state to Desktop/cloud_sync_diag.log");
		}

		void Update()
		{
			if (Input.GetKeyDown(KeyCode.F12))
			{
				Debug.Log("[CloudDiag] F12 — dumping state...");
				CloudSyncDiagnostics.DumpCurrentState();
				Debug.Log("[CloudDiag] Done. Desktop/cloud_sync_diag.log");
			}
		}
	}
}
#endif
