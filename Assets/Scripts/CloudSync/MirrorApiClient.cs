using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Cliente HTTP da API server-pg (dados no Postgres). Usa UnityHttp
	/// (UnityWebRequest) — o System.Net.Http.HttpClient não funciona no build
	/// standalone do Unity. Autentica com o token do Supabase (GoTrue) e descobre
	/// o endpoint via MirrorConfigProvider.
	/// </summary>
	public static class MirrorApiClient
	{
		// ── DTOs de resposta ─────────────────────────────────────────────────

		public class ProjectItem
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("projectName")] public string ProjectName { get; set; }
			[JsonProperty("projectData")] public string ProjectData { get; set; }
			[JsonProperty("lastModified")] public long LastModified { get; set; }
		}

		public class ChipItem
		{
			[JsonProperty("chipId")] public string ChipId { get; set; }
			[JsonProperty("chipName")] public string ChipName { get; set; }
			[JsonProperty("chipData")] public string ChipData { get; set; }
			[JsonProperty("lastModified")] public long LastModified { get; set; }
		}

		public class BundleItem : ProjectItem
		{
			[JsonProperty("chips")] public List<ChipItem> Chips { get; set; } = new();
		}

		public class TurmaItem
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("teacherName")] public string TeacherName { get; set; }
			[JsonProperty("projectName")] public string ProjectName { get; set; }
			[JsonProperty("displayName")] public string DisplayName { get; set; }
			[JsonProperty("active")] public bool Active { get; set; } = true;
		}

		class ItemsResponse<T>
		{
			[JsonProperty("items")] public List<T> Items { get; set; } = new();
		}

		class ItemResponse<T>
		{
			[JsonProperty("item")] public T Item { get; set; }
		}

		class ErrorResponse
		{
			[JsonProperty("error")] public string Error { get; set; }
		}

		// ── Núcleo HTTP ──────────────────────────────────────────────────────

		static async Task<string> GetIdTokenAsync()
		{
			var user = FirebaseAuthManager.CurrentUser;
			if (user == null)
			{
				throw new Exception("Usuário não autenticado");
			}

			// TokenAsync(false) usa cache do SDK e renova automaticamente ao expirar
			return await user.TokenAsync(false);
		}

		static async Task<UnityHttpResponse> SendOnceAsync(string method, string path, string jsonBody, bool authenticated)
		{
			string baseUrl = await MirrorConfigProvider.GetBaseUrlAsync();
			var headers = new Dictionary<string, string>();
			if (authenticated)
			{
				headers["Authorization"] = "Bearer " + await GetIdTokenAsync();
			}
			return await UnityHttp.SendAsync(method, baseUrl + path, jsonBody, headers);
		}

		static async Task<T> SendAsync<T>(string method, string path, object body = null, bool authenticated = true)
		{
			string jsonBody = body == null ? null : JsonConvert.SerializeObject(body);

			// Offline: falha rápido (a Outbox mantém o item e retenta) — não gasta
			// timeout tentando, nem re-descobre a URL à toa.
			if (Application.internetReachability == NetworkReachability.NotReachable)
			{
				throw new Exception("Sem conexão");
			}

			// Uma tentativa só. Se falhar, a exceção propaga e a Outbox retenta depois
			// (evita somar 2× o timeout quando está offline). A re-descoberta da URL
			// do túnel, se necessária, é feita pela Outbox entre ciclos.
			UnityHttpResponse response = await SendOnceAsync(method, path, jsonBody, authenticated);

			if (response.Status == 401 && authenticated)
			{
				// token pode ter acabado de expirar — força refresh e repete uma vez
				var user = FirebaseAuthManager.CurrentUser;
				if (user != null)
				{
					await user.TokenAsync(true);
				}
				response = await SendOnceAsync(method, path, jsonBody, authenticated);
			}

			return ReadResponse<T>(response, method, path);
		}

		static T ReadResponse<T>(UnityHttpResponse response, string method, string path)
		{
			string content = response.Body ?? string.Empty;
			if (!response.IsSuccess)
			{
				string detail = string.Empty;
				try
				{
					detail = JsonConvert.DeserializeObject<ErrorResponse>(content)?.Error ?? string.Empty;
				}
				catch
				{
					// corpo não-JSON
				}

				throw new Exception($"Servidor respondeu {response.Status} em {method} {path}{(string.IsNullOrEmpty(detail) ? string.Empty : $": {detail}")}");
			}

			if (typeof(T) == typeof(object) || string.IsNullOrEmpty(content))
			{
				return default;
			}

			return JsonConvert.DeserializeObject<T>(content);
		}

		static string Esc(string segment) => Uri.EscapeDataString(segment ?? string.Empty);

		// ── Perfil ───────────────────────────────────────────────────────────

		public static async Task<Dictionary<string, object>> GetUserProfileAsync(string userId)
		{
			try
			{
				var response = await SendAsync<ItemResponse<Dictionary<string, object>>>("GET", $"/api/users/{Esc(userId)}/profile");
				return response?.Item;
			}
			catch (Exception ex) when (ex.Message.Contains("404"))
			{
				return null; // perfil ainda não existe
			}
		}

		public static async Task<Dictionary<string, object>> UpsertUserProfileAsync(string userId, Dictionary<string, object> fields)
		{
			var response = await SendAsync<ItemResponse<Dictionary<string, object>>>("PUT", $"/api/users/{Esc(userId)}/profile", fields);
			return response?.Item;
		}

		// ── Projetos e chips ─────────────────────────────────────────────────

		public static Task SaveProjectAsync(string userId, string projectId, string projectName, string projectData)
		{
			return SendAsync<object>("PUT", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}", new Dictionary<string, object>
			{
				{ "projectName", projectName },
				{ "projectData", projectData },
				{ "lastModified", NowMillis() },
			});
		}

		public static Task SaveChipAsync(string userId, string projectId, string chipName, string chipLookupKey, string chipData)
		{
			return SendAsync<object>("PUT", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/{Esc(chipName)}", new Dictionary<string, object>
			{
				{ "chipName", chipName },
				{ "chipLookupKey", chipLookupKey },
				{ "chipData", chipData },
				{ "lastModified", NowMillis() },
			});
		}

		public static Task SaveBundleAsync(string userId, string projectId, object project, IEnumerable<object> chips)
		{
			return SendAsync<object>("POST", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/bundle", new Dictionary<string, object>
			{
				{ "project", project },
				{ "chips", chips },
			});
		}

		public static async Task<List<ProjectItem>> LoadAllProjectsAsync(string userId)
		{
			var response = await SendAsync<ItemsResponse<ProjectItem>>("GET", $"/api/users/{Esc(userId)}/projects/full");
			return response?.Items ?? new List<ProjectItem>();
		}

		public static async Task<List<BundleItem>> LoadAllBundlesAsync(string userId)
		{
			var response = await SendAsync<ItemsResponse<BundleItem>>("GET", $"/api/users/{Esc(userId)}/bundles");
			return response?.Items ?? new List<BundleItem>();
		}

		public static async Task<List<ChipItem>> LoadChipsAsync(string userId, string projectId)
		{
			var response = await SendAsync<ItemsResponse<ChipItem>>("GET", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/full");
			return response?.Items ?? new List<ChipItem>();
		}

		public static Task DeleteProjectAsync(string userId, string projectId)
		{
			return SendAsync<object>("DELETE", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}");
		}

		public static Task DeleteChipAsync(string userId, string projectId, string chipName)
		{
			return SendAsync<object>("DELETE", $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/{Esc(chipName)}");
		}

		public static Task DeleteAllUserDataAsync(string userId)
		{
			return SendAsync<object>("DELETE", $"/api/users/{Esc(userId)}/data");
		}

		// ── Turmas ───────────────────────────────────────────────────────────

		public static async Task<List<TurmaItem>> LoadTurmasAsync()
		{
			// authenticated:false — o endpoint é público e precisa funcionar na tela de
			// criação de conta, quando o aluno ainda não tem token.
			var response = await SendAsync<ItemsResponse<TurmaItem>>("GET", "/api/turmas?active=1", authenticated: false);
			return response?.Items ?? new List<TurmaItem>();
		}

		static long NowMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
