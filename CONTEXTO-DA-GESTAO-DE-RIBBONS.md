# Contexto da Gestão de Ribbons

## 1. Objetivo

Este documento registra a arquitetura proposta para criação e gestão modular de comandos, abas, painéis e botões da Ribbon do ZWCAD.

O objetivo é permitir que um comando novo declare sua própria apresentação na Ribbon, sem exigir alterações manuais no código central que constrói a interface.

Um comando novo muda a Ribbon visualmente. O que não deve mudar é a implementação central do RibbonHost.

## 2. Problema que estamos resolvendo

Na implementação tradicional, um comando costuma ser cadastrado em mais de um lugar:

1. Método com CommandMethod.
2. Catálogo central da Ribbon.
3. Configuração da aba e do painel.
4. Associação de texto, ícone e posição.
5. Inicialização dos serviços usados pelo comando.

Isso duplica informações e permite inconsistências. O comando pode existir sem botão, o botão pode apontar para um nome incorreto ou a mesma aba pode ser criada mais de uma vez.

A arquitetura deve:

- manter comando e metadados da Ribbon próximos;
- descobrir comandos automaticamente;
- evitar um catálogo central atualizado manualmente;
- gerar uma única DLL para instalação;
- organizar cada funcionalidade de forma fácil para pessoas e ferramentas de IA;
- compartilhar somente serviços realmente comuns;
- isolar UI, regras e recursos específicos.

Na prática, trata-se de uma arquitetura de módulos internos, semelhante a plugins, mas compilada em uma única DLL.

## 3. Decisão arquitetural

O plugin terá inicialmente:

- um único projeto .csproj;
- uma única DLL carregada pelo ZWCAD;
- módulos lógicos organizados por funcionalidade;
- serviços compartilhados;
- uma camada de aplicação responsável pela integração com o ZWCAD;
- contratos pequenos para descoberta e descrição da Ribbon.

Cada comando não será um projeto independente. Ele será uma funcionalidade independente dentro da mesma DLL.

Projetos separados só devem ser considerados quando houver necessidade real de implantação, versionamento ou distribuição independente.

## 4. Estrutura proposta

~~~text
PluginConceito/
├── 01-Services/
│   ├── Configuration/
│   ├── Logging/
│   └── serviços realmente compartilhados
├── 02-Application/
│   ├── Bootstrap/
│   │   └── StarterApplication.cs
│   ├── Contracts/
│   │   ├── ICntModule.cs
│   │   ├── IModuleContext.cs
│   │   └── CntRibbonCommandAttribute.cs
│   ├── Ribbon/
│   │   ├── RibbonDiscovery.cs
│   │   ├── RibbonValidator.cs
│   │   └── RibbonHost.cs
│   ├── Telemetry/
│   └── Zwcad/
│       ├── ZwcadContext.cs
│       └── ZwcadCommandDispatcher.cs
├── 03-Modules/
│   ├── LayerInfo/
│   │   ├── LayerInfoModule.cs
│   │   ├── LayerInfoCommand.cs
│   │   ├── LayerInfoService.cs
│   │   ├── LayerInfoWindow.xaml
│   │   └── Resources/layer-info.png
│   └── Documentation/
│       ├── DocumentationModule.cs
│       ├── DocumentationCommand.cs
│       ├── DocumentationService.cs
│       ├── DocumentationPanel.xaml
│       └── Resources/
└── PluginConceito.csproj
~~~

Os nomes atuais 01-Sevices e 02-Aplication podem ser normalizados futuramente para 01-Services e 02-Application. Essa correção não é necessária para validar o conceito.

## 5. Responsabilidades

### Services

Contém serviços usados por mais de um módulo, como configuração, logging, persistência comum e integrações compartilhadas.

Um serviço usado por apenas uma funcionalidade permanece na pasta do módulo. Services não conhece Ribbon, comandos concretos ou janelas específicas.

### Application

Contém inicialização, descoberta dos módulos, Ribbon, telemetria, palettes, documento ativo, envio de comandos e demais integrações com o ZWCAD.

Application conhece os contratos, mas não conhece módulos concretos como LayerInfo ou Documentation.

### Modules

Cada pasta representa uma funcionalidade completa. Ela pode conter comando, composição das dependências, serviço específico, handler, UI, modelos e recursos.

Essa organização mantém todo o contexto necessário para alterar um comando no mesmo local.

## 6. Contrato do módulo

O módulo inicializa sua funcionalidade. Seu construtor deve ser simples e não produzir efeitos colaterais.

~~~csharp
public interface ICntModule
{
    string Id { get; }

    void Initialize(IModuleContext context);
}
~~~

O contexto expõe somente dependências compartilhadas aprovadas:

~~~csharp
public interface IModuleContext
{
    ITelemetry Telemetry { get; }

    IZwcadContext Zwcad { get; }

    IServiceProvider Services { get; }
}
~~~

Não é necessário criar uma interface para cada classe. Interfaces representam fronteiras importantes ou pontos em que testes e substituição tenham benefício real.

## 7. Metadados declarativos

Para botões comuns, a configuração da Ribbon fica no próprio método de comando por meio de um atributo.

~~~csharp
public sealed class CntRibbonCommandAttribute : Attribute
{
    public CntRibbonCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }

    public string CommandName { get; }
    public string ButtonId { get; set; }
    public string DisplayName { get; set; }
    public string TabId { get; set; }
    public string TabTitle { get; set; }
    public string PanelId { get; set; }
    public string PanelTitle { get; set; }
    public string IconResource { get; set; }
    public string ToolTip { get; set; }
    public int Order { get; set; }
    public RibbonItemSize Size { get; set; }
}
~~~

IDs e títulos têm funções diferentes:

- TabId e PanelId são identificadores estáveis;
- TabTitle e PanelTitle são textos apresentados;
- renomear um título não cria outra aba ou painel;
- CommandName é globalmente único;
- Order controla a posição sem depender da ordem da reflexão.

## 8. Exemplo de comando

~~~csharp
public sealed class LayerInfoCommand
{
    private const string CommandName = "CNT_LAYER_INFO";

    [CommandMethod(CommandName)]
    [CntRibbonCommand(
        CommandName,
        ButtonId = "CNT_LAYER_INFO_BUTTON",
        DisplayName = "Informações de camadas",
        TabId = "CNT_GERAL",
        TabTitle = "Geral",
        PanelId = "CNT_LAYERS",
        PanelTitle = "Camadas",
        IconResource = "03-Modules/LayerInfo/Resources/layer-info.png",
        Order = 10,
        Size = RibbonItemSize.Large)]
    public void Execute()
    {
        LayerInfoModule.Handler.Execute();
    }
}
~~~

A constante garante que CommandMethod e o atributo da Ribbon usem o mesmo nome.

O método do comando permanece pequeno: inicia a operação, chama o handler ou serviço e apresenta o resultado.

## 9. Descoberta automática

Como tudo está na mesma DLL, os módulos são descobertos no próprio assembly:

~~~csharp
Assembly assembly = typeof(StarterApplication).Assembly;

IReadOnlyList<ICntModule> modules = assembly
    .GetTypes()
    .Where(type =>
        typeof(ICntModule).IsAssignableFrom(type) &&
        !type.IsAbstract &&
        !type.IsInterface)
    .Select(type => (ICntModule)Activator.CreateInstance(type))
    .ToList();
~~~

Depois, RibbonDiscovery procura métodos com CntRibbonCommandAttribute, coleta seus metadados e entrega definições validadas ao RibbonHost.

Não é necessário usar Assembly.LoadFrom, carregar plugins externos ou unir DLLs com ILMerge ou Costura.

## 10. Fluxo de inicialização

~~~text
ZWCAD carrega PluginConceito.dll
    ↓
StarterApplication.Initialize()
    ↓
Descobre e inicializa ICntModule
    ↓
Descobre métodos com CntRibbonCommandAttribute
    ↓
Valida comandos e metadados
    ↓
Aguarda o RibbonControl
    ↓
Agrupa itens por TabId e PanelId
    ↓
Cria abas, painéis e botões
~~~

Ao clicar em um botão:

~~~text
RibbonButton
    ↓
ZwcadCommandDispatcher.SendStringToExecute()
    ↓
ZWCAD localiza o método com CommandMethod
    ↓
Comando chama o handler ou serviço do módulo
~~~

## 11. Responsabilidade do RibbonHost

O RibbonHost é genérico e deve:

- receber definições descobertas;
- criar ou reutilizar abas por TabId;
- criar ou reutilizar painéis por PanelId;
- ordenar itens por Order;
- carregar e manter ícones em cache;
- associar o clique ao nome do comando;
- evitar itens duplicados;
- registrar erros sem impedir os demais comandos;
- aguardar quando a Ribbon do ZWCAD ainda não estiver disponível.

O RibbonHost não contém referências a comandos concretos.

## 12. Como um comando altera a Ribbon

Um comando novo altera a Ribbon, mas a alteração acontece declarativamente.

No próprio comando são informados aba, painel, texto, ícone, posição e nome do comando executado.

O código central da Ribbon não é editado. Na inicialização seguinte, ele descobre os metadados e produz a nova interface.

> Um comando novo altera a Ribbon visual, mas não exige alteração manual na implementação central da Ribbon.

## 13. Controles complexos

O primeiro estágio deve priorizar botões grandes e pequenos. Toggle, menus, split buttons, separadores e agrupamentos devem ser adicionados quando existir um caso real.

Se o atributo ficar complexo demais, pode ser criado um contrato opcional:

~~~csharp
public interface IRibbonContributor
{
    IEnumerable<RibbonItemDefinition> GetRibbonItems();
}
~~~

Esse contrato complementa o atributo simples; não deve substituí-lo para todos os comandos.

## 14. UI e telemetria

A UI específica pertence ao módulo. Uma LayerInfoWindow, por exemplo, fica em 03-Modules/LayerInfo.

Application contém apenas hosts e adaptadores genéricos, como PaletteHost. Services não conhece controles WPF específicos.

A telemetria permanece centralizada em Application. O host pode observar início, sucesso, cancelamento e falha dos comandos com prefixo CNT_. O módulo emite eventos adicionais apenas para informações específicas de negócio.

## 15. Validações

Antes de criar a Ribbon, a aplicação verifica:

- CommandName duplicado;
- ButtonId duplicado;
- TabId ou PanelId vazio;
- títulos vazios;
- ícone inexistente;
- método sem CommandMethod correspondente;
- tipo de item não suportado;
- falha durante a inicialização de um módulo.

Uma falha em um módulo deve ser registrada e isolada sempre que possível.

## 16. Convenções para desenvolvimento com IA

1. Cada funcionalidade possui uma pasta em 03-Modules.
2. Pasta, módulo, comando e prefixo das classes usam o mesmo nome.
3. Os metadados da Ribbon ficam no arquivo do comando.
4. A UI e os serviços específicos ficam na pasta do módulo.
5. Um serviço só vai para 01-Services quando for compartilhado.
6. RibbonHost não é editado para cadastrar um botão comum.
7. O nome global do comando começa com CNT_.
8. IDs não dependem do texto apresentado.
9. Alterações em serviços compartilhados exigem análise dos consumidores.

Exemplo de solicitação:

> Altere somente o comando LayerInfo. Preserve os contratos públicos e não modifique o RibbonHost. O contexto específico está em 03-Modules/LayerInfo.

Uma busca por LayerInfo ou CNT_LAYER_INFO deve levar diretamente ao módulo completo.

## 17. Passos para adicionar um comando

1. Criar uma pasta em 03-Modules.
2. Criar ICntModule quando houver inicialização própria.
3. Criar o método com CommandMethod.
4. Adicionar CntRibbonCommandAttribute ao mesmo método.
5. Informar IDs estáveis de aba, painel e botão.
6. Adicionar ícone e UI dentro da pasta do módulo.
7. Manter o método de comando pequeno.
8. Executar as validações automatizadas.
9. Verificar no ZWCAD a criação, posição e execução do botão.

Não deve ser necessário editar um catálogo central da Ribbon.

## 18. Decisões evitadas

### Um projeto por comando

Foi evitado porque aumentaria DLLs, referências, configurações de build e pontos de falha na instalação.

### Junção posterior de DLLs

ILMerge, Costura e ferramentas semelhantes podem complicar WPF, ícones, depuração e descoberta de comandos.

### Catálogo central manual

Foi substituído por metadados declarados junto ao comando e descoberta por reflexão.

### Interfaces para todas as classes

Foi evitado para não criar abstrações sem benefício.

### Serviços globais para tudo

Serviços específicos permanecem no módulo. Somente comportamentos compartilhados pertencem a Services.

## 19. Critérios de sucesso

A arquitetura estará funcionando quando:

- o ZWCAD carregar uma única PluginConceito.dll;
- os módulos forem inicializados automaticamente;
- os comandos forem registrados pelo ZWCAD;
- a Ribbon for criada com base nos metadados encontrados;
- um comando novo adicionar seu botão sem alterar RibbonHost;
- uma funcionalidade puder ser localizada pela sua pasta;
- erros de configuração forem encontrados antes de produzir uma Ribbon inconsistente.

## 20. Resumo

~~~text
Uma DLL
    +
Uma funcionalidade por pasta
    +
ICntModule para inicialização
    +
CntRibbonCommandAttribute para apresentação
    +
RibbonHost genérico
    +
Serviços compartilhados somente quando necessário
~~~

Essa estrutura mantém a simplicidade operacional de uma DLL e a modularidade necessária para evolução assistida por IA.
