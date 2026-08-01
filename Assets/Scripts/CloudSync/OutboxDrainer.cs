using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Processa a Outbox periodicamente e sob demanda. Roda na main thread (o
	/// MirrorApiClient/UnityHttp exigem isso). Tenta drenar a cada poucos segundos
	/// quando há itens pendentes e conexão; reage também à volta da conectividade.
	/// Criado automaticamente no boot.
	/// </summary>
	public class OutboxDrainer : MonoBehaviour
	{
		static OutboxDrainer instance;
		static bool drainRequested;

		const float idlePollSeconds = 5f;    // intervalo entre tentativas quando há pendências
		float nextPollAt;
		bool draining;
		NetworkReachability lastReachability;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Bootstrap()
		{
			if (instance != null) return;
			var go = new GameObject("[OutboxDrainer]");
			DontDestroyOnLoad(go);
			instance = go.AddComponent<OutboxDrainer>();
		}

		/// <summary>Pede uma tentativa de drenagem no próximo frame.</summary>
		public static void RequestDrain() => drainRequested = true;

		void Start()
		{
			lastReachability = Application.internetReachability;
		}

		async void Update()
		{
			if (draining) return;

			// dispara quando: pediram, chegou a hora do poll, ou a conexão acabou de voltar
			bool reconnected = lastReachability == NetworkReachability.NotReachable
			                   && Application.internetReachability != NetworkReachability.NotReachable;
			lastReachability = Application.internetReachability;

			bool due = drainRequested || reconnected || (Outbox.HasWork && Time.realtimeSinceStartup >= nextPollAt);
			if (!due) return;

			drainRequested = false;
			nextPollAt = Time.realtimeSinceStartup + idlePollSeconds;

			if (!Outbox.HasWork || !FirebaseAuthManager.IsLoggedIn) return;

			draining = true;
			try
			{
				await Outbox.DrainAsync();
			}
			finally
			{
				draining = false;
			}
		}
	}
}
