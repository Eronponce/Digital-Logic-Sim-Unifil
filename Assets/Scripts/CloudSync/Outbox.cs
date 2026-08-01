using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DLS.SaveSystem;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Fila de reenvio persistente (outbox). Cada save/delete de circuito na nuvem
	/// vira um item gravado em disco (Profiles/&lt;uid&gt;/outbox/). Um drenador reenvia
	/// os itens em ordem quando há conexão; itens que falham permanecem e são
	/// retentados. Sobrevive a fechar o app (drena no próximo login).
	///
	/// Isto torna o salvamento na nuvem confiável: gravar em disco sempre funciona;
	/// o upload é assíncrono e nunca trava o app nem perde trabalho offline.
	/// </summary>
	public static class Outbox
	{
		public enum Kind { SaveChip, SaveProject, DeleteChip, DeleteProject }

		[Serializable]
		public class Item
		{
			public long seq;
			public string kind;
			public string projectName;
			public string chipName;
			public string chipLookupKey;
			public string payload;      // chipData ou projectData serializado
			public long enqueuedAt;
		}

		// estado observável (lido pelo indicador do HUD)
		public static int PendingCount { get; private set; }
		public static bool Draining { get; private set; }
		public static bool Offline { get; private set; }
		public static string LastError { get; private set; }
		public static float LastChangeAt { get; private set; }

		static readonly List<Item> queue = new();
		static long seqCounter;
		static bool loaded;
		static string loadedForUid;

		static string OutboxDir => Path.Combine(SavePaths.ActiveProfileDataPath, "outbox");

		// ── enfileirar ─────────────────────────────────────────────────────────

		public static void EnqueueSaveChip(string projectName, string chipName, string chipLookupKey, string chipData)
			=> Enqueue(Kind.SaveChip, projectName, chipName, chipLookupKey, chipData);

		public static void EnqueueSaveProject(string projectName, string projectData)
			=> Enqueue(Kind.SaveProject, projectName, null, null, projectData);

		public static void EnqueueDeleteChip(string projectName, string chipName)
			=> Enqueue(Kind.DeleteChip, projectName, chipName, null, null);

		public static void EnqueueDeleteProject(string projectName)
			=> Enqueue(Kind.DeleteProject, projectName, null, null, null);

		static void Enqueue(Kind kind, string projectName, string chipName, string chipLookupKey, string payload)
		{
			EnsureLoaded();

			// coalescing: um novo save/delete do mesmo alvo torna obsoletos os pendentes
			// (sobe só o estado mais recente). Não coalesce save com delete — a ordem importa.
			queue.RemoveAll(it =>
				it.kind == kind.ToString() &&
				string.Equals(it.projectName, projectName, StringComparison.Ordinal) &&
				string.Equals(it.chipName ?? "", chipName ?? "", StringComparison.Ordinal) &&
				RemoveFile(it));

			var item = new Item
			{
				seq = ++seqCounter,
				kind = kind.ToString(),
				projectName = projectName,
				chipName = chipName,
				chipLookupKey = chipLookupKey,
				payload = payload,
				enqueuedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
			};
			queue.Add(item);
			WriteFile(item);
			UpdateState();

			// tenta drenar já (se online); senão fica na fila
			OutboxDrainer.RequestDrain();
		}

		// ── drenar (chamado pelo OutboxDrainer, na main thread) ─────────────────

		public static bool HasWork => queue.Count > 0;

		static bool drainInProgress; // guard de reentrância (main-thread, mas async intercala)

		public static async Task DrainAsync()
		{
			if (drainInProgress) return; // já há uma drenagem rolando — evita colisão na fila
			EnsureLoaded();
			if (queue.Count == 0) { UpdateState(); return; }
			if (!FirebaseAuthManager.IsLoggedIn) return;

			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				Offline = true;
				UpdateState();
				return;
			}

			drainInProgress = true;
			Draining = true;
			Offline = false;
			UpdateState();

			string uid = FirebaseAuthManager.UserId;

			try
			{
				// processa em ordem; para no primeiro erro (mantém ordem e tenta depois)
				while (queue.Count > 0)
				{
					Item item = queue[0];
					try
					{
						// Timeout DURO: se a requisição não responder (ex.: DNS travado
						// sem rede), não deixa o drenador preso — trata como pendente.
						Task send = Send(uid, item);
						Task done = await Task.WhenAny(send, Task.Delay(HardTimeoutMs));
						if (done != send)
						{
							Offline = true;
							LastError = "sem resposta do servidor";
							return; // retenta no próximo ciclo; a task pendente é ignorada
						}
						await send; // propaga exceção se houve

						if (queue.Count > 0 && queue[0] == item) queue.RemoveAt(0);
						RemoveFile(item);
						LastError = null;
						Offline = false;
						UpdateState();
					}
					catch (Exception ex)
					{
						// falha de rede/servidor: mantém o item e para; retenta depois.
						// Detecta offline pelo RESULTADO (não confia só no status do SO).
						bool conn = IsConnectivityError(ex) || Application.internetReachability == NetworkReachability.NotReachable;
						LastError = ex.Message;
						Offline = conn;
						// se não foi conectividade, a URL do túnel pode ter mudado →
						// re-descobre para a próxima tentativa.
						if (!conn) MirrorConfigProvider.InvalidateCache();
						return;
					}
				}

				Offline = false;
			}
			finally
			{
				Draining = false;
				drainInProgress = false;
				UpdateState();
			}
		}

		const int HardTimeoutMs = 12000;

		// É problema de conexão a menos que o SERVIDOR tenha respondido (4xx/5xx).
		// Assim qualquer falha de rede — offline, DNS travado, "insecure connection",
		// timeout, connection refused — é tratada como pendência (fica na fila e
		// mostra "sem conexão"), e não como erro definitivo.
		static bool IsConnectivityError(Exception ex)
		{
			string m = ex?.Message ?? string.Empty;
			return !m.Contains("Servidor respondeu");
		}

		static Task Send(string uid, Item item)
		{
			return item.kind switch
			{
				nameof(Kind.SaveChip) => MirrorApiClient.SaveChipAsync(uid, item.projectName, item.chipName, item.chipLookupKey, item.payload),
				nameof(Kind.SaveProject) => MirrorApiClient.SaveProjectAsync(uid, item.projectName, item.projectName, item.payload),
				nameof(Kind.DeleteChip) => MirrorApiClient.DeleteChipAsync(uid, item.projectName, item.chipName),
				nameof(Kind.DeleteProject) => MirrorApiClient.DeleteProjectAsync(uid, item.projectName),
				_ => Task.CompletedTask,
			};
		}

		// ── persistência ────────────────────────────────────────────────────────

		public static void ReloadForActiveProfile()
		{
			loaded = false;
			queue.Clear();
			EnsureLoaded();
			UpdateState();
			if (queue.Count > 0) OutboxDrainer.RequestDrain();
		}

		static void EnsureLoaded()
		{
			string uid = FirebaseAuthManager.UserId ?? "offline";
			if (loaded && loadedForUid == uid) return;

			queue.Clear();
			seqCounter = 0;
			try
			{
				if (Directory.Exists(OutboxDir))
				{
					foreach (string file in Directory.GetFiles(OutboxDir, "*.json"))
					{
						try
						{
							Item it = JsonUtility.FromJson<Item>(File.ReadAllText(file));
							if (it != null && it.seq > 0) { queue.Add(it); seqCounter = Math.Max(seqCounter, it.seq); }
						}
						catch { /* item corrompido — ignora */ }
					}
					queue.Sort((a, b) => a.seq.CompareTo(b.seq));
				}
			}
			catch (Exception ex) { Debug.LogWarning($"[Outbox] falha ao carregar: {ex.Message}"); }

			loaded = true;
			loadedForUid = uid;
		}

		static void WriteFile(Item item)
		{
			try
			{
				Directory.CreateDirectory(OutboxDir);
				File.WriteAllText(Path.Combine(OutboxDir, $"{item.seq:D6}.json"), JsonUtility.ToJson(item));
			}
			catch (Exception ex) { Debug.LogWarning($"[Outbox] falha ao gravar item: {ex.Message}"); }
		}

		static bool RemoveFile(Item item)
		{
			try
			{
				string f = Path.Combine(OutboxDir, $"{item.seq:D6}.json");
				if (File.Exists(f)) File.Delete(f);
			}
			catch { /* ok */ }
			return true;
		}

		static void UpdateState()
		{
			PendingCount = queue.Count;
			LastChangeAt = Application.isPlaying ? Time.realtimeSinceStartup : 0f;
		}
	}
}
