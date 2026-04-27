# CONTEXTO DO REPOSITORIO: DIGITAL LOGIC SIMULATOR

> Atualizacao 2026-04-08: este arquivo continua util como visao historica, mas nao e mais a fonte canonica do repositorio. Para o estado atual, consulte `AGENTS.md` e os arquivos em `docs/`.

## INFORMAÇÕES GERAIS

**Nome do Projeto:** Digital Logic Simulator
**Autor Original:** Sebastian Lague
**Tipo:** Simulador de Circuitos Lógicos Digitais
**Engine:** Unity (C#)
**Versão Atual:** 2.1.6
**Última Atualização:** Outubro 2025
**Repositório Original:** https://github.com/SebLague/Digital-Logic-Sim
**YouTube Series:** [Exploring How Computers Work](https://www.youtube.com/playlist?list=PLFt_AvWsXl0dPhqVsKt1Ni_46ARyiCGSq)

### Descrição
Um simulador minimalista de lógica digital que permite criar, simular e visualizar circuitos lógicos desde portas básicas (NAND) até sistemas completos como processadores customizados, displays e memória.

---

## ESTRUTURA DO PROJETO

### Organização de Pastas

```
Digital-Logic-Sim-Unifil/
├── Assets/
│   ├── Build/                  # Recursos para build
│   ├── Dev/                    # Ferramentas de desenvolvimento
│   │   ├── SaveRefac/         # Refatoração do sistema de salvamento
│   │   └── VidTools/          # Ferramentas para gravação de vídeos
│   └── Scripts/               # TODO O CÓDIGO-FONTE
│       ├── Description/       # Sistema de tipos e serialização
│       ├── Game/             # Lógica principal da aplicação
│       ├── Graphics/         # Sistema de renderização e UI
│       ├── SaveSystem/       # Salvamento/carregamento
│       ├── Simulation/       # Motor de simulação
│       └── Seb/             # Framework customizado
│
├── Library/                   # Cache do Unity (não versionado)
├── Packages/                  # Pacotes Unity
├── ProjectSettings/          # Configurações do Unity
├── TestData/                 # Dados de teste
└── UserSettings/            # Configurações locais do usuário
```

### Assets/Scripts - Módulos Principais

```
Assets/Scripts/
│
├── Description/                          [DADOS E SERIALIZAÇÃO]
│   ├── Helpers/
│   │   └── ChipTypeHelper.cs            # Utilitários para tipos de chips
│   ├── Serialization/
│   │   ├── Newtonsoft/                  # Biblioteca JSON
│   │   ├── Serializer.cs                # Serializador principal
│   │   └── UnsavedChangeDetector.cs     # Detecção de mudanças
│   └── Types/
│       ├── AppSettings.cs               # Configurações globais
│       ├── ChipDescription.cs           # Descrição de um chip
│       ├── ProjectDescription.cs        # Metadados do projeto
│       └── SubTypes/
│           ├── ChipTypes.cs             # Enum de tipos de chips
│           ├── DisplayDescription.cs    # Configuração de displays
│           ├── PinAddress.cs            # Endereçamento de pinos
│           ├── PinDescription.cs        # Descrição de pino
│           ├── SubChipDescription.cs    # Chip dentro de chip
│           └── WireDescription.cs       # Descrição de fio/conexão
│
├── Game/                                [LÓGICA PRINCIPAL]
│   ├── Audio/
│   │   ├── AudioState.cs                # Estado global de áudio
│   │   └── AudioUnity.cs                # Interface Unity Audio
│   ├── Elements/                        # Instâncias de elementos
│   │   ├── DevPinInstance.cs           # Pino de I/O do chip em edição
│   │   ├── DisplayInstance.cs          # Display (7-seg, RGB, etc)
│   │   ├── PinInstance.cs              # Pino genérico
│   │   ├── SubChipInstance.cs          # Instância de subchip
│   │   └── WireInstance.cs             # Fio/conexão
│   ├── Helpers/
│   │   ├── GridHelper.cs               # Sistema de grid/snapping
│   │   └── IDGenerator.cs              # Gerador de IDs únicos
│   ├── Interaction/                     # Sistema de interação do usuário
│   │   ├── CameraController.cs         # Controle de câmera 2D
│   │   ├── ChipInteractionController.cs # CONTROLADOR PRINCIPAL (1066 linhas)
│   │   ├── InteractionState.cs         # Estado global de interação
│   │   ├── KeyboardShortcuts.cs        # Atalhos de teclado
│   │   ├── UndoController.cs           # Sistema de undo/redo
│   │   └── Interfaces/
│   │       ├── IInteractable.cs        # Elementos clicáveis
│   │       └── IMoveable.cs            # Elementos mováveis
│   ├── Main/
│   │   ├── Main.cs                     # Classe estática central
│   │   └── UnityMain.cs                # MonoBehaviour principal
│   └── Project/
│       ├── BuiltinChipCreator.cs       # Fábrica de chips pré-fabricados
│       ├── BuiltinCollectionCreator.cs # Coleções de chips
│       ├── ChipLibrary.cs              # Biblioteca central de chips
│       ├── DevChipInstance.cs          # Chip sendo editado
│       └── Project.cs                  # Gerenciador de projeto
│
├── Graphics/                            [RENDERIZAÇÃO E UI]
│   ├── DrawSettings.cs                  # Configurações visuais/temas
│   ├── UI/
│   │   ├── MenuHelper.cs               # Utilitários para menus
│   │   ├── UIDrawer.cs                 # Renderizador de UI
│   │   └── Menus/
│   │       ├── BottomBarUI.cs          # Barra inferior de chips
│   │       ├── ChipCustomizationMenu.cs # Customização de aparência
│   │       ├── ChipLabelMenu.cs        # Edição de labels
│   │       ├── ChipLibraryMenu.cs      # Navegador de biblioteca
│   │       ├── ChipSaveMenu.cs         # Salvar/renomear chips
│   │       ├── ContextMenu.cs          # Menu de contexto (clique direito)
│   │       ├── MainMenu.cs             # Menu principal
│   │       ├── PinEditMenu.cs          # Editor de pinos I/O
│   │       ├── PreferencesMenu.cs      # Preferências do usuário
│   │       ├── PulseEditMenu.cs        # Editor de chip Pulse
│   │       ├── RebindKeyChipMenu.cs    # Rebind de teclas (Key chip)
│   │       ├── RomEditMenu.cs          # Editor hexadecimal de ROM
│   │       ├── SearchPopup.cs          # Busca rápida (Ctrl+K)
│   │       ├── SimPausedUI.cs          # UI quando simulação pausada
│   │       ├── UnsavedChangesPopup.cs  # Confirmação de mudanças
│   │       └── ViewedChipsBar.cs       # Breadcrumb de navegação
│   └── World/
│       ├── CustomizationSceneDrawer.cs # Renderização na customização
│       ├── DevSceneDrawer.cs           # Renderização na edição
│       ├── WireDrawer.cs               # Renderização de fios
│       ├── WireLayoutHelper.cs         # Layout de fios multi-bit
│       └── WorldDrawer.cs              # Ponto de entrada de renderização
│
├── SaveSystem/                          [PERSISTÊNCIA]
│   ├── DescriptionCreator.cs            # Cria Descriptions a partir de instâncias
│   ├── Loader.cs                        # Carrega projetos e chips
│   ├── SavePaths.cs                     # Caminhos de arquivos
│   ├── SaveUtils.cs                     # Utilitários de salvamento
│   ├── Saver.cs                         # Salva projetos e chips
│   └── UpgradeHelper.cs                 # Upgrade de versões antigas
│
├── Simulation/                          [MOTOR DE SIMULAÇÃO]
│   ├── SimChip.cs                       # Representação de chip na sim
│   ├── SimPin.cs                        # Representação de pino na sim
│   ├── Simulator.cs                     # MOTOR PRINCIPAL (thread separada)
│   ├── PinState.cs                      # Formato de estado de pino (32 bits)
│   └── Helpers/
│       └── SimModifyCommand.cs          # Comandos thread-safe
│
└── Seb/                                 [FRAMEWORK CUSTOMIZADO]
    ├── Helpers/
    │   ├── ColHelper.cs                 # Helpers de cores
    │   ├── ComputeHelper.cs             # Helpers de compute shaders
    │   ├── Maths.cs                     # Utilitários matemáticos
    │   ├── StringHelper.cs              # Utilitários de strings
    │   └── Input/
    │       ├── InputHelper.cs           # Sistema de input abstrato
    │       └── Input Source/
    │           ├── IInputSource.cs      # Interface de input
    │           └── UnityInputSource.cs  # Implementação Unity
    └── SebVis/                          # Sistema de visualização 2D
        ├── Anchor.cs                    # Sistema de ancoragem
        ├── Draw.cs                      # API de desenho
        ├── DrawManager.cs               # Gerenciador de renderização
        └── Internal/
            ├── Drawer.cs                # Renderizador base
            ├── FontMap.cs               # Mapeamento de fontes
            ├── InstancedDrawer.cs       # Renderização instanciada
            ├── QuadGenerator.cs         # Geração de meshes
            └── SebText/                 # Sistema de texto customizado
                ├── Loader/              # Carregamento de fontes
                └── Renderer/            # Renderização de texto
```

---

## CONCEITOS FUNDAMENTAIS

### 1. HIERARQUIA DE CHIPS

**Chips Builtin (Pré-fabricados):**
- NAND, TriStateBuffer
- Clock, Pulse (geradores de sinal)
- RAM, ROM (memória)
- Displays (7-Seg, RGB, Dot, LED)
- Split/Merge (conversores de bit-width)
- In/Out (pinos de I/O)
- Bus Origin/Terminus (barramentos)
- Key (input de teclado)
- Buzzer (output de áudio)

**Chips Customizados:**
- Criados pelo usuário
- Compostos de subchips (builtin ou custom)
- Podem ser usados como subchips em outros chips
- Hierarquia recursiva ilimitada

### 2. SISTEMA DE PINOS

**Tipos de Pinos:**
- **Input Pins:** Recebem sinais de fora do chip
- **Output Pins:** Enviam sinais para fora do chip
- **DevPins:** Pinos de I/O do chip em edição (viram In/Out quando usado como subchip)

**Bit-Width:**
- Pinos podem ser de 1, 4, 8 ou 16 bits
- Fios só podem conectar pinos de mesma largura
- Split/Merge convertem entre larguras

**PinAddress (Endereçamento):**
```csharp
struct PinAddress {
    int PinOwnerID;  // ID do chip/devpin dono
    int PinID;       // ID do pino específico
}
```

### 3. SISTEMA DE FIOS

**WireInstance:**
- Conecta um pino source a um pino target
- Pode ter múltiplos wire points para roteamento customizado
- Suporta wire-to-wire connections (fio conectado a outro fio)
- Fios multi-bit renderizam bits individuais com offset

**Wire Points:**
- Pontos intermediários de roteamento
- Editáveis pelo usuário (arrastar para ajustar)
- Sistema de snap-to-grid

### 4. ESTADO DE SIMULAÇÃO

**PinState (32 bits):**
```
Bits 31-16: Tristate flags (1 bit por canal)
Bits 15-0:  Bit states (valor real 0/1)
```

**Estados por bit:**
- `0` = LOW (0V)
- `1` = HIGH (5V)
- `2` = DISCONNECTED (tristate, alta impedância)

**Exemplo para 4 bits:**
- `0x00000101` = `[0101]` = HIGH, LOW, HIGH, LOW
- `0x00010101` = `[0101]` com bit 0 tristate = DISCONNECTED, LOW, HIGH, LOW

---

## FLUXO DE EXECUÇÃO

### Inicialização

```
UnityMain.Awake()
  ├─ Cria AudioState
  ├─ Main.Init()
  │   ├─ Garante diretórios de save existem
  │   ├─ Carrega AppSettings
  │   └─ Configura InputHelper
  └─ Main.CreateOrLoadProject()
      ├─ Carrega ProjectDescription
      ├─ Cria ChipLibrary (builtin + custom)
      ├─ Carrega chip inicial
      └─ Inicia thread de simulação
```

### Loop Principal (Main Thread)

```
UnityMain.Update() (cada frame Unity)
  └─ Main.Update()
      ├─ CameraController.Update()
      │   └─ Pan, zoom, limites de câmera
      │
      ├─ ActiveProject.Update()
      │   ├─ HandleProjectInput()
      │   │   ├─ Menu toggles (Esc, Tab)
      │   │   ├─ Save chip (Ctrl+S)
      │   │   ├─ Enter/Exit view mode
      │   │   └─ Navegação breadcrumb
      │   │
      │   └─ controller.Update()
      │       ├─ HandleKeyboardInput()
      │       │   ├─ Undo/Redo (Ctrl+Z/Y)
      │       │   ├─ Delete (Del)
      │       │   ├─ Duplicate (Ctrl+D)
      │       │   ├─ Select All (Ctrl+A)
      │       │   └─ Search (Ctrl+K)
      │       │
      │       └─ HandleMouseInput()
      │           ├─ Placement mode
      │           ├─ Selection mode (box select)
      │           ├─ Move mode
      │           ├─ Wire creation/editing
      │           └─ Pin clicking
      │
      ├─ InteractionState.ClearFrame()
      │   └─ Limpa estados temporários
      │
      ├─ WorldDrawer.DrawWorld(ActiveProject)
      │   ├─ DevSceneDrawer.Draw()
      │   │   ├─ Grid
      │   │   ├─ Fios
      │   │   ├─ SubChips
      │   │   ├─ DevPins
      │   │   ├─ Displays
      │   │   └─ Selection box
      │   │
      │   └─ CustomizationSceneDrawer.Draw()
      │       └─ Preview de aparência do chip
      │
      └─ UIDrawer.Draw()
          ├─ Menus ativos
          ├─ BottomBarUI
          ├─ ViewedChipsBar
          └─ Popups
```

### Thread de Simulação (Separada)

```
Project.SimThread() (loop infinito em thread de alta prioridade)
  │
  ├─ Simulator.ApplyModifications()
  │   └─ Processa fila thread-safe de modificações
  │       ├─ Add/Remove chips
  │       ├─ Add/Remove pins
  │       └─ Add/Remove connections
  │
  ├─ Sincroniza com main thread (espera frame)
  │
  ├─ ViewedChip.UpdateStateFromSim()
  │   └─ Copia estados de SimPins para visual
  │
  ├─ Se pausado: Sleep(10ms), continue
  │
  ├─ Simulator.RunSimulationStep()
  │   │
  │   ├─ Propaga inputs controlados por jogador
  │   │   └─ Copia estados de DevPins para SimPins
  │   │
  │   ├─ Para cada subchip (em ordem topológica):
  │   │   ├─ PropagateInputs()
  │   │   │   └─ Envia estado de cada input pin para conectados
  │   │   │
  │   │   ├─ ProcessChip()
  │   │   │   ├─ Se builtin: executa lógica direta
  │   │   │   └─ Se custom: recursão (processa internamente)
  │   │   │
  │   │   └─ PropagateOutputs()
  │   │       └─ Envia estado de cada output pin para conectados
  │   │
  │   ├─ Atualiza audio state (Buzzer)
  │   │
  │   └─ Incrementa simulationFrame
  │
  ├─ Sleep para atingir target ticks/sec
  │   └─ Default: 1000 steps/segundo
  │
  └─ Atualiza performance counter
```

---

## PRINCIPAIS CLASSES E RESPONSABILIDADES

### Camada de Dados (Description)

**ChipDescription**
- Descrição completa de um chip (serializável)
- Campos: name, type, size, colour, pins, subchips, wires, displays
- Comparação case-insensitive de nomes
- Usado para salvamento/carregamento

**ProjectDescription**
- Metadados do projeto
- Preferências de simulação (velocidade, pause, grid)
- Lista de chips customizados
- Sistema de favoritos (StarredList)
- Coleções organizadas pelo usuário

**WireDescription**
- Source pin address
- Target pin address
- Wire points (Vector2[])

### Camada de Runtime (Game)

**Project**
- Gerencia projeto completo
- View stack (navegação de chips)
- Thread de simulação
- ChipLibrary
- UndoController
- Detecção de mudanças não salvas

**DevChipInstance**
- Chip sendo editado
- Lista de elementos: subchips, devpins, wires, displays
- Sincroniza com simulação
- Valida operações (previne ciclos, sobreposição)
- Cria/aplica undo actions

**ChipInteractionController (1066 linhas)**
- CONTROLADOR PRINCIPAL de interação
- Estados: placing, selecting, moving, wire_creating, wire_editing
- Box selection
- Snap to grid
- Straight line mode
- Wire-to-wire connections
- Duplicação (Ctrl+D)
- Validações complexas

**ChipLibrary**
- Biblioteca central de todos chips (builtin + custom)
- Lookup O(1) por nome (Dictionary case-insensitive)
- Rastreia chips ocultos
- Análise de dependências (GetDirectParentChips)

### Camada de Simulação (Simulation)

**Simulator**
- Motor principal (roda em thread separada)
- Gerencia: chips, pins, connections
- ConcurrentQueue para modificações thread-safe
- Ordenação topológica de chips
- Propagação de sinais
- Processamento de chips builtin

**SimChip**
- Representação de chip na simulação
- InputPins[], OutputPins[]
- SubChips[]
- InternalState[] (para RAM, ROM, Clock, etc)
- Conta quantos inputs estão prontos

**SimPin**
- State (uint de 32 bits)
- Lista de pins conectados
- LastReceivedFromID (resolução de conflitos)
- NumInputsReceivedThisFrame

**PinState (struct estático)**
- Funções para manipular estado de 32 bits
- Extrai/seta bits individuais
- Lida com tristate
- Constantes: LogicLow, LogicHigh, Disconnected

### Camada de Renderização (Graphics)

**WorldDrawer**
- Ponto de entrada de renderização
- Delega para DevSceneDrawer ou CustomizationSceneDrawer

**DevSceneDrawer**
- Renderiza cena de edição
- Grid, fios, chips, pins, displays
- Mostra estados de simulação (cores)
- Overlay de seleção

**UIDrawer**
- Renderiza todos os menus
- Sistema de pilha de menus
- Eventos de UI

**SebVis Framework**
- Draw.cs: API de desenho imediato
- Suporte para: quads, círculos, linhas, texto
- Renderização instanciada para performance
- Shaders customizados

---

## SISTEMA DE SIMULAÇÃO - DETALHES TÉCNICOS

### Processamento de Chips Builtin

**NAND:**
```csharp
uint nandOp = 1 ^ (chip.InputPins[0].State & chip.InputPins[1].State);
chip.OutputPins[0].State = (ushort)(nandOp & 1);
```

**Clock:**
```csharp
int stepsPerTransition = Prefs_SimStepsPerClockTick;
bool high = (simulationFrame / stepsPerTransition) & 1 == 0;
chip.OutputPins[0].State = high ? LogicHigh : LogicLow;
```

**Pulse:**
```csharp
bool buttonDown = chip.InternalState[0] == 1;
bool rising = buttonDown && chip.InternalState[1] == 0;
chip.InternalState[1] = buttonDown ? 1u : 0;
chip.OutputPins[0].State = rising ? LogicHigh : LogicLow;
```

**TriStateBuffer:**
```csharp
bool enabled = PinState.FirstBitHigh(enablePin);
chip.OutputPins[0].State = enabled ? inputPin.State : Disconnected;
```

**RAM (8-bit, edge-triggered):**
```csharp
uint address = chip.InputPins[0].State;
uint data = chip.InputPins[1].State;
bool writeEnable = PinState.FirstBitHigh(chip.InputPins[2]);
bool clockHigh = PinState.FirstBitHigh(chip.InputPins[3]);

bool wasClockLow = chip.InternalState[^1] == 0;
bool isRisingEdge = clockHigh && wasClockLow;
chip.InternalState[^1] = clockHigh ? 1u : 0;

if (isRisingEdge && writeEnable) {
    chip.InternalState[address] = data;
}
chip.OutputPins[0].State = (ushort)chip.InternalState[address];
```

**ROM (256x16):**
```csharp
uint address = chip.InputPins[0].State;
uint data = chip.InternalState[address];
chip.OutputPins[0].State = (ushort)((data >> 8) & 0xFF);  // High byte
chip.OutputPins[1].State = (ushort)(data & 0xFF);         // Low byte
```

**Displays (7-Seg, RGB, Dot, LED):**
```csharp
// Apenas copia inputs para internal state
// Renderização lida com visualização
chip.InternalState[displayIndex] = chip.InputPins[displayIndex].State;
```

**Split (8→4+4):**
```csharp
uint input8bit = chip.InputPins[0].State;
chip.OutputPins[0].State = (ushort)((input8bit >> 4) & 0xF);  // High nibble
chip.OutputPins[1].State = (ushort)(input8bit & 0xF);         // Low nibble
```

**Merge (4+4→8):**
```csharp
uint high = chip.InputPins[0].State;
uint low = chip.InputPins[1].State;
chip.OutputPins[0].State = (ushort)((high << 4) | low);
```

### Resolução de Conflitos

Quando múltiplos sinais chegam ao mesmo pino:
```csharp
if (numInputsReceivedThisFrame > 0) {
    uint OR = source.State | State;
    uint AND = source.State & State;

    // Escolhe aleatoriamente entre OR e AND
    ushort bitsNew = Simulator.RandomBool() ? (ushort)OR : (ushort)AND;

    // Mas sempre aceita bits tristated
    ushort tristatedBitsMask = (ushort)(OR >> 16);
    bitsNew = (ushort)((bitsNew & ~tristatedBitsMask) | (OR & tristatedBitsMask));

    State = (uint)(bitsNew | (tristatedBitsMask << 16));
}
```

Esta randomização simula condições de corrida realistas.

### Ordenação Topológica

**Objetivo:** Processar chips na ordem de dependência para propagação correta.

**Algoritmo:**
1. Para cada chip sem predecessores, marca como "pronto"
2. Processa todos chips prontos
3. Quando chip é processado, seus sucessores ficam "prontos"
4. Repete até todos processados

**Randomização:**
- A cada 100 frames, randomiza ordem entre chips no mesmo nível
- Simula variações de timing realistas
- Expõe condições de corrida ao usuário

---

## SISTEMA DE SALVAMENTO

### Estrutura de Arquivos

```
%AppData%/Roaming/Digital Logic Sim/
│
├── Settings/
│   └── appsettings.json          # Configurações globais (resolução, vsync, etc)
│
└── Projects/
    └── [NomeDoProjeto]/
        ├── project.json          # ProjectDescription
        │
        ├── Chips/
        │   ├── MyChip1.json     # ChipDescription
        │   ├── MyChip2.json
        │   └── ...
        │
        └── DeletedChips/        # Backup automático
            ├── OldChip.json
            └── ...
```

### Formato de Salvamento

**project.json:**
```json
{
  "ProjectName": "MeuProjeto",
  "DLSVersion_LastSaved": "2.1.6",
  "CreationTime": "2025-01-15T10:30:00",
  "LastModifiedTime": "2025-01-20T15:45:00",
  "Prefs_SimTargetStepsPerSecond": 1000,
  "Prefs_SimStepsPerClockTick": 250,
  "Prefs_PauseSim": false,
  "Prefs_ShowGrid": true,
  "AllCustomChipNames": ["ALU", "Register", "Counter"],
  "StarredList": ["ALU", "Register"],
  "ChipCollections": [
    {
      "CollectionName": "CPU Components",
      "ChipNames": ["ALU", "Register", "Counter"]
    }
  ]
}
```

**Chips/MyChip.json:**
```json
{
  "Name": "ALU",
  "ChipType": "Custom",
  "Size": {"x": 10.0, "y": 8.0},
  "Colour": {"r": 0.3, "g": 0.5, "b": 0.8},
  "InputPins": [
    {
      "Name": "A",
      "BitWidth": 8,
      "Pos": {"x": -5.0, "y": 2.0},
      "NameDisplayMode": 1,
      "Colour": {"r": 1, "g": 1, "b": 1}
    },
    {
      "Name": "B",
      "BitWidth": 8,
      "Pos": {"x": -5.0, "y": 0.0},
      "NameDisplayMode": 1,
      "Colour": {"r": 1, "g": 1, "b": 1}
    }
  ],
  "OutputPins": [
    {
      "Name": "Out",
      "BitWidth": 8,
      "Pos": {"x": 5.0, "y": 1.0},
      "NameDisplayMode": 1,
      "Colour": {"r": 1, "g": 1, "b": 1}
    }
  ],
  "SubChips": [
    {
      "ID": 1,
      "Name": "NAND",
      "Pos": {"x": 0.0, "y": 0.0},
      "IsDeleted": false
    }
  ],
  "Wires": [
    {
      "SourcePinAddress": {"PinOwnerID": 0, "PinID": 0},
      "TargetPinAddress": {"PinOwnerID": 1, "PinID": 0},
      "WirePoints": [
        {"x": -3.0, "y": 2.0},
        {"x": -2.0, "y": 0.5}
      ],
      "IsDeleted": false
    }
  ],
  "DisplayData": []
}
```

### Detecção de Mudanças

**UnsavedChangeDetector:**
- Compara JSON atual com JSON salvo
- Usa Newtonsoft.Json para serialização
- Ignora whitespace e ordem de propriedades
- Detecta mudanças em:
  - Estrutura do chip (subchips, fios, pinos)
  - Aparência (cor, tamanho, posições)
  - Dados internos (ROM data, etc)

### Sistema de Backup

**Quando chip é deletado:**
1. Salva chip em `DeletedChips/[Nome].json`
2. Remove de `project.json`
3. Remove arquivo de `Chips/[Nome].json`
4. Remove de todos chips que o usavam como subchip

---

## SISTEMA DE UNDO/REDO

### UndoController

**Tipos de Ações:**
- `AddElementsAction`: Adicionar subchips/devpins
- `DeleteElementsAction`: Deletar elementos
- `AddWiresAction`: Adicionar fios
- `DeleteWiresAction`: Deletar fios
- `MoveElementsAction`: Mover elementos
- `EditChipAction`: Editar e salvar chip

**Estrutura:**
```csharp
class UndoController {
    Stack<IUndoableAction> undoStack;
    Stack<IUndoableAction> redoStack;

    void RegisterAction(IUndoableAction action);
    void Undo();
    void Redo();
}
```

**Exemplo - MoveElementsAction:**
```csharp
class MoveElementsAction : IUndoableAction {
    int[] elementIDs;
    Vector2[] startPositions;
    Vector2[] endPositions;

    void Undo() {
        // Restaura posições originais
        for (int i = 0; i < elementIDs.Length; i++) {
            GetElement(elementIDs[i]).Pos = startPositions[i];
        }
    }

    void Redo() {
        // Aplica posições finais
        for (int i = 0; i < elementIDs.Length; i++) {
            GetElement(elementIDs[i]).Pos = endPositions[i];
        }
    }
}
```

---

## SISTEMA DE BARRAMENTOS (BUS)

### Conceito

Permite teleportar sinais entre pontos distantes sem fios visuais.

### Implementação

**Bus Origin:**
- Tipo especial de chip
- Tem 1 input (oculto) + 1 output (visível)
- Armazena BusID único

**Bus Terminus:**
- Par do Origin (mesmo BusID)
- Tem apenas 1 input
- Oculto da biblioteca (criado automaticamente com Origin)

**Criação Automática:**
```csharp
// Ao colocar Bus Origin:
BusOrigin origin = PlaceChip("Bus_8Bit");
origin.BusID = IDGenerator.GetNextBusID();

BusTerminus terminus = PlaceChip("BusTerminus_8Bit");
terminus.BusID = origin.BusID;  // Link
terminus.Pos = origin.Pos + Vector2.right * 10;
```

**Na Simulação:**
```csharp
// Origin recebe sinal no input oculto
originChip.InputPins[0].State = incomingSignal;

// Encontra Terminus com mesmo BusID
terminusChip = FindTerminusForOrigin(originChip.BusID);

// Copia sinal para output do Origin (que fios podem conectar)
originChip.OutputPins[0].State = terminusChip.InputPins[0].State;
```

---

## SISTEMA DE KEY CHIPS

### Conceito

Pinos de input ativados por teclas do teclado.

### Implementação

**KeyChip:**
- Tipo especial de chip
- Armazena KeyCode (tecla associada)
- Output de 1 bit

**Configuração:**
```csharp
// Menu de rebind
RebindKeyChipMenu.Show(keyChip);
// Espera usuário pressionar tecla
keyChip.KeyCode = Input.GetKeyDown(...);
```

**Na Simulação:**
```csharp
// Main thread atualiza estado baseado em input
if (Input.GetKey(keyChip.KeyCode)) {
    keyChip.InputPins[0].State = LogicHigh;
} else {
    keyChip.InputPins[0].State = LogicLow;
}
```

---

## SISTEMA DE DISPLAYS

### Tipos de Display

**7-Segment Display:**
- 7 inputs de 1 bit (segmentos a-g)
- Renderização vetorial dos segmentos

**RGB Display (Pixel Grid):**
- 1 input de 8 bits por pixel
- Formato: RRRGGGBB
- Grid configurável (ex: 16x16 pixels)

**Dot Display (Monochrome Grid):**
- 1 input de 1 bit por pixel
- Renderização como matriz de pontos

**LED Display:**
- 1 input de 1 bit
- Renderização como círculo colorido

### Configuração

```json
"DisplayData": [
  {
    "DisplayType": "SevenSegmentDisplay",
    "BitWidth": 1,
    "NumBits": 7,
    "Positions": [
      {"x": 0, "y": 0},
      {"x": 0.5, "y": 0},
      ...
    ]
  }
]
```

### Renderização

```csharp
DevSceneDrawer.DrawDisplays(chip) {
    foreach (display in chip.Displays) {
        uint state = chip.SubChips[display.ChipIndex].InternalState[display.Index];

        switch (display.Type) {
            case SevenSegmentDisplay:
                DrawSevenSegmentDisplay(state, pos);
                break;
            case DisplayRGB:
                DrawRGBDisplay(state, gridSize, pos);
                break;
            ...
        }
    }
}
```

---

## PADRÕES DE DESIGN UTILIZADOS

### 1. Separation of Concerns (Camadas)
```
Description (Dados)
    ↓
Game (Runtime)
    ↓
Simulation (Lógica)
    ↓
Graphics (Apresentação)
```

### 2. Factory Pattern
```csharp
BuiltinChipCreator.CreateChip(ChipType type) -> ChipDescription
DevChipInstance.LoadFromDescription(ChipDescription) -> DevChipInstance
```

### 3. Command Pattern
```csharp
SimModifyCommand {
    AddChip(chip)
    RemoveChip(chipID)
    AddConnection(pinA, pinB)
}
// Enqueued thread-safe
Simulator.modificationQueue.Enqueue(command);
```

### 4. Memento Pattern
```csharp
UndoController {
    SaveState() -> ChipDescription (memento)
    RestoreState(ChipDescription)
}
```

### 5. Observer Pattern (implícito)
```csharp
Simulator.UpdatePinStates()
    ↓
PinInstance.ObserveSimState()
    ↓
WireDrawer.UpdateColorFromState()
```

### 6. Composite Pattern
```csharp
SimChip {
    SubChips: SimChip[]  // Recursão
}
```

### 7. State Machine (ChipInteractionController)
```
States: Idle → Placing → Placed
        Idle → Selecting → Moving → Moved
        Idle → WireCreating → WireCreated
```

### 8. Flyweight Pattern
```csharp
// ChipDescriptions compartilhadas
ChipLibrary.GetDescription("NAND") // Mesmo objeto para todas instâncias
SubChipInstance.Description // Referência, não cópia
```

---

## OTIMIZAÇÕES DE PERFORMANCE

### Thread de Simulação Separada
- Alta prioridade
- Roda independente de rendering
- Target: 1000 steps/segundo
- Sleep adaptativo para economizar CPU

### Ordenação Lazy
```csharp
// Só reordena quando necessário
if (needsOrderPass) {
    ReorderChips();
    needsOrderPass = false;
}
```

### Caching de Arrays
```csharp
DevChipInstance {
    PinInstance[] inputPins_cached;  // Atualizado só quando muda
    PinInstance[] outputPins_cached;
}
```

### Instanced Rendering
```csharp
// Renderiza múltiplos elementos em 1 draw call
InstancedDrawer.DrawInstances(chips, material);
```

### String Pooling
```csharp
// Evita alocações repetidas
StringHelper.Pool("NAND");
```

---

## ATALHOS DE TECLADO

### Navegação
- `Esc` - Abrir/fechar menu principal
- `Tab` - Alternar biblioteca de chips
- `Ctrl+K` - Busca rápida de chips
- `Enter` - Entrar em chip (view mode)
- `Backspace` - Sair de chip (exit view mode)

### Edição
- `Ctrl+S` - Salvar chip atual
- `Ctrl+Z` - Undo
- `Ctrl+Y` ou `Ctrl+Shift+Z` - Redo
- `Ctrl+D` - Duplicar seleção
- `Ctrl+A` - Selecionar tudo
- `Delete` - Deletar seleção
- `Shift` (hold) - Modo linha reta (movement)
- `Ctrl` (hold) - Desabilitar snap to grid

### Interação
- `Mouse Wheel` - Zoom in/out
- `Middle Mouse` - Pan
- `Left Click` - Selecionar/interagir
- `Right Click` - Menu de contexto
- `Left Drag` - Box select
- `Left Drag (element)` - Mover elemento

---

## TEMAS E CUSTOMIZAÇÃO

### DrawSettings

**Cores principais:**
```csharp
Color backgroundColor
Color gridColor
Color wireOffColor
Color wireOnColor
Color wireHighZColor
Color wireErrorColor
Color selectionBoxColor
Color chipBodyColor
Color pinColor
```

**Temas pré-definidos:**
- Light Theme
- Dark Theme
- High Contrast

**Customização de Chips:**
- Cor do corpo
- Tamanho (largura x altura)
- Nome e label
- Posição dos pinos

---

## EXTENSIBILIDADE

### Como Adicionar Novo Chip Builtin

**1. Adicionar enum:**
```csharp
// ChipTypes.cs
public enum ChipType {
    ...
    MyNewChip,
}
```

**2. Criar descrição:**
```csharp
// BuiltinChipCreator.cs
ChipDescription CreateMyNewChip() {
    return new ChipDescription {
        Name = "MyNewChip",
        ChipType = ChipType.MyNewChip,
        InputPins = new[] {
            new PinDescription { Name = "In", BitWidth = 1 }
        },
        OutputPins = new[] {
            new PinDescription { Name = "Out", BitWidth = 1 }
        }
    };
}
```

**3. Implementar lógica:**
```csharp
// Simulator.cs
void ProcessBuiltinChip(SimChip chip) {
    switch (chip.ChipType) {
        ...
        case ChipType.MyNewChip:
            chip.OutputPins[0].State = ProcessMyNewChip(chip.InputPins[0].State);
            break;
    }
}
```

**4. (Opcional) Renderização customizada:**
```csharp
// DevSceneDrawer.cs
void DrawSpecialChip(SubChipInstance chip) {
    if (chip.Type == ChipType.MyNewChip) {
        // Custom rendering
    }
}
```

### Como Adicionar Novo Menu

**1. Criar classe:**
```csharp
// Graphics/UI/Menus/MyMenu.cs
public static class MyMenu {
    static bool isOpen;

    public static void Open() {
        isOpen = true;
    }

    public static void Draw() {
        if (!isOpen) return;

        // Usando SebVis
        Draw.String("My Menu", pos, size, Anchor.Center);

        if (UIHelper.Button("Close")) {
            isOpen = false;
        }
    }
}
```

**2. Registrar em UIDrawer:**
```csharp
// UIDrawer.cs
void Draw() {
    ...
    MyMenu.Draw();
}
```

---

## DEBUGGING E FERRAMENTAS DE DESENVOLVIMENTO

### VidTools (Dev/VidTools/)
- Ferramentas para gravação de vídeos
- Gravação/reprodução de input
- Controle de câmera para demos

### Debug Settings
```csharp
UnityMain {
    bool showDebugInfo;
    bool logSimulationPerformance;
}
```

### Performance Monitoring
```csharp
Simulator {
    PerformanceCounter {
        int stepsPerSecond;
        float averageStepTime;
    }
}
```

### Validation Checks
```csharp
DevChipInstance.Validate() {
    // Verifica:
    - Ciclos de dependência
    - Sobreposição de chips
    - Conexões inválidas
    - Pinos desconectados (warning)
}
```

---

## LIMITAÇÕES CONHECIDAS

### Técnicas
- Máximo de 16 bits por pino
- IDs são `int` (limite de ~2 bilhões de elementos)
- Thread de simulação única (não paralelizável facilmente)
- Sem netlist compilation (sempre interpreta hierarquia)

### UX
- Não há multi-seleção de chips na biblioteca
- Não há copy/paste entre projetos
- Não há git-style versioning de chips
- Busca é apenas por nome (não por tag/categoria)

### Performance
- Chips muito grandes (>1000 subchips) podem causar lag
- Wire-to-wire recursivo profundo pode ser lento
- Renderização de texto não é otimizada para milhares de labels

---

## DEPENDÊNCIAS EXTERNAS

### Unity Packages
- Unity InputSystem (opcional)
- Unity UI (não usado, UI customizada)

### NuGet Packages
- **Newtonsoft.Json** (v13.0.1)
  - Serialização/deserialização
  - Licença: MIT

### Assets Internos
- **SebVis Framework** (customizado por Sebastian Lague)
- **FreeType Fonts** (para renderização de texto)

---

## COMPATIBILIDADE DE VERSÕES

### Formato de Salvamento
- **Versão 2.0.0+:** Formato JSON estável
- **Versões antigas:** Sistema de upgrade automático via `UpgradeHelper`

### Upgrade Path
```csharp
UpgradeHelper.Upgrade(ChipDescription desc, Version fromVersion) {
    if (fromVersion < new Version(2, 0)) {
        // Converte formato antigo
    }
    if (fromVersion < new Version(2, 1)) {
        // Adiciona novos campos com defaults
    }
}
```

---

## RECURSOS ÚTEIS

### Documentação Original
- GitHub: https://github.com/SebLague/Digital-Logic-Sim
- Discussões: https://github.com/SebLague/Digital-Logic-Sim/discussions
- Builds: https://sebastian.itch.io/digital-logic-sim

### YouTube Series
- [Exploring How Computers Work](https://www.youtube.com/playlist?list=PLFt_AvWsXl0dPhqVsKt1Ni_46ARyiCGSq)
- Explica conceitos de lógica digital
- Demonstra construção de CPU no simulador

### Comunidade
- Discussões de desenvolvimento em GitHub/Discussions/Dev
- Pull requests bem-vindos (foco em performance/UX/bugfixes)

---

## GUIA RÁPIDO PARA DESENVOLVEDORES

### Setup Inicial
1. Clone o repositório
2. Abra no Unity (versão recomendada: 2022.3 LTS ou superior)
3. Cena principal: `Assets/Scenes/Main.unity`
4. Ponto de entrada: `UnityMain.cs`

### Fluxo de Trabalho Comum

**Adicionar funcionalidade:**
1. Identifique camada apropriada (Description/Game/Simulation/Graphics)
2. Implemente seguindo padrões existentes
3. Teste em modo Play
4. Verifique salvamento/carregamento funciona
5. Adicione ao sistema de undo se aplicável

**Debugar simulação:**
1. Coloque breakpoint em `Simulator.ProcessChip()`
2. Pause simulação (`Main.ActiveProject.PauseSim()`)
3. Inspecione `SimChip.InputPins[].State`

**Testar performance:**
1. Crie chip com muitos subchips (100+)
2. Monitore `Simulator.PerformanceCounter`
3. Use Unity Profiler para CPU/Memory

### Convenções de Código

**Nomenclatura:**
- Classes: PascalCase (`ChipInstance`)
- Métodos: PascalCase (`UpdateState()`)
- Variáveis locais: camelCase (`chipIndex`)
- Campos privados: camelCase (`inputPins`)
- Constantes: PascalCase (`LogicHigh`)

**Organização:**
- Usar `#region` para agrupar métodos relacionados
- Comentários explicativos para lógica complexa
- TODO comments para melhorias futuras

---

## GLOSSÁRIO

**Chip:** Componente lógico (builtin ou customizado)
**SubChip:** Instância de chip dentro de outro chip
**DevPin:** Pino de I/O do chip em desenvolvimento
**Pin:** Ponto de conexão (input ou output)
**Wire:** Conexão entre dois pinos
**Wire Point:** Ponto intermediário de roteamento de fio
**Bit-Width:** Número de bits de um pino (1, 4, 8 ou 16)
**PinState:** Estado de 32 bits de um pino (valor + tristate flags)
**Tristate:** Estado de alta impedância (disconnected)
**View Mode:** Visualizar internamente um chip (não editável)
**Edit Mode:** Editar estrutura de um chip
**View Stack:** Pilha de navegação de chips
**Builtin Chip:** Chip pré-fabricado (NAND, Clock, etc)
**Custom Chip:** Chip criado pelo usuário
**SimChip:** Representação de chip na simulação
**SimPin:** Representação de pino na simulação
**Topological Sort:** Ordenação por dependência
**Edge-Triggered:** Ativado na borda de sinal (rising/falling)
**Bus:** Sistema de teleporte de sinais (Origin + Terminus)
**Memento:** Snapshot de estado para undo/redo

---

## CONCLUSÃO

O Digital Logic Simulator é um projeto educacional excepcionalmente bem arquitetado que demonstra:

- **Separação clara de responsabilidades** (dados, lógica, apresentação)
- **Concorrência eficiente** (thread de simulação separada)
- **Persistência robusta** (JSON serializável, versionamento)
- **UX polida** (undo/redo, validações, feedback visual)
- **Extensibilidade** (fácil adicionar chips/funcionalidades)

A arquitetura modular e os padrões de design aplicados tornam o código fácil de entender e modificar, mesmo para desenvolvedores iniciantes em simulação de circuitos digitais.

Este contexto deve fornecer base sólida para:
- Entender a arquitetura do projeto
- Adicionar novas funcionalidades
- Debugar problemas
- Contribuir com pull requests
- Usar como referência para projetos similares

---

**Documento criado em:** 2025-10-21
**Baseado na versão:** 2.1.6
**Autor do documento:** Claude Code
**Autor do projeto:** Sebastian Lague
