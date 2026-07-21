# Contexto — Formulário `PlotFolhasWindow` e arquitetura do módulo PlotFolhas

Este documento descreve a arquitetura interna do formulário WPF e dos serviços do módulo `PlotFolhas`. Ele é o contrato técnico para agentes ou desenvolvedores que precisarem evoluir o código sem quebrar a estrutura existente.

## 1. Visão geral da arquitetura

```
Usuário (Ribbon/Console)
    ↓
PlotFolhasCommand.cs        → [CommandMethod] + [CntRibbonCommand] — adaptador fino
    ↓
PlotFolhasModule.cs         → ICntModule — composição do handler
    ↓
PlotFolhasHandler.cs        → orquestrador — cria sessão, abre janela, assina eventos
    ↓
PlotFolhasWindow.xaml/.cs   → UI WPF modeless — DataGrid + controles
    ↓ (eventos)
PlotFolhasHandler.cs        → processa eventos chamando serviços
    ↓
Serviços especializados     → regras de negócio, acesso ao ZWCAD, geração de arquivos
```

## 2. Estrutura de diretórios do módulo

```
03-Modules/PlotFolhas/
├── Bootstrap/
│   ├── PlotFolhasCommand.cs      # Atributos + delegação
│   └── PlotFolhasModule.cs       # ICntModule, compõe handler
├── Discovery/
│   └── FolhaScanner.cs           # Escaneia folhas no layout (CEP-*)
├── Domain/
│   ├── FolhaFormatCatalog.cs     # Catálogo de formatos CEP-A4...CEP-A0E
│   ├── FolhaInfo.cs              # Modelo da folha (INotifyPropertyChanged)
│   └── NamingHeader.cs           # Estrutura de nome com partes
├── Naming/
│   ├── ArquivoNomeService.cs     # Sanitização e validação de nomes
│   ├── FolhaNomenclaturaService.cs # Leitura/escrita do atributo CNT_NOME_ARQUIVO
│   ├── NamingStandardParser.cs   # Parser de separadores
│   └── ParsedName.cs             # Modelo de nome parseado
├── Plotting/
│   ├── PlotExecutionService.cs   # Executa plotagem PDF + export DWG
│   ├── PlotOutputPlan.cs         # Plano de saída (PDFs, DWGs, arquivos existentes)
│   ├── PlotService.cs            # Plotagem PDF via API ZWCAD
│   └── PlotSettingsConfigurator.cs # Configura PlotSettings
├── Export/
│   ├── DwgExportService.cs       # Exporta DWGs individuais
│   └── ModelIsolation/           # Isolamento do Model por viewports
├── Navigation/
│   └── SheetZoomService.cs       # Zoom para uma folha específica
└── UI/
    ├── PlotFolhasHandler.cs      # Orquestrador da janela
    ├── PlotFolhasViewModel.cs    # ViewModel (INotifyPropertyChanged)
    ├── PlotFolhasWindow.xaml     # Layout WPF
    ├── PlotFolhasWindow.xaml.cs  # Code-behind
    └── Services/
        ├── PlotFolhasSessionService.cs   # Cria sessão (scan + nomes + dispositivos)
        ├── PlotFolhasNamingService.cs    # Aplica estrutura, normaliza, valida
        ├── PlotFolhasGenerationService.cs # Prepara e executa geração de arquivos
        └── SeloBlockService.cs           # Scan de blocos, atributos, preenchimento de selo
```

## 3. Fluxo de dados — da abertura à geração

### 3.1 Criação da sessão

```
PlotFolhasHandler.Execute()
  → PlotFolhasSessionService.Create()
    → FolhaScanner.ScanActiveLayout()         // descobre folhas CEP-* no layout
    → FolhaNomenclaturaService.LoadSavedNames() // carrega CNT_NOME_ARQUIVO de cada bloco
    → Atribui nomes automáticos se vazios
    → PlotService.GetPlotDevices() / GetPlotStyleSheets()
    → NamingStandardParser.Parse()            // extrai separador e partes do primeiro nome
    → Retorna PlotFolhasSession
```

### 3.2 Abertura da janela

```
PlotFolhasHandler.ShowWindow(session)
  → SeloBlockService.GetBlockNames()          // lista blocos com atributos
  → new PlotFolhasWindow(...)                 // cria ViewModel + XAML
  → Assina eventos: ApplyStructuredName, FileNameEdited, Zoom, SaveNames, Plot,
                    StampBlockChanged, RefreshRequested
  → Vincula documento (fecha janela se trocar/fechar DWG)
  → ZwcadApplication.ShowModelessWindow()
```

### 3.3 Eventos da janela

| Evento | Handler | Ação |
|--------|---------|------|
| `ApplyStructuredNameRequested` | `OnApplyStructuredNameRequested` | `NamingService.ApplyStructure()` com suporte a campos sequenciais |
| `FileNameEdited` | `OnFileNameEdited` | `NamingService.NormalizeEditedName()` |
| `ZoomRequested` | `OnZoomRequested` | `SheetZoomService.ZoomTo()` |
| `SaveNamesRequested` | `OnSaveNamesRequested` | Salva nomes + preenche selo (se configurado) |
| `PlotRequested` | `OnPlotRequested` | Valida → Prepara → Preenche selo → Gera PDFs/DWGs |
| `StampBlockChanged` | `OnStampBlockChanged` | `SeloBlockService.GetAttributeTags()` |
| `RefreshRequested` | `OnRefreshRequested` | Recria sessão e reabre janela |

### 3.4 Geração de arquivos

```
OnPlotRequested()
  → window.CommitChanges()
  → NamingService.NormalizeAndValidate()
  → GenerationService.Prepare()
    → PlotOutputPlan.Create()         // separa PDFs e DWGs
    → Valida: folhas selecionadas, erros, plotter, pasta
    → Cria pasta (ou subpasta Emissão NN)
    → Retorna PlotFolhasGenerationPreparation
  → Confirma sobrescrita se necessário
  → ExecuteGeneration()
    → SeloBlockService.FillSeloAttributes()  // preenche atributo do selo
    → GenerationService.Execute()
      → PlotExecutionService.Execute()
        → FolhaNomenclaturaService.SaveNames()   // salva CNT_NOME_ARQUIVO (sem .pdf)
        → PlotService.PlotSheets()               // gera PDFs
        → DwgExportService.Export()              // gera DWGs individuais
    → GenerationService.TryOpenOutputFolder()    // abre Explorer
```

## 4. Arquitetura da UI (WPF)

### 4.1 PlotFolhasWindow.xaml — Layout (7 linhas de grid)

| Row | Conteúdo | Bindings/Controles |
|-----|----------|-------------------|
| 0 | Título + cards de resumo + botão Atualizar | `TotalSheetCount`, `PdfCount`, `DwgCount`, `IssueCount` |
| 1 | Expander: Estrutura de nomenclatura | `NamingParts` (ItemsControl), `NamingSeparator`, botões ± campo, Aplicar |
| 2 | Barra de filtro | `SearchText`, `ShowOnlyIssues`, `VisibleSheetCount`, botões PDF/DWG todas/nenhuma |
| 3 | DataGrid principal | `SheetsView` (ICollectionView), colunas: PDF☑, DWG☑, #, Formato, Nome, Situação, Zoom |
| 4 | Configurações de saída | `OutputFolder`, `DeviceName`, `CtbName`, `OverwriteExisting` |
| 5 | Expander: Copiar nome para selo | `StampBlockNames`, `SelectedStampBlock`, `StampAttributes`, `SelectedStampAttribute` |
| 6 | Botões finais + status | `IsBusy`, `StatusMessage`, botões Salvar/Plotar |
| overlay | Overlay de loading | Cobre toda a janela quando `IsBusy = true` |

### 4.2 PlotFolhasViewModel — Propriedades

| Binding | Tipo | Descrição |
|---------|------|-----------|
| `Sheets` | `ObservableCollection<FolhaInfo>` | Lista completa de folhas |
| `SheetsView` | `ICollectionView` | View filtrada por SearchText e ShowOnlyIssues |
| `SearchText` | `string` | Filtro de texto (nome, formato, status, sequência) |
| `ShowOnlyIssues` | `bool` | Mostra só folhas com erro ou aviso |
| `OutputFolder` | `string` | Pasta de saída |
| `UseAutomaticEmissionFolder` | `bool` (ro) | true se usuário nunca alterou a pasta |
| `AutomaticEmissionBaseFolder` | `string` | Pasta base para subpastas Emissão NN |
| `DeviceName` | `string` | Plotter selecionado |
| `CtbName` | `string` | CTB/STB selecionado |
| `OverwriteExisting` | `bool` | Sobrescrever arquivos existentes |
| `NamingSeparator` | `string` | Separador (1 char) entre partes do nome |
| `NamingParts` | `ObservableCollection<NamingPartViewModel>` | Partes da estrutura de nome |
| `StampBlockNames` | `ObservableCollection<string>` | Blocos com atributos disponíveis |
| `StampAttributes` | `ObservableCollection<string>` | Atributos do bloco selecionado |
| `SelectedStampBlock` | `string` | Bloco de selo escolhido |
| `SelectedStampAttribute` | `string` | Atributo do selo escolhido |
| `SelectedSheet` | `FolhaInfo` | Linha selecionada no DataGrid |
| `IsBusy` | `bool` | Controla overlay de loading |
| `StatusMessage` | `string` | Mensagem na barra de status |
| `TotalSheetCount`, `PdfCount`, `DwgCount`, `IssueCount`, `VisibleSheetCount` | `int` | Cards de resumo |

### 4.3 NamingPartViewModel

```csharp
internal sealed class NamingPartViewModel : INotifyPropertyChanged
{
    int Position { get; }          // 1-based
    string Value { get; set; }     // texto do campo (max 6 chars)
    bool IsSequential { get; set; } // se true, incrementa numeração por folha
}
```

### 4.4 Eventos expostos pelo Window

Todos os eventos são `EventHandler`. O Handler assina e processa:

```csharp
event EventHandler ApplyStructuredNameRequested;  // botão "Aplicar a todas"
event EventHandler FileNameEdited;                 // edição na coluna "Nome do arquivo"
event EventHandler ZoomRequested;                  // botão "Zoom" na linha
event EventHandler SaveNamesRequested;             // botão "Salvar nomenclatura"
event EventHandler PlotRequested;                  // botão "Gerar arquivos"
event EventHandler StampBlockChanged;              // seleção de bloco de selo mudou
event EventHandler RefreshRequested;               // botão "Atualizar"
```

### 4.5 Comportamentos de foco

- `PreviewMouseLeftButtonDown` na Window limpa `Keyboard.ClearFocus()` ao clicar fora de controles interativos
- Exceto para: `TextBox`, `ComboBox`, `ComboBoxItem`, `CheckBox`, `Button`, `DataGridCell`
- `CommitChanges()` no Window força commit do DataGrid e limpa foco

## 5. Serviços — responsabilidades e contratos

### 5.1 FolhaNomenclaturaService

Salva e carrega nomes no atributo `CNT_NOME_ARQUIVO` dos blocos CEP-*.

**IMPORTANTE**:
- Ao **salvar**, o nome é armazenado **sem extensão `.pdf`** (`Path.GetFileNameWithoutExtension`)
- Ao **carregar**, a extensão `.pdf` é adicionada de volta
- Na primeira execução, cria automaticamente um `AttributeDefinition` na definição do bloco CEP-* e um `AttributeReference` na instância
- O atributo é sempre invisível (`Invisible = true`)

```csharp
void LoadSavedNames(Document, IEnumerable<FolhaInfo>)  // carrega do DWG
int SaveNames(Document, IEnumerable<FolhaInfo>)         // salva no DWG, retorna count
```

### 5.2 SeloBlockService

Gerencia o preenchimento de atributos de blocos de selo.

**Regras**:
- `GetBlockNames()` escaneia a BlockTable e filtra blocos que têm `AttributeDefinition`
- `GetAttributeTags(blockName)` retorna tags do bloco selecionado (recursivo, blocos aninhados)
- `FillSeloAttributes(sheets, blockName, tag)` para cada folha:
  1. Busca o bloco de selo no **Paper Space** (não dentro do bloco CEP)
  2. Usa interseção geométrica com os limites da folha (maior área de sobreposição)
  3. Preenche o atributo com `Path.GetFileNameWithoutExtension(sheet.NomeArquivo)`
  4. Chama `RecordGraphicsModified(true)` no bloco de selo
  5. Após o commit, executa `_.ATTSYNC N nomeDoBloco` para sincronizar

```csharp
IReadOnlyList<string> GetBlockNames()
IReadOnlyList<string> GetAttributeTags(string blockName)
int FillSeloAttributes(IReadOnlyList<FolhaInfo> sheets, string blockName, string tag)
```

### 5.3 PlotFolhasSessionService

Cria a sessão completa a partir do documento ativo.

```csharp
PlotFolhasSession Create()  // retorna sessão com folhas, dispositivos, nomes, etc.
```

### 5.4 PlotFolhasNamingService

Aplica estrutura de nomenclatura com suporte a campos sequenciais.

```csharp
void ApplyStructure(sheets, separator, parts, sequentialFlags)
void NormalizeEditedName(editedSheet, allSheets)
PlotFolhasNameValidation NormalizeAndValidate(sheets)
int Save(sheets)
```

### 5.5 PlotFolhasGenerationService

Prepara e executa a geração de arquivos.

```csharp
PlotFolhasGenerationPreparation Prepare(sheets, folder, device, useAutoEmission, baseFolder)
PlotExecutionResult Execute(preparation, folder, device, ctb, overwrite, progress)
string TryOpenOutputFolder(folder)
```

### 5.6 FolhaScanner

Escaneia o layout ativo por blocos CEP-*.

**Regras**:
- Só roda em Layout (não Model)
- Reconhece blocos: CEP-A4, CEP-A3, CEP-A2, CEP-A1, CEP-A0, CEP-A1E, CEP-A0E
- Suporta blocos dinâmicos (`DynamicBlockTableRecord`)
- Busca limites na layer `502-CEP-FOR-06` (recursivo, até 8 níveis)
- Fallback: ponto de inserção + dimensões do formato
- Último fallback: `GeometricExtents`
- Valida escala 1:1, rotação 90°, dimensões, sobreposição
- Ordena: cima→baixo (Y desc), esquerda→direita (X asc), tolerância 10mm para mesma linha

### 5.7 Demais serviços

| Serviço | Responsabilidade |
|---------|-----------------|
| `ArquivoNomeService` | Sanitiza partes, valida nomes, constrói nome estruturado. **Nomes sempre terminam com `.pdf`** |
| `PlotService` | Lista dispositivos/CTBs, executa plotagem PDF folha a folha |
| `PlotExecutionService` | Orquestra SaveNames → Plot PDF → Export DWG |
| `DwgExportService` | Exporta DWG individual com isolamento de Model por viewport |
| `SheetZoomService` | Navega/Zoom para os limites de uma folha |
| `FolhaFormatCatalog` | Catálogo estático de formatos e dimensões |
| `NamingStandardParser` | Detecta separador e parseia nome em partes |
| `PlotOutputPlan` | Modelo que separa folhas selecionadas em PdfSheets e DwgSheets |
| `PlotFolhasGenerationPreparation` | Resultado da preparação (válido ou erro) |

## 6. Padrões e convenções

### 6.1 Comunicação Window ↔ Handler

- A Window **nunca** chama serviços diretamente
- A Window expõe **eventos** que o Handler assina
- O Handler lê propriedades da Window e chama serviços
- O Handler atualiza a Window via métodos públicos (`SetBusy`, `SetStatusMessage`, `RefreshSheets`, etc.)

### 6.2 ViewModel

- Implementa `INotifyPropertyChanged`
- Usa `SetField<T>()` genérico para evitar boilerplate
- `ObservableCollection<T>` para listas bindáveis
- `ICollectionView` para filtro da DataGrid
- `NamingPartViewModel` é uma classe interna para as partes da estrutura de nome

### 6.3 Segurança com o ZWCAD

- Sempre validar `ActiveDocument != null` antes de operar
- Usar `DocumentLock` para escritas a partir de janela modeless
- Usar `Transaction` com `OpenMode.ForRead` por padrão, promover para `ForWrite` só quando necessário
- Usar `Commit()` apenas após toda a operação válida
- `using` para locks, transações e objetos descartáveis
- Janela modeless vinculada ao documento de origem (fecha se trocar/fechar DWG)

### 6.4 Nomenclatura e estilo

- C# 7.3, .NET Framework 4.8, WPF
- Sem comentários desnecessários
- Nomes expressam intenção
- Métodos pequenos com um nível de abstração coerente
- Dependências por construtor, validadas com `ArgumentNullException`
- Retornos antecipados preferidos a aninhamento profundo

### 6.5 Como adicionar uma nova funcionalidade ao form

1. **Adicionar binding no ViewModel** — propriedade com `SetField<T>()` + `INotifyPropertyChanged`
2. **Adicionar controle no XAML** — manter a paleta de cores (`AccentBrush: #F08F38`, `BorderBrush: #C0C0C0`)
3. **Se precisar de evento**, adicionar `EventHandler` na Window e assinar no Handler
4. **Se precisar de serviço**, criar em `UI/Services/` e compor no construtor do Handler
5. **Adicionar .cs ao .csproj** — projetos clássicos exigem inclusão explícita
6. **Nunca alterar** `StarterApplication.cs`, `RibbonHost.cs` ou a infraestrutura central

### 6.6 Paleta de cores

```xml
<SolidColorBrush x:Key="AccentBrush" Color="#F08F38" />      <!-- Laranja principal -->
<SolidColorBrush x:Key="AccentHoverBrush" Color="#FF9F40" />  <!-- Hover vibrante -->
<SolidColorBrush x:Key="TextBrush" Color="#243038" />          <!-- Texto escuro -->
<SolidColorBrush x:Key="MutedBrush" Color="#65747D" />         <!-- Texto secundário -->
<SolidColorBrush x:Key="BorderBrush" Color="#C0C0C0" />        <!-- Bordas -->
```

- Botões: fundo branco, hover laranja com texto branco
- Botão primário (`PrimaryButton`): fundo laranja, texto branco, hover laranja vibrante

## 7. Regras de negócio críticas

### 7.1 Nomes de arquivo

- Internamente sempre terminam com `.pdf` (validado por `ArquivoNomeService`)
- Atributo `CNT_NOME_ARQUIVO` salva **sem** `.pdf` (para limpeza visual no DWG)
- Atributo do selo também preenche **sem** `.pdf`
- Ao carregar de volta, `.pdf` é readicionado

### 7.2 Campos sequenciais

- `NamingPartViewModel.IsSequential = true` ativa numeração
- Aplica-se apenas ao clicar "Aplicar a todas"
- Suporta prefixo alfanumérico: `"A001"` → `"A001"`, `"A002"`, `"A003"`
- Padding preservado: `"01"` → `"01"`, `"02"`... `"99"`
- Se valor não for numérico, mantém o texto fixo

### 7.3 Preenchimento do selo

- Ocorre tanto em "Salvar nomenclatura" quanto em "Gerar arquivos"
- Busca o bloco de selo no Paper Space por interseção geométrica com a folha
- Pega o bloco com maior área de sobreposição
- Após preencher, chama `ATTSYNC N nomeDoBloco` para sincronizar

## 8. Checklist para agentes

Antes de entregar qualquer modificação no form ou no módulo:

- [ ] Nenhum serviço foi chamado diretamente da Window ou ViewModel
- [ ] Novos `.cs` foram incluídos no `.csproj`
- [ ] Cores seguem a paleta definida
- [ ] Hover dos botões funciona (estilo global `TargetType="Button"`)
- [ ] Foco é liberado ao clicar fora de controles
- [ ] Janela fecha ao trocar/fechar documento
- [ ] Eventos da Window são removidos no `Closed`
- [ ] `DocumentLock` e `Transaction` usados corretamente para escritas
- [ ] Nomes sem `.pdf` nos atributos, com `.pdf` internamente
- [ ] Compila com MSBuild x64 sem erros
- [ ] Testado: abrir form, preencher nomes, salvar nomenclatura, recarregar (Atualizar), gerar arquivos
