using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace DLS.CloudSync
{
	/// <summary>
	/// Cliente HTTP baseado em UnityWebRequest. Substitui o System.Net.Http.HttpClient,
	/// que não inicializa no build standalone do Unity (Mono) por depender do sistema
	/// de configuração .NET. UnityWebRequest funciona em qualquer build/plataforma.
	///
	/// Como UnityWebRequest exige a main thread, as requisições são enfileiradas num
	/// dispatcher que as executa no Update. Os métodos retornam Task, então o código
	/// async existente (SupabaseAuthClient, MirrorApiClient) usa await normalmente.
	/// </summary>
	public class UnityHttpResponse
	{
		public long Status { get; set; }
		public string Body { get; set; }
		public bool IsSuccess => Status >= 200 && Status < 300;
	}

	public static class UnityHttp
	{
		public static Task<UnityHttpResponse> GetAsync(string url, IDictionary<string, string> headers = null)
			=> SendAsync(UnityWebRequest.kHttpVerbGET, url, null, headers);

		public static Task<UnityHttpResponse> SendAsync(string method, string url, string jsonBody, IDictionary<string, string> headers)
		{
			var tcs = new TaskCompletionSource<UnityHttpResponse>();
			MainThreadDispatcher.Enqueue(() =>
			{
				try
				{
					var req = new UnityWebRequest(url, method)
					{
						downloadHandler = new DownloadHandlerBuffer(),
						timeout = 8, // curto: a Outbox retenta; hard timeout de 15s cobre travas de DNS
					};
					if (jsonBody != null)
					{
						req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
						req.SetRequestHeader("Content-Type", "application/json");
					}
					if (headers != null)
					{
						foreach (KeyValuePair<string, string> kv in headers)
						{
							req.SetRequestHeader(kv.Key, kv.Value);
						}
					}

					UnityWebRequestAsyncOperation op = req.SendWebRequest();
					op.completed += _ =>
					{
						// Lê TODAS as propriedades ANTES de dispor o request (acessá-las
						// após Dispose lança NullReferenceException). E garante que a Task
						// SEMPRE completa (sucesso ou erro) — nunca deixa o await pendurado.
						long status;
						string body;
						bool isConnError;
						string error;
						try
						{
							status = req.responseCode;
							body = req.downloadHandler != null ? req.downloadHandler.text : string.Empty;
							isConnError = req.result != UnityWebRequest.Result.Success && status == 0;
							error = req.error;
						}
						catch (Exception ex)
						{
							try { req.Dispose(); } catch { }
							tcs.TrySetException(new Exception($"Falha de conexão: {ex.Message}"));
							return;
						}

						try { req.Dispose(); } catch { }

						if (isConnError)
						{
							tcs.TrySetException(new Exception($"Falha de conexão: {error}"));
							return;
						}
						tcs.TrySetResult(new UnityHttpResponse { Status = status, Body = body });
					};
				}
				catch (Exception ex)
				{
					tcs.TrySetException(ex);
				}
			});
			return tcs.Task;
		}
	}

	/// <summary>
	/// Executa ações na main thread do Unity (necessário para UnityWebRequest).
	/// Criado automaticamente no boot; drena a fila a cada Update.
	/// </summary>
	public class MainThreadDispatcher : MonoBehaviour
	{
		static MainThreadDispatcher instance;
		static readonly ConcurrentQueue<Action> queue = new();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		static void Bootstrap()
		{
			if (instance != null)
			{
				return;
			}
			var go = new GameObject("[MainThreadDispatcher]");
			DontDestroyOnLoad(go);
			instance = go.AddComponent<MainThreadDispatcher>();
		}

		public static void Enqueue(Action action)
		{
			queue.Enqueue(action);
		}

		void Update()
		{
			while (queue.TryDequeue(out Action action))
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					Debug.LogError($"[MainThreadDispatcher] {ex.Message}");
				}
			}
		}
	}
}
