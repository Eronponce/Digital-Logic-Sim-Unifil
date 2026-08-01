using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Estado do salvamento na nuvem para o indicador do HUD, derivado da Outbox
	/// (fonte de verdade). Como lê a fila a cada frame, nunca "trava": reflete
	/// exatamente quantos itens faltam subir e se está offline.
	///
	///  fila vazia + subiu algo há pouco → "Salvo" (verde, some)
	///  drenando                          → "Salvando (N)..." (branco)
	///  offline com pendências            → "Sem conexão — N pendente(s)" (amarelo)
	///  erro de servidor com pendências   → "Erro: ..." (vermelho)
	/// </summary>
	public static class CloudSaveStatus
	{
		public enum State { Idle, Saving, Saved, Offline, Error }

		// momento em que a fila zerou pela última vez (para o "Salvo" sumir)
		static int lastPending;
		static float clearedAt;

		public static State Current
		{
			get
			{
				int pending = Outbox.PendingCount;

				// detecta transição "tinha pendência → zerou" para mostrar "Salvo"
				if (pending == 0 && lastPending > 0)
				{
					clearedAt = Now;
				}
				lastPending = pending;

				if (pending > 0)
				{
					if (Outbox.Offline) return State.Offline;
					if (!string.IsNullOrEmpty(Outbox.LastError) && !Outbox.Draining) return State.Error;
					return State.Saving;
				}
				return State.Idle; // "Saved" é derivado no Message/visibilidade via clearedAt
			}
		}

		public static float ClearedAt => clearedAt;

		public static string Message
		{
			get
			{
				int pending = Outbox.PendingCount;
				if (pending > 0)
				{
					if (Outbox.Offline) return pending == 1 ? "Sem conexão — 1 pendente" : $"Sem conexão — {pending} pendentes";
					if (!string.IsNullOrEmpty(Outbox.LastError) && !Outbox.Draining) return $"Erro ao salvar — tentando de novo";
					return pending == 1 ? "Salvando..." : $"Salvando ({pending})...";
				}
				return "Salvo";
			}
		}

		static float Now => Application.isPlaying ? Time.realtimeSinceStartup : 0f;

		public static void Reset() { lastPending = 0; clearedAt = 0; }
	}
}
