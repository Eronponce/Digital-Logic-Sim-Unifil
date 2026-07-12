namespace DLS.CloudSync
{
	/// <summary>
	/// Endpoints da infraestrutura em nuvem (Supabase self-hosted + API server-pg).
	/// Substitui a configuração do Firebase. Em produção com alunos externos, estas
	/// URLs devem apontar para um domínio/túnel público estável; hoje usam o IP
	/// Tailscale do servidor para desenvolvimento/laboratório.
	///
	/// Pode ser sobrescrito em runtime pelo MirrorConfigProvider (mirror-config.json).
	/// </summary>
	public static class CloudConfig
	{
		// Supabase (GoTrue auth via Kong gateway)
		public const string SupabaseUrl = "https://took-belts-pursue-pens.trycloudflare.com";
		public const string SupabaseAnonKey =
			"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJyb2xlIjoiYW5vbiIsImlzcyI6InN1cGFiYXNlIiwiaWF0IjoxNzgzNzg2MjI2LCJleHAiOjIwOTkxNDYyMjZ9._BDdcMGcIAI4WmTyT1J88SSyjUn2wOcB5IUagNWsU9c";

		// API de dados (server-pg) — porta 3002 até o cutover do mirror antigo
		public const string ApiBaseUrl = "https://approximately-glasses-root-oclc.trycloudflare.com";

		// valores efetivos (podem ser sobrescritos pela config remota)
		public static string EffectiveSupabaseUrl { get; set; } = SupabaseUrl;
		public static string EffectiveSupabaseAnonKey { get; set; } = SupabaseAnonKey;
		public static string EffectiveApiBaseUrl { get; set; } = ApiBaseUrl;
	}
}
