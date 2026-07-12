using System.Threading.Tasks;

namespace DLS.CloudSync
{
	/// <summary>
	/// Fornece a URL base da API de dados (server-pg). A descoberta remota via
	/// mirror-config.json foi desativada na migração para o Supabase; a URL agora
	/// vem de CloudConfig (que pode ser sobrescrito em runtime, se necessário).
	/// </summary>
	public static class MirrorConfigProvider
	{
		public static Task<string> GetBaseUrlAsync()
		{
			return Task.FromResult(CloudConfig.EffectiveApiBaseUrl.TrimEnd('/'));
		}

		// Mantido por compatibilidade com o retry do MirrorApiClient; no-op agora
		// que o endpoint é fixo por configuração.
		public static void InvalidateCache() { }
	}
}
