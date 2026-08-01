using System;
using System.Collections;
using System.Threading.Tasks;
using DLS.CloudSync;
using DLS.SaveSystem;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace DLS.PlayTests
{
	/// <summary>
	/// Simula a QUEDA de conexão sem depender do Wi-Fi: aponta o app para um endereço
	/// que não responde (equivale a offline do ponto de vista do HTTP) e roda o fluxo
	/// real da Outbox em PlayMode (com o loop de jogo ativo, então o MainThreadDispatcher
	/// e as requisições UnityWebRequest funcionam de verdade).
	///
	/// Valida: (1) drenar offline NÃO trava; (2) marca offline/erro e mantém o item na
	/// fila; (3) ao "voltar a conexão", reenvia sozinho e zera a fila.
	/// </summary>
	public class OutboxOfflinePlayTest
	{
		const string Email = "teste@teste.com";
		const string Password = "teste123456";
		const string DeadUrl = "https://10.255.255.1"; // IP não-roteável, HTTPS → timeout real (rede "morta")

		static IEnumerator Await(Task t)
		{
			while (!t.IsCompleted) yield return null;
			if (t.IsFaulted) throw t.Exception?.InnerException ?? t.Exception;
		}

		[UnityTest]
		public IEnumerator Offline_nao_trava_e_reenvia_ao_voltar()
		{
			// 0. descobre a URL real e loga (precisa de conexão boa aqui)
			yield return Await(MirrorConfigProvider.EnsureDiscoveredAsync());
			string goodUrl = CloudConfig.EffectiveApiBaseUrl;
			Assert.IsFalse(string.IsNullOrEmpty(goodUrl), "não descobriu a URL do servidor");

			Task<AuthUser> login = SupabaseAuthClient.SignInWithPasswordAsync(Email, Password);
			yield return Await(login);
			AuthUser user = login.Result;
			Assert.IsNotNull(user, "login falhou");
			string uid = user.UserId;
			SavePaths.UseCloudProfile(uid);
			Outbox.ReloadForActiveProfile();

			// 1. "desliga o Wi-Fi": aponta para um endereço morto
			CloudConfig.EffectiveApiBaseUrl = DeadUrl;
			Outbox.EnqueueSaveChip("PlayTestProj", "AND_playtest", "and_playtest", "{\"g\":\"and\"}");
			Assert.AreEqual(1, Outbox.PendingCount, "item deve entrar na fila");

			// 2. drena OFFLINE — não pode travar; mede o tempo
			float t0 = Time.realtimeSinceStartup;
			Task drainOffline = Outbox.DrainAsync();
			yield return Await(drainOffline);
			float elapsed = Time.realtimeSinceStartup - t0;

			Debug.Log($"[PlayTest] drain offline levou {elapsed:F1}s; pending={Outbox.PendingCount} offline={Outbox.Offline} err={Outbox.LastError}");
			Assert.Less(elapsed, 20f, "drenar offline não pode travar (>20s)");
			Assert.AreEqual(1, Outbox.PendingCount, "item deve PERMANECER na fila após falha offline");
			Assert.IsTrue(Outbox.Offline, "deve marcar OFFLINE (mostra amarelo), não erro de servidor");

			// 3. "religa o Wi-Fi": volta a URL boa; a fila deve drenar sozinha
			CloudConfig.EffectiveApiBaseUrl = goodUrl;
			OutboxDrainer.RequestDrain();

			// aguarda a fila zerar por polling (o drenador automático processa)
			float deadline = Time.realtimeSinceStartup + 25f;
			while (Outbox.PendingCount > 0 && Time.realtimeSinceStartup < deadline)
			{
				CloudConfig.EffectiveApiBaseUrl = goodUrl; // mantém a URL boa (EnsureDiscovered pode reescrever)
				OutboxDrainer.RequestDrain();
				yield return new WaitForSeconds(1f);
			}

			Debug.Log($"[PlayTest] drain online: pending={Outbox.PendingCount} err={Outbox.LastError}");
			Assert.AreEqual(0, Outbox.PendingCount, "após voltar a conexão, a fila deve zerar (reenviou)");
			Assert.IsFalse(Outbox.Offline, "não deve mais estar offline");

			// 4. cleanup: remove o projeto de teste do servidor (tolerante a 404)
			Task delProj = MirrorApiClient.DeleteProjectAsync(uid, "PlayTestProj");
			while (!delProj.IsCompleted) yield return null; // ignora exceção (404 se não existir)

			SavePaths.UseOfflineProfile();
		}
	}
}
