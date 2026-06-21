# Plano de Features - Circuit Editor
_Atualizado: 2026-06-18_
    
## Features aprovadas (ordem de prioridade definida pelo Eron)

### 1. Labels nos fios (WIRE LABELS)
**O que é:** Texto opcional visível no meio do fio. Ex: "CLK", "RESET", "A[3]".
**Impacto no salvamento:** Sim — `WireDescription` ganha campo `Label` string opcional.
Retrocompatível (null/empty = sem label). Aumento mínimo no JSON (~10 bytes por fio com label).
**Impacto na correção:** NENHUM.
**Implementação:**
- `WireDescription.cs` → adicionar `public string Label;`
- `DevSceneDrawer.DrawWire()` → se `wire.Label` não vazio, `Draw.Text()` no ponto médio do fio
- UI: double-click no fio → input field flutuante → salva na WireInstance
- `WireInstance` → campo `Label` + serialização via DescriptionCreator

---

### 2. Comentários/anotações no canvas (ANNOTATIONS)
**O que é:** Caixas de texto livres no canvas. Aluno documenta raciocínio.
**Impacto no salvamento:** Sim — nova lista `Annotations[]` em `ChipDescription` (Text, Position, Size, Colour). Retrocompatível.
**Impacto na correção:** NENHUM.
**Implementação:**
- `AnnotationDescription.cs` (novo) — struct com Text, Position, Size, Colour
- `ChipDescription.cs` → adicionar `public AnnotationDescription[] Annotations;`
- `AnnotationInstance.cs` (novo) — IMoveable para drag
- `DevSceneDrawer` → `DrawAnnotations()` método novo
- `ChipInteractionController` → criar/selecionar/mover/deletar
- Shortcut: tecla A → cria annotation no mouse

---

### 3. Desenho automático de portas lógicas (GATE SYMBOLS)
**O que é:** Chips com nome AND/OR/XOR/XNOR/NOR/NAND/NOT desenham o símbolo IEEE.
**Impacto no salvamento:** NENHUM.
**Impacto na correção:** NENHUM.
**Implementação:**
- `GateSymbolDrawer.cs` (novo) — `DrawAND`, `DrawOR`, `DrawNOT`, `DrawXOR`, `DrawNAND`, `DrawNOR`, `DrawXNOR`
- `DevSceneDrawer.DrawSubChip()` → `IsGateChip(name)` → redireciona para GateSymbolDrawer
- Formas aproximadas com `Draw.Line` (segmentos): AND ~16 seg, OR ~24 seg, NOT triângulo+círculo
- Pins não mudam de posição

---

### 4. Cores e Visual (COLOUR + WIRE STYLE)

#### 4a. Swatches rápidos de cor para chips
**O que é:** Grade de ~12 cores predefinidas no ChipCustomizationMenu, acima do HSV picker.
Clicar = aplica cor instantaneamente. Ex: vermelho, azul, verde, amarelo, roxo, laranja, ciano, rosa, branco, cinza escuro, preto, verde-limão.
**Impacto no salvamento:** NENHUM (campo `Colour` já existe e é salvo).
**Implementação:**
- `ChipCustomizationMenu.cs` → grade de botões coloridos antes do `UI.DrawColourPicker`
- Array de cores predefinidas como constante

#### 4b. Textura/padrão nos fios (WIRE PATTERNS)
**O que é:** Dois mini-menus no painel do fio (ao selecionar/clicar fio):
- **Menu 1 — Cor:** swatches rápidos (mesmo estilo do 4a, cores predefinidas)
- **Menu 2 — Padrão:** sem padrão / listrado vertical / listrado horizontal

Listrado vertical = traços perpendiculares ao fio (tipo "zebra" ao longo do comprimento).
Listrado horizontal = traços paralelos ao fio (fio aparece dividido em faixas paralelas).
Padrão se combina com a cor escolhida. Ex: fio azul com listras verticais brancas.

**Impacto no salvamento:** Sim — `WireDescription` ganha:
- `public Color? CustomColour;` (nullable, null = cor padrão do sinal)
- `public WirePattern Pattern;` (enum: None=0, StripedVertical=1, StripedHorizontal=2)
Retrocompatível (campos opcionais, default = sem estilo).
**Impacto na correção:** NENHUM.

**Implementação:**
- `WireDescription.cs` → campos `CustomColour` e `Pattern`
- `WirePattern.cs` → enum None/StripedVertical/StripedHorizontal
- `WireInstance` → propriedades correspondentes
- `DevSceneDrawer.DrawWire()` ou `WireDrawer` → ao desenhar segmento, se Pattern != None:
  - StripedVertical: a cada N pixels ao longo do fio, alterna cor base ↔ cor clara
  - StripedHorizontal: desenha 2-3 linhas paralelas finas ao lado do fio principal
- UI: right-click no fio → popup com 2 mini-painéis (cor + padrão)
- `DescriptionCreator` → serializar/deserializar novos campos

---

## Features descartadas
- Mini-mapa
- Auto-colorir por tipo de chip
- Cor de fio independente do sinal (substituído por 4b)
- Regiões visuais (4d)
- Espessura por largura de bits (já existe no sistema)
- Multi-select por tipo
- Nome do chip inline editável
- Export PNG
- Versão do circuito no chip
- Snap to grid (já existe)

---

## Ordem de implementação
1. **Labels nos fios** — menor risco, maior impacto UX imediato
2. **Swatches rápidos (4a)** — rápido de fazer, visual imediato
3. **Textura/padrão nos fios (4b)** — organiza circuitos densos
4. **Comentários** — valor pedagógico alto
5. **Gate symbols** — visual puro, zero risco de dados
