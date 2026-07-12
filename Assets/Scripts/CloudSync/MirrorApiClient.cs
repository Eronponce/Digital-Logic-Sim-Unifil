using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Cliente HTTP do mirror server (substitui o SDK do Firestore).
	/// Autentica com o idToken do Firebase Auth (que continua em uso) e descobre
	/// o endpoint via MirrorConfigProvider.
	///
	/// Threading: todos os métodos são async Task e devem ser aguardados a partir
	/// da main thread — as continuações voltam pela UnitySynchronizationContext,
	/// então callbacks podem tocar objetos Unity. Não usar ConfigureAwait(false).
	/// </summary>
	public static class MirrorApiClient
	{
		static readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(30) };

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

		static async Task<HttpResponseMessage> SendOnceAsync(HttpMethod method, string path, string jsonBody, bool authenticated)
		{
			string baseUrl = await MirrorConfigProvider.GetBaseUrlAsync();
			using HttpRequestMessage request = new(method, baseUrl + path);
			if (authenticated)
			{
				request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", await GetIdTokenAsync());
			}

			if (jsonBody != null)
			{
				request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
			}

			return await http.SendAsync(request);
		}

		static async Task<T> SendAsync<T>(HttpMethod method, string path, object body = null, bool authenticated = true)
		{
			string jsonBody = body == null ? null : JsonConvert.SerializeObject(body);

			HttpResponseMessage response;
			try
			{
				response = await SendOnceAsync(method, path, jsonBody, authenticated);
			}
			catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
			{
				// URL do tunnel pode ter mudado — re-busca a config e tenta uma vez mais
				MirrorConfigProvider.InvalidateCache();
				response = await SendOnceAsync(method, path, jsonBody, authenticated);
			}

			using (response)
			{
				if (response.StatusCode == HttpStatusCode.Unauthorized && authenticated)
				{
					// token pode ter acabado de expirar — força refresh e repete uma vez
					response.Dispose();
					var user = FirebaseAuthManager.CurrentUser;
					if (user != null)
					{
						await user.TokenAsync(true);
					}

					using HttpResponseMessage retry = await SendOnceAsync(method, path, jsonBody, authenticated);
					return await ReadResponseAsync<T>(retry, method, path);
				}

				return await ReadResponseAsync<T>(response, method, path);
			}
		}

		static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, HttpMethod method, string path)
		{
			string content = response.Content == null ? string.Empty : await response.Content.ReadAsStringAsync();
			if (!response.IsSuccessStatusCode)
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

				throw new Exception($"Servidor respondeu {(int)response.StatusCode} em {method} {path}{(string.IsNullOrEmpty(detail) ? string.Empty : $": {detail}")}");
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
				var response = await SendAsync<ItemResponse<Dictionary<string, object>>>(HttpMethod.Get, $"/api/users/{Esc(userId)}/profile");
				return response?.Item;
			}
			catch (Exception ex) when (ex.Message.Contains("404"))
			{
				return null; // perfil ainda não existe
			}
		}

		public static async Task<Dictionary<string, object>> UpsertUserProfileAsync(string userId, Dictionary<string, object> fields)
		{
			var response = await SendAsync<ItemResponse<Dictionary<string, object>>>(HttpMethod.Put, $"/api/users/{Esc(userId)}/profile", fields);
			return response?.Item;
		}

		// ── Projetos e chips ─────────────────────────────────────────────────

		public static Task SaveProjectAsync(string userId, string projectId, string projectName, string projectData)
		{
			return SendAsync<object>(HttpMethod.Put, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}", new Dictionary<string, object>
			{
				{ "projectName", projectName },
				{ "projectData", projectData },
				{ "lastModified", NowMillis() },
			});
		}

		public static Task SaveChipAsync(string userId, string projectId, string chipName, string chipLookupKey, string chipData)
		{
			return SendAsync<object>(HttpMethod.Put, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/{Esc(chipName)}", new Dictionary<string, object>
			{
				{ "chipName", chipName },
				{ "chipLookupKey", chipLookupKey },
				{ "chipData", chipData },
				{ "lastModified", NowMillis() },
			});
		}

		public static Task SaveBundleAsync(string userId, string projectId, object project, IEnumerable<object> chips)
		{
			return SendAsync<object>(HttpMethod.Post, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/bundle", new Dictionary<string, object>
			{
				{ "project", project },
				{ "chips", chips },
			});
		}

		public static async Task<List<ProjectItem>> LoadAllProjectsAsync(string userId)
		{
			var response = await SendAsync<ItemsResponse<ProjectItem>>(HttpMethod.Get, $"/api/users/{Esc(userId)}/projects/full");
			return response?.Items ?? new List<ProjectItem>();
		}

		public static async Task<List<BundleItem>> LoadAllBundlesAsync(string userId)
		{
			var response = await SendAsync<ItemsResponse<BundleItem>>(HttpMethod.Get, $"/api/users/{Esc(userId)}/bundles");
			return response?.Items ?? new List<BundleItem>();
		}

		public static async Task<List<ChipItem>> LoadChipsAsync(string userId, string projectId)
		{
			var response = await SendAsync<ItemsResponse<ChipItem>>(HttpMethod.Get, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/full");
			return response?.Items ?? new List<ChipItem>();
		}

		public static Task DeleteProjectAsync(string userId, string projectId)
		{
			return SendAsync<object>(HttpMethod.Delete, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}");
		}

		public static Task DeleteChipAsync(string userId, string projectId, string chipName)
		{
			return SendAsync<object>(HttpMethod.Delete, $"/api/users/{Esc(userId)}/projects/{Esc(projectId)}/chips/{Esc(chipName)}");
		}

		public static Task DeleteAllUserDataAsync(string userId)
		{
			return SendAsync<object>(HttpMethod.Delete, $"/api/users/{Esc(userId)}/data");
		}

		// ── Turmas ───────────────────────────────────────────────────────────

		public static async Task<List<TurmaItem>> LoadTurmasAsync()
		{
			var response = await SendAsync<ItemsResponse<TurmaItem>>(HttpMethod.Get, "/api/turmas?active=1");
			return response?.Items ?? new List<TurmaItem>();
		}

		static long NowMillis() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
	}
}
