using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Usuário autenticado (substitui o FirebaseUser). TokenAsync devolve o
	/// access_token atual do GoTrue, renovando-o via refresh_token quando expira.
	/// </summary>
	public class AuthUser
	{
		public string UserId { get; internal set; }
		public string Email { get; internal set; }
		public string DisplayName { get; internal set; }
		public bool IsEmailVerified { get; internal set; }

		public Task<string> TokenAsync(bool forceRefresh) => SupabaseAuthClient.GetAccessTokenAsync(forceRefresh);
	}

	/// <summary>
	/// Cliente REST do Supabase Auth (GoTrue), via Kong gateway. Faz signup, login
	/// por senha, refresh, recover (reset de senha) e update de usuário. Persiste a
	/// sessão em PlayerPrefs para "manter logado".
	/// </summary>
	public static class SupabaseAuthClient
	{
		const string SessionKey = "DLS_SupabaseSession";

		static string accessToken;
		static string refreshToken;
		static long expiresAtUnix;
		static AuthUser currentUser;

		public static AuthUser CurrentUser => currentUser;

		static string Base => CloudConfig.EffectiveSupabaseUrl.TrimEnd('/') + "/auth/v1";
		static string AnonKey => CloudConfig.EffectiveSupabaseAnonKey;

		// ── DTOs ──────────────────────────────────────────────────────────────

		// Os corpos de request usam classes explícitas (e não tipos anônimos):
		// tipos anônimos ficam fora de qualquer namespace, então o link.xml não os
		// preserva e o managed stripping do build standalone remove os getters —
		// o Newtonsoft serializaria "{}" e o GoTrue recusaria o request.

		class CredentialsBody
		{
			[JsonProperty("email")] public string Email { get; set; }
			[JsonProperty("password")] public string Password { get; set; }
			[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)] public UserMetadataBody Data { get; set; }
		}

		class UserMetadataBody
		{
			[JsonProperty("displayName")] public string DisplayName { get; set; }
		}

		class EmailBody
		{
			[JsonProperty("email")] public string Email { get; set; }
		}

		class RefreshTokenBody
		{
			[JsonProperty("refresh_token")] public string RefreshToken { get; set; }
		}

		class UpdateUserBody
		{
			[JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)] public string Password { get; set; }
			[JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)] public UserMetadataBody Data { get; set; }
		}

		class TokenResponse
		{
			[JsonProperty("access_token")] public string AccessToken { get; set; }
			[JsonProperty("refresh_token")] public string RefreshToken { get; set; }
			[JsonProperty("expires_in")] public long ExpiresIn { get; set; }
			[JsonProperty("user")] public GoTrueUser User { get; set; }
		}

		class GoTrueUser
		{
			[JsonProperty("id")] public string Id { get; set; }
			[JsonProperty("email")] public string Email { get; set; }
			[JsonProperty("email_confirmed_at")] public string EmailConfirmedAt { get; set; }
			[JsonProperty("user_metadata")] public UserMetadata UserMetadata { get; set; }
		}

		class UserMetadata
		{
			[JsonProperty("displayName")] public string DisplayName { get; set; }
			[JsonProperty("name")] public string Name { get; set; }
		}

		class PersistedSession
		{
			public string accessToken;
			public string refreshToken;
			public long expiresAtUnix;
			public string userId;
			public string email;
			public string displayName;
			public bool emailVerified;
		}

		// ── API pública ───────────────────────────────────────────────────────

		public static async Task<AuthUser> SignInWithPasswordAsync(string email, string password)
		{
			await MirrorConfigProvider.EnsureDiscoveredAsync();
			var body = new CredentialsBody { Email = email, Password = password };
			var resp = await PostAsync("/token?grant_type=password", body);
			return ApplyToken(resp);
		}

		public static async Task<AuthUser> SignUpAsync(string email, string password, string displayName)
		{
			await MirrorConfigProvider.EnsureDiscoveredAsync();
			var body = new CredentialsBody
			{
				Email = email,
				Password = password,
				Data = string.IsNullOrWhiteSpace(displayName) ? null : new UserMetadataBody { DisplayName = displayName },
			};
			var resp = await PostAsync("/signup", body);
			// Com autoconfirm, o signup já devolve sessão; senão, faz login em seguida.
			if (!string.IsNullOrEmpty(resp?.AccessToken))
			{
				return ApplyToken(resp);
			}
			return await SignInWithPasswordAsync(email, password);
		}

		public static async Task SendPasswordResetAsync(string email)
		{
			await PostRawAsync("/recover", new EmailBody { Email = email });
		}

		public static async Task UpdatePasswordAsync(string newPassword)
		{
			await PutUserAsync(new UpdateUserBody { Password = newPassword });
		}

		public static async Task UpdateDisplayNameAsync(string displayName)
		{
			await PutUserAsync(new UpdateUserBody { Data = new UserMetadataBody { DisplayName = displayName } });
			if (currentUser != null) currentUser.DisplayName = displayName;
		}

		public static void SignOut()
		{
			accessToken = null;
			refreshToken = null;
			expiresAtUnix = 0;
			currentUser = null;
			PlayerPrefs.DeleteKey(SessionKey);
			PlayerPrefs.Save();
		}

		/// <summary>Restaura a sessão salva (para "manter logado"). Retorna o usuário ou null.</summary>
		public static async Task<AuthUser> TryRestoreSessionAsync()
		{
			string json = PlayerPrefs.GetString(SessionKey, string.Empty);
			if (string.IsNullOrEmpty(json)) return null;
			PersistedSession s;
			try { s = JsonConvert.DeserializeObject<PersistedSession>(json); }
			catch { return null; }
			if (s == null || string.IsNullOrEmpty(s.refreshToken)) return null;

			accessToken = s.accessToken;
			refreshToken = s.refreshToken;
			expiresAtUnix = s.expiresAtUnix;
			currentUser = new AuthUser { UserId = s.userId, Email = s.email, DisplayName = s.displayName, IsEmailVerified = s.emailVerified };

			// valida/renova
			try
			{
				await GetAccessTokenAsync(false);
				return currentUser;
			}
			catch
			{
				SignOut();
				return null;
			}
		}

		/// <summary>Token de acesso válido; renova via refresh_token se expirado ou forçado.</summary>
		public static async Task<string> GetAccessTokenAsync(bool forceRefresh)
		{
			long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
			bool expired = now >= (expiresAtUnix - 60); // margem de 60s
			if (!forceRefresh && !expired && !string.IsNullOrEmpty(accessToken))
			{
				return accessToken;
			}
			if (string.IsNullOrEmpty(refreshToken))
			{
				throw new Exception("Sessão expirada — faça login novamente.");
			}
			var resp = await PostAsync("/token?grant_type=refresh_token", new RefreshTokenBody { RefreshToken = refreshToken });
			ApplyToken(resp);
			return accessToken;
		}

		// ── internos ──────────────────────────────────────────────────────────

		static AuthUser ApplyToken(TokenResponse resp)
		{
			if (resp == null || string.IsNullOrEmpty(resp.AccessToken))
			{
				throw new Exception("Resposta de autenticação inválida.");
			}
			accessToken = resp.AccessToken;
			refreshToken = resp.RefreshToken ?? refreshToken;
			expiresAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + (resp.ExpiresIn > 0 ? resp.ExpiresIn : 3600);

			var u = resp.User;
			if (u != null)
			{
				currentUser = new AuthUser
				{
					UserId = u.Id,
					Email = u.Email,
					DisplayName = u.UserMetadata?.DisplayName ?? u.UserMetadata?.Name,
					IsEmailVerified = !string.IsNullOrEmpty(u.EmailConfirmedAt),
				};
			}
			PersistSession();
			return currentUser;
		}

		static void PersistSession()
		{
			if (currentUser == null) return;
			var s = new PersistedSession
			{
				accessToken = accessToken,
				refreshToken = refreshToken,
				expiresAtUnix = expiresAtUnix,
				userId = currentUser.UserId,
				email = currentUser.Email,
				displayName = currentUser.DisplayName,
				emailVerified = currentUser.IsEmailVerified,
			};
			PlayerPrefs.SetString(SessionKey, JsonConvert.SerializeObject(s));
			PlayerPrefs.Save();
		}

		static async Task<TokenResponse> PostAsync(string path, object body)
		{
			string content = await PostRawAsync(path, body);
			return JsonConvert.DeserializeObject<TokenResponse>(content);
		}

		static async Task<string> PostRawAsync(string path, object body)
		{
			var headers = new Dictionary<string, string> { { "apikey", AnonKey } };
			UnityHttpResponse resp = await UnityHttp.SendAsync("POST", Base + path, JsonConvert.SerializeObject(body), headers);
			if (!resp.IsSuccess)
			{
				throw new AuthException((int)resp.Status, ExtractError(resp.Body));
			}
			return resp.Body;
		}

		static async Task PutUserAsync(object body)
		{
			string token = await GetAccessTokenAsync(false);
			var headers = new Dictionary<string, string>
			{
				{ "apikey", AnonKey },
				{ "Authorization", "Bearer " + token },
			};
			UnityHttpResponse resp = await UnityHttp.SendAsync("PUT", Base + "/user", JsonConvert.SerializeObject(body), headers);
			if (!resp.IsSuccess)
			{
				throw new AuthException((int)resp.Status, ExtractError(resp.Body));
			}
		}

		static string ExtractError(string content)
		{
			try
			{
				var obj = JsonConvert.DeserializeObject<ErrorBody>(content);
				return obj?.ErrorDescription ?? obj?.Msg ?? obj?.Error ?? content;
			}
			catch { return content; }
		}

		class ErrorBody
		{
			[JsonProperty("error")] public string Error { get; set; }
			[JsonProperty("error_description")] public string ErrorDescription { get; set; }
			[JsonProperty("msg")] public string Msg { get; set; }
		}
	}

	public class AuthException : Exception
	{
		public int StatusCode { get; }
		public AuthException(int statusCode, string message) : base(message) => StatusCode = statusCode;
	}
}
