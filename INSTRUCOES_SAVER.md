# INSTRUÇÕES: Modificar Saver.cs

## Arquivo: `Assets/Scripts/SaveSystem/Saver.cs`

### MODIFICAÇÃO 1: Método SaveChip (linha ~46)

**ENCONTRE:**
```csharp
public static void SaveChip(ChipDescription chipDescription, string projectName)
{
	string serializedDescription = CreateSerializedChipDescription(chipDescription);
	WriteToFile(serializedDescription, GetChipFilePath(chipDescription.Name, projectName));
}
```

**SUBSTITUA POR:**
```csharp
public static void SaveChip(ChipDescription chipDescription, string projectName)
{
	// Salvamento local
	string serializedDescription = CreateSerializedChipDescription(chipDescription);
	WriteToFile(serializedDescription, GetChipFilePath(chipDescription.Name, projectName));

	// Sincronização cloud
	SaverCloudExtension.SyncChipToCloud(chipDescription, projectName);
}
```

---

## ✅ PRONTO!

Agora o sistema salva:
1. **Localmente** (como antes) - funciona offline
2. **No Firebase** (automático) - se estiver logado

Se não estiver logado, funciona normalmente em modo offline.
