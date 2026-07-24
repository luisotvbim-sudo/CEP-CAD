# Contexto — comando `CNT_PLOT_FOLHAS`

## Finalidade

O comando **`CNT_PLOT_FOLHAS`** localiza as folhas presentes no **Model ou layout ativo** do ZWCAD, permite definir o nome de saída de cada uma e gera arquivos **PDF**, **DWG** ou ambos individualmente.

Ele está disponível na Ribbon em **CNT > Plotagem > Plotar folhas**.

## Como funciona

1. Ative o Model ou o Layout que contém as folhas e execute `CNT_PLOT_FOLHAS`.
2. O comando procura blocos de folha reconhecidos somente no espaço ativo e os ordena visualmente: de cima para baixo e, na mesma linha, da esquerda para a direita.
3. A janela indica a origem da sessão no seletor informativo `MODEL | LAYOUT` e lista as folhas encontradas. Por padrão, todas vêm marcadas para gerar PDF; DWG é opcional.
4. Informe/ajuste o nome de arquivo de cada folha, a pasta de saída, a impressora PDF e, se necessário, o arquivo CTB.
5. Ao plotar, o comando salva a nomenclatura nos blocos, gera os arquivos selecionados e abre a pasta de saída.

## O que o desenho CAD precisa ter

### Espaço e blocos de folha

- A execução pode ocorrer no **Model** ou em uma **aba de Layout**.
- As folhas devem estar inseridas diretamente no espaço ativo como referências de bloco.
- O nome efetivo do bloco deve ser exatamente um dos nomes abaixo (sem complementos):

  | Nome do bloco | Formato (mm) |
  |---|---:|
  | `CEP-A4` | 210 × 297 |
  | `CEP-A3` | 297 × 420 |
  | `CEP-A2` | 420 × 594 |
  | `CEP-A1` | 594 × 841 |
  | `CEP-A0` | 841 × 1189 |
  | `CEP-A1E` | 594 × 1189 |
  | `CEP-A0E` | 841 × 1408 |

- No Layout, o bloco deve estar em escala **1:1**. No Model, pode usar qualquer escala **uniforme em X/Y**; essa escala é aplicada automaticamente à plotagem.
- A rotação deve ser múltipla de **90°** (0°, 90°, 180° ou 270°).
- As dimensões da moldura devem corresponder ao formato informado no nome do bloco, em milímetros (com pequena tolerância técnica).
- As folhas não podem se sobrepor. Sobreposições bloqueiam a geração.

### Limite de plotagem recomendado

- Dentro do bloco de folha, desenhe o contorno que define a área de impressão na layer **`502-CEP-FOR-06`**.
- O comando usa a extensão dos objetos dessa layer como janela de plotagem. A layer pode estar dentro de blocos aninhados.
- Se ela não existir, o comando estima a janela com base no tamanho do formato; isso é permitido, mas aparece como aviso e pode não recortar exatamente como esperado.

### Nome dos arquivos

- Cada folha selecionada precisa ter um nome de arquivo **único** e com extensão **`.pdf`**.
- Não use caracteres inválidos para nome de arquivo do Windows (`\\ / : * ? \" < > |`) e mantenha o nome com até 180 caracteres.
- O nome-base sugerido é o nome do DWG atual. Ao aplicá-lo a todas as folhas, revise os nomes: arquivos repetidos são inválidos e impedem a geração.
- Os nomes são gravados automaticamente em um atributo invisível chamado **`CNT_NOME_ARQUIVO`** em cada bloco de folha. Não é necessário criar esse atributo previamente.

### Sincronização com um atributo de selo

- O painel **Copiar nome para selo** permite escolher qualquer bloco nomeado que tenha atributos; ele não exige que o nome contenha `SELO`.
- Para cada folha, o comando procura no mesmo espaço da sessão o bloco com o mesmo nome efetivo e escolhe a referência com maior área de interseção com os limites da folha.
- Ao salvar a nomenclatura ou gerar arquivos, o nome da folha é escrito no atributo selecionado, sem a extensão `.pdf`.
- O botão **Usar atributo nos nomes** executa o sentido inverso: lê esse atributo na referência correspondente de cada folha e coloca o valor na coluna **Nome do arquivo**.
- Depois da leitura, os nomes são normalizados para `.pdf` e passam pelas validações existentes de vazio, caracteres inválidos e duplicidade.
- Se uma folha não contiver o bloco, não possuir o atributo escolhido ou tiver o atributo vazio, seu nome atual não é substituído.

### Para gerar PDF

- Selecione uma impressora/plotter PDF disponível no ZWCAD.
- Se desejado, selecione um CTB disponível. Sem CTB, o comando usa a configuração aplicável ao espaço ativo.
- Escolha uma pasta de saída com permissão de escrita. A pasta do DWG é sugerida automaticamente quando o desenho já foi salvo.
- Se a pasta sugerida não for alterada pelo usuário, o comando cria uma subpasta incremental `Emissão 01`, `Emissão 02`, `Emissão 03` e assim por diante. Se o usuário escolher outra pasta, ela é usada diretamente.

### Para gerar DWG por folha

- A exportação cria uma cópia do desenho ativo em memória; alterações confirmadas ainda não salvas também entram no DWG individual.
- O DWG de saída recebe o mesmo nome da folha, trocando `.pdf` por `.dwg`.
- Para folhas de Layout, a exportação mantém no Layout apenas o conteúdo da folha e suas viewports. No Model, usa a união das regiões vistas por essas viewports e conserva somente o conteúdo visível.
- Para folhas encontradas diretamente no Model, a exportação não usa viewports: mantém o bloco da folha selecionada e qualquer entidade que intercepte seus limites. Linhas, blocos, Xrefs e demais entidades são preservados inteiros, sem `TRIM`, `EXPLODE` ou recorte geométrico.
- A visibilidade também respeita `Entity.Visible`, layer globalmente desligada/congelada e layers congeladas por viewport. Uma entidade só usa a região de uma viewport quando sua layer está visível nela.
- Antes de apagar ou recortar, a cópia em memória registra `IsOff`, `IsFrozen` e `IsLocked` de todas as layers, destrava/descongela temporariamente e restaura exatamente os estados originais antes do `Commit`. A visibilidade é sempre decidida pelo estado registrado, não pelo estado temporário de edição.

## Validações e comportamentos importantes

- Apenas o **Model ou layout ativo no momento da abertura/atualização** é mapeado. O seletor `MODEL | LAYOUT` é informativo; para trocar a origem, ative a aba desejada e clique em **Atualizar**.
- Se não houver blocos reconhecidos, nenhuma folha será listada.
- Erros de formato, escala, rotação, sobreposição ou nome impedem a geração da respectiva folha.
- É possível gerar somente PDF, somente DWG ou ambos, marcando as opções da folha.
- Caso já existam arquivos com os mesmos nomes na pasta de saída, o comando solicita confirmação para sobrescrevê-los.
- O botão **Zoom** serve para conferir visualmente a janela calculada para a folha selecionada antes de gerar os arquivos.

## Checklist rápido antes de usar

- [ ] Model ou Layout correto ativo.
- [ ] Blocos `CEP-*` inseridos diretamente no espaço ativo.
- [ ] Blocos em 1:1 no Layout ou escala X/Y uniforme no Model, rotação de 90° e tamanho correspondente ao formato.
- [ ] Contorno de impressão na layer `502-CEP-FOR-06`.
- [ ] Folhas sem sobreposição.
- [ ] Nomes de PDF únicos para as folhas selecionadas.
- [ ] Plotter PDF, CTB e pasta de saída definidos.
