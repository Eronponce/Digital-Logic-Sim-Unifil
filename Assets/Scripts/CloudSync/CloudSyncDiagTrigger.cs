using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Temporary: press F12 in Play Mode to dump cloud sync state to Desktop/cloud_sync_diag.log
	/// Auto-injects itself at runtime — no scene setup needed. Remove after investigation.
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
