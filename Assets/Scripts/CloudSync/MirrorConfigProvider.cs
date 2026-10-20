using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace DLS.CloudSync
{
	/// <summary>
	/// Descobre a URL atual do servidor. Como o túnel Cloudflare é gratuito e sua
	/// URL pode mudar, o app consulta um arquivo no GitHub (endereço permanente,
	/// liberado em redes escolares) que sempre aponta para a URL de hoje. A URL é
	/// cacheada em PlayerPrefs (uso normal = sem rede); re-descobre em falha.
	/// Ao descobrir, atualiza CloudConfig.Effective* (Supabase + API compartilham
	/// a mesma URL — o proxy no servidor separa /auth de /api).
	/// </summary>
	public static class MirrorConfigProvider
	{
		const string ConfigApiUrl = "https://api.github.com/repos/Eronponce/logisim-config/contents/config.json";
		const string PrefKey = "logisim_base_url";

		static string discovered;
		static Task<string> inFlight;

		/// <summary>URL base da API (após garantir a descoberta).</summary>
		public static async Task<string> GetBaseUrlAsync()
		{
			await EnsureDiscoveredAsync();
			return CloudConfig.EffectiveApiBaseUrl.TrimEnd('/');
		}

		/// <summary>
		/// Garante que CloudConfig.Effective* aponte para a URL atual. Usa cache
		/// local primeiro; busca no GitHub para atualizar. Chamada no boot e antes
		/// de autenticar. Deduplica chamadas concorrentes.
		/// </summary>
		public static Task<string> EnsureDiscoveredAsync()
		{
			if (!string.IsNullOrEmpty(discovered))
			{
				return Task.FromResult(discovered);
			}
			inFlight ??= DiscoverAsync();
			return inFlight;
		}

		/// <summary>Invalida o cache e força nova descoberta (chamado em falha).</summary>
		public static void InvalidateCache()
		{
			discovered = null;
			inFlight = null;
		}

		/// <summary>
		/// Roda `action` uma vez; se falhar por falha de conexão (UnityHttp lança
		/// "Falha de conexão: ..." quando a requisição nem chega a receber resposta
		/// — exatamente o caso de uma URL de túnel cacheada que morreu enquanto o
		/// app ficava aberto), invalida o cache e busca a URL atual de novo ANTES de
		/// tentar mais uma vez. Sem isto, login/carregar projeto ficavam presos pra
		/// sempre numa URL morta — só a Outbox (retry de salvamento em 2º plano)
		/// redescobria, e só quando o erro batia como "sem internet" (não como "URL
		/// errada", que é o caso real de um túnel caído). Uma tentativa extra só,
		/// nunca mais que isso — se a redescoberta não resolver, o erro propaga.
		/// </summary>
		public static async Task<T> WithRediscoveryAsync<T>(Func<Task<T>> action)
		{
			try
			{
				return await action();
			}
			catch (Exception ex) when (ex.Message.StartsWith("Falha de conexão"))
			{
				InvalidateCache();
				await EnsureDiscoveredAsync();
				return await action();
			}
		}

		static async Task<string> DiscoverAsync()
		{
			// 1. cache local — aplica de imediato para não bloquear o boot
			string cached = PlayerPrefs.GetString(PrefKey, string.Empty);
			if (!string.IsNullOrEmpty(cached))
			{
				Apply(cached);
			}

			// 2. busca a URL fresca no GitHub
			try
			{
				var headers = new Dictionary<string, string>
				{
					{ "Accept", "application/vnd.github.raw+json" },
					{ "User-Agent", "dls-app" },
				};
				UnityHttpResponse resp = await UnityHttp.GetAsync(ConfigApiUrl, headers);
				string json = resp.IsSuccess ? resp.Body : string.Empty;
				Match m = Regex.Match(json, "\"apiBaseUrl\"\\s*:\\s*\"([^\"]+)\"");
				if (m.Success)
				{
					string url = m.Groups[1].Value.TrimEnd('/');
					Apply(url);
					discovered = url;
					PlayerPrefs.SetString(PrefKey, url);
					PlayerPrefs.Save();
					Debug.Log($"[MirrorConfig] endpoint descoberto: {url}");
					inFlight = null;
					return url;
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[MirrorConfig] falha ao descobrir (usando cache/config): {ex.Message}");
			}

			inFlight = null;
			// 3. fallback: cache, senão o valor de CloudConfig
			discovered = !string.IsNullOrEmpty(cached) ? cached : CloudConfig.EffectiveApiBaseUrl.TrimEnd('/');
			return discovered;
		}

		static void Apply(string url)
		{
			CloudConfig.EffectiveApiBaseUrl = url;
			CloudConfig.EffectiveSupabaseUrl = url;
		}
	}
}
