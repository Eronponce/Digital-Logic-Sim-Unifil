using System.IO;
using DLS.CloudSync;
using DLS.SaveSystem;
using NUnit.Framework;

namespace DLS.EditorTests
{
	/// <summary>
	/// Valida a lógica da fila de reenvio (Outbox) sem depender de rede: a garantia
	/// central é que trabalho salvo NÃO se perde — enfileira, persiste em disco,
	/// coalesce e recupera após "reabrir o app".
	/// </summary>
	public class OutboxTests
	{
		string outboxDir;

		[SetUp]
		public void SetUp()
		{
			// perfil de teste isolado (não toca dados reais)
			SavePaths.UseCloudProfile("outbox-test-uid");
			outboxDir = Path.Combine(SavePaths.ActiveProfileDataPath, "outbox");
			if (Directory.Exists(outboxDir)) Directory.Delete(outboxDir, true);
			Outbox.ReloadForActiveProfile();
		}

		[TearDown]
		public void TearDown()
		{
			if (Directory.Exists(outboxDir)) Directory.Delete(outboxDir, true);
			SavePaths.UseOfflineProfile();
		}

		[Test]
		public void Enfileira_e_persiste_em_disco()
		{
			Outbox.EnqueueSaveChip("Proj", "AND", "and", "{\"g\":1}");
			Outbox.EnqueueSaveChip("Proj", "OR", "or", "{\"g\":2}");

			Assert.AreEqual(2, Outbox.PendingCount, "deve ter 2 itens pendentes");
			int filesOnDisk = Directory.GetFiles(outboxDir, "*.json").Length;
			Assert.AreEqual(2, filesOnDisk, "os 2 itens devem estar gravados em disco");
		}

		[Test]
		public void Coalesce_salvar_o_mesmo_chip_duas_vezes()
		{
			Outbox.EnqueueSaveChip("Proj", "AND", "and", "{\"v\":1}");
			Outbox.EnqueueSaveChip("Proj", "AND", "and", "{\"v\":2}"); // sobrescreve o pendente

			Assert.AreEqual(1, Outbox.PendingCount, "salvar o mesmo chip 2x deve deixar 1 item");
			Assert.AreEqual(1, Directory.GetFiles(outboxDir, "*.json").Length, "só 1 arquivo em disco");
		}

		[Test]
		public void Sobrevive_a_reabrir_o_app()
		{
			Outbox.EnqueueSaveChip("Proj", "AND", "and", "{}");
			Outbox.EnqueueSaveProject("Proj", "{\"p\":1}");
			Outbox.EnqueueDeleteChip("Proj", "Velho");
			Assert.AreEqual(3, Outbox.PendingCount);

			// simula fechar e reabrir o app: recarrega a fila do disco do zero
			Outbox.ReloadForActiveProfile();

			Assert.AreEqual(3, Outbox.PendingCount, "as 3 pendências devem ser recuperadas do disco");
		}

		[Test]
		public void Save_e_delete_do_mesmo_alvo_nao_coalescem()
		{
			// a ordem importa: salvar e depois apagar são operações distintas
			Outbox.EnqueueSaveChip("Proj", "AND", "and", "{}");
			Outbox.EnqueueDeleteChip("Proj", "AND");

			Assert.AreEqual(2, Outbox.PendingCount, "save e delete do mesmo chip são 2 itens em ordem");
		}
	}
}
