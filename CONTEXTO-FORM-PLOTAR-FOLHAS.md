# Contexto — Formulário `PlotFolhasWindow` e arquitetura do módulo PlotFolhas

Este documento descreve a arquitetura interna do formulário WPF e dos serviços do módulo `PlotFolhas`. Ele é o contrato técnico para agentes ou desenvolvedores que precisarem evoluir o código sem quebrar a estrutura existente.

## 1. Visão geral da arquitetura

```
Usuário (Ribbon/Console)
    ↓
PlotFolhasCommand.cs        → [CommandMethod] + [CntRibbonCommand] — adaptador fino
    ↓
PlotFolhasModule.cs         → ICntModule
    ↓
PlotFolhasCompositionRoot.cs → composição explícita das dependências
    ↓
PlotFolhasHandler.cs        → orquestrador — abre sessão/janela e assina eventos
    ↓
PlotFolhasWindow.xaml/.cs   → UI WPF modeless — DataGrid + controles
    ↓ (eventos)
Workflows especializados    → nomes/selo, zoom e geração
    ↓
Serviços especializados     → regras de negócio, acesso ao ZWCAD, geração de arquivos
```

## 2. Estrutura de diretórios do módulo

```
03-Modules/PlotFolhas/
├── Bootstrap/
│   ├── PlotFolhasCommand.cs      # Atributos + delegação
│   ├── PlotFolhasModule.cs       # ICntModule
│   └── PlotFolhasCompositionRoot.cs # Compõe serviços e handler
├── Discovery/
│   ├── FolhaScanner.cs           # Escaneia folhas no layout (CEP-*)
│   ├── FolhaBoundaryResolver.cs  # Resolve limites e transformações
│   └── FolhaValidationService.cs # Valida formato, escala e sobreposição
├── Domain/
│   ├── FolhaFormatCatalog.cs     # Catálogo de formatos CEP-A4...CEP-A0E
│   └── FolhaInfo.cs              # Modelo da folha (INotifyPropertyChanged)
├── Naming/
│   ├── ArquivoNomeService.cs     # Sanitização e validação de nomes
│   ├── FolhaNomenclaturaService.cs # Leitura/escrita do atributo CNT_NOME_ARQUIVO
│   ├── FolhaNameAttributeStore.cs # Persistência de baixo nível do atributo
│   ├── NamingStandardParser.cs   # Parser de separadores
│   └── ParsedName.cs             # Modelo de nome parseado
├── Plotting/
│   ├── PlotExecutionService.cs   # Executa plotagem PDF + export DWG
│   ├── PlotOutputPlan.cs         # Plano de saída (PDFs, DWGs, arquivos existentes)
│   ├── PlotService.cs            # Plotagem PDF via API ZWCAD
│   └── PlotSettingsConfigurator.cs # Configura PlotSettings
├── Export/
│   ├── DwgExportService.cs       # Orquestra o lote de DWGs
│   ├── DwgSheetExportService.cs  # Exporta uma única folha
│   ├── Infrastructure/           # Operações CAD compartilhadas
│   ├── LayoutIsolation/          # Isolamento do Paper Space e vista inicial
│   └── ModelIsolation/           # Isolamento do Model por viewports
├── Navigation/
│   └── SheetZoomService.cs       # Zoom para uma folha específica
└── UI/
    ├── PlotFolhasHandler.cs      # Ciclo de vida da janela
    ├── PlotFolhasViewModel.cs    # Estado geral da janela
    ├── PlotSheetCollectionViewModel.cs # Filtro e resumo das folhas
    ├── NamingStructureViewModel.cs # Estrutura de nomenclatura
    ├── PlotOutputOptionsViewModel.cs # Opções de saída
    ├── StampSelectionViewModel.cs # Seleção de selo/atributo
    ├── PlotFolhasWindow.xaml     # Layout WPF
    ├── PlotFolhasWindow.xaml.cs  # Code-behind
    ├── Services/
        ├── PlotFolhasSessionService.cs   # Cria sessão (scan + nomes + dispositivos)
        ├── PlotFolhasNamingService.cs    # Aplica estrutura, normaliza, valida
        ├── PlotFolhasGenerationService.cs # Prepara e executa geração de arquivos
        ├── OutputFolderService.cs          # Prepara e abre pastas de saída
        ├── SeloBlockService.cs           # Fachada do recurso de selo
        ├── SeloBlockCatalog.cs           # Catálogo de blocos
        ├── BlockAttributeCatalog.cs      # Busca recursiva de atributos
        ├── StampBlockLocator.cs          # Escolhe selo pela maior sobreposição
        └── SeloAttributeWriter.cs        # Escreve e sincroniza atributos
    └── Workflows/
        ├── PlotFolhasNamingWorkflow.cs
        ├── PlotFolhasZoomWorkflow.cs
        ├── PlotFolhasGenerationWorkflow.cs
        └── PlotFolhasGenerationRunner.cs
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
| `ApplyStructuredNameRequested` | `PlotFolhasNamingWorkflow` | Aplica estrutura com suporte a campos sequenciais |
| `FileNameEdited` | `PlotFolhasNamingWorkflow` | Normaliza o nome editado |
| `ZoomRequested` | `PlotFolhasZoomWorkflow` | Executa `SheetZoomService.ZoomTo()` |
| `SaveNamesRequested` | `PlotFolhasNamingWorkflow` | Salva nomes + preenche selo (se configurado) |
| `PlotRequested` | `PlotFolhasGenerationWorkflow` | Valida, prepara e confirma a sobrescrita |
| `StampBlockChanged` | `PlotFolhasNamingWorkflow` | Carrega tags pelo `SeloBlockService` |
| `RefreshRequested` | `OnRefreshRequested` | Recria sessão e reabre janela |

### 3.4 Geração de arquivos

```
PlotFolhasGenerationWorkflow.Run()
  → window.CommitChanges()
  → NamingService.NormalizeAndValidate()
  → GenerationService.Prepare()
    → PlotOutputPlan.Create()         // separa PDFs e DWGs
    → Valida: folhas selecionadas, erros, plotter, pasta
    → Cria pasta (ou subpasta Emissão NN)
    → Retorna PlotFolhasGenerationPreparation
  → Confirma sobrescrita se necessário
  → PlotFolhasGenerationRunner.Run()
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
| 0 | Título + cards de resumo + botão Atualizar | `SheetCollection.TotalCount/PdfCount/DwgCount/IssueCount` |
| 1 | Expander: Estrutura de nomenclatura | `NamingStructure.Parts`, `NamingStructure.Separator`, botões ± campo, Aplicar |
| 2 | Barra de filtro | `SheetCollection.SearchText/ShowOnlyIssues/VisibleCount`, botões PDF/DWG |
| 3 | DataGrid principal | `SheetCollection.View`, colunas: PDF☑, DWG☑, #, Formato, Nome, Situação, Zoom |
| 4 | Configurações de saída | `Output.OutputFolder/DeviceName/CtbName/OverwriteExisting` |
| 5 | Expander: Copiar nome para selo | `StampSelection.BlockNames/SelectedBlock/Attributes/SelectedAttribute` |
| 6 | Botões finais + status | `IsBusy`, `StatusMessage`, botões Salvar/Plotar |
| overlay | Overlay de loading | Cobre toda a janela quando `IsBusy = true` |

### 4.2 ViewModels compostos

| ViewModel | Responsabilidade |
|-----------|------------------|
| `PlotFolhasViewModel` | Estado global: seleção, busy e mensagem de status |
| `PlotSheetCollectionViewModel` | Folhas, filtro, seleção em massa e contadores |
| `NamingStructureViewModel` | Separador, partes e flags sequenciais |
| `PlotOutputOptionsViewModel` | Pasta, emissão automática, plotter, CTB e sobrescrita |
| `StampSelectionViewModel` | Blocos de selo, atributos e seleções atuais |

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
As operações de baixo nível sobre `AttributeDefinition` e `AttributeReference` ficam em
`FolhaNameAttributeStore`; o serviço coordena documento, lock, transação e folhas.

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

É a fachada do preenchimento de atributos de blocos de selo. A implementação é dividida em:

- `SeloBlockCatalog`: lista definições elegíveis;
- `BlockAttributeCatalog`: percorre definições aninhadas sem repetir ciclos;
- `StampBlockLocator`: escolhe a referência com maior sobreposição com a folha;
- `SeloAttributeWriter`: grava valores e solicita o `ATTSYNC`.

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

### 5.6 Descoberta e validação de folhas

`FolhaScanner` escaneia o layout ativo por blocos CEP-*. O cálculo de limites fica em `FolhaBoundaryResolver`; as validações ficam em `FolhaValidationService`.

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
| `DwgExportService` | Orquestra o lote e delega cada folha ao `DwgSheetExportService` |
| `DwgSheetExportService` | Clona o banco, isola Layout/Model, prepara a vista e publica o arquivo |
| `SheetZoomService` | Navega/Zoom para os limites de uma folha |
| `FolhaFormatCatalog` | Catálogo estático de formatos e dimensões |
| `NamingStandardParser` | Detecta separador e parseia nome em partes |
| `PlotOutputPlan` | Modelo que separa folhas selecionadas em PdfSheets e DwgSheets |
| `PlotFolhasGenerationPreparation` | Resultado da preparação (válido ou erro) |

## 6. Padrões e convenções

### 6.1 Comunicação Window ↔ Handler

- A Window **nunca** chama serviços diretamente
- A Window expõe **eventos** que o Handler assina
- O Handler cuida do ciclo de vida e delega os eventos aos workflows
- Os workflows leem propriedades e atualizam a Window por métodos públicos

### 6.2 ViewModel

- Os ViewModels herdam `ObservableObject`, compartilhado em `02-Application/Presentation`
- Cada ViewModel representa um grupo coeso de bindings
- `ObservableCollection<T>` para listas bindáveis
- `ICollectionView` para filtro da DataGrid
- `NamingPartViewModel` representa uma parte da estrutura de nome

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

1. **Adicionar binding no ViewModel responsável** — folha, nomenclatura, saída, selo ou estado global
2. **Adicionar controle no XAML** — manter a paleta de cores (`AccentBrush: #F08F38`, `BorderBrush: #C0C0C0`)
3. **Se precisar de evento**, adicionar `EventHandler` na Window e delegar pelo Handler
4. **Se for um fluxo de UI**, criar/estender um workflow; acesso CAD reutilizável fica em `UI/Services/`
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

### 7.4 Exportação DWG isolada

Fluxo por folha:

1. `DwgDatabaseCloner` cria um banco independente com `Wblock()`;
2. `DwgLayoutIsolator` mantém a folha, entidades pertencentes a ela e suas viewports;
3. `ViewportModelIsolator` calcula um plano antes de apagar qualquer entidade do Model;
4. `DwgOpeningViewService` deixa o Layout ativo e centralizado na folha;
5. `DwgOutputFile` salva em temporário, publica no destino e verifica o resultado.

Política obrigatória do Model:

- nenhuma viewport de Model na folha: apagar todo o Model;
- uma ou mais viewports válidas: manter entidades cujos limites intersectem as regiões projetadas;
- viewports válidas, mas nenhuma entidade encontrada: preservar o Model integralmente por segurança;
- entidade sem `GeometricExtents`: preservar a entidade;
- viewport em perspectiva ou transformação inválida: cancelar antes de alterar o Model clonado.

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
