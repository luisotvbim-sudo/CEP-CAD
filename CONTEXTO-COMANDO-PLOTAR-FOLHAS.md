# Contexto — comando `CNT_PLOT_FOLHAS`

## Finalidade

O comando **`CNT_PLOT_FOLHAS`** localiza as folhas presentes no **layout ativo** do ZWCAD, permite definir o nome de saída de cada uma e gera arquivos **PDF**, **DWG** ou ambos individualmente.

Ele está disponível na Ribbon em **CNT > Plotagem > Plotar folhas**.

## Como funciona

1. Com uma aba de Layout ativa, execute `CNT_PLOT_FOLHAS`.
2. O comando procura, no Paper Space, blocos de folha reconhecidos e os ordena visualmente: de cima para baixo e, na mesma linha, da esquerda para a direita.
3. A janela lista as folhas encontradas. Por padrão, todas vêm marcadas para gerar PDF; DWG é opcional.
4. Informe/ajuste o nome de arquivo de cada folha, a pasta de saída, a impressora PDF e, se necessário, o arquivo CTB.
5. Ao plotar, o comando salva a nomenclatura nos blocos, gera os arquivos selecionados e abre a pasta de saída.

## O que o desenho CAD precisa ter

### Layout e blocos de folha

- A execução deve ocorrer em uma **aba de Layout**. O comando não funciona na aba **Model**.
- As folhas devem estar inseridas no **Paper Space** do layout ativo como referências de bloco.
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

- O bloco deve estar em escala **1:1** e com rotação múltipla de **90°** (0°, 90°, 180° ou 270°).
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
- Para cada folha, o comando procura no Paper Space o bloco com o mesmo nome efetivo e escolhe a referência com maior área de interseção com os limites da folha.
- Ao salvar a nomenclatura ou gerar arquivos, o nome da folha é escrito no atributo selecionado, sem a extensão `.pdf`.
- O botão **Usar atributo nos nomes** executa o sentido inverso: lê esse atributo na referência correspondente de cada folha e coloca o valor na coluna **Nome do arquivo**.
- Depois da leitura, os nomes são normalizados para `.pdf` e passam pelas validações existentes de vazio, caracteres inválidos e duplicidade.
- Se uma folha não contiver o bloco, não possuir o atributo escolhido ou tiver o atributo vazio, seu nome atual não é substituído.

### Para gerar PDF

- Selecione uma impressora/plotter PDF disponível no ZWCAD.
- Se desejado, selecione um CTB disponível. Sem CTB, o comando usa a configuração aplicável do layout.
- Escolha uma pasta de saída com permissão de escrita. A pasta do DWG é sugerida automaticamente quando o desenho já foi salvo.
- Se a pasta sugerida não for alterada pelo usuário, o comando cria uma subpasta incremental `Emissão 01`, `Emissão 02`, `Emissão 03` e assim por diante. Se o usuário escolher outra pasta, ela é usada diretamente.

### Para gerar DWG por folha

- A exportação cria uma cópia do desenho ativo em memória; alterações confirmadas ainda não salvas também entram no DWG individual.
- O DWG de saída recebe o mesmo nome da folha, trocando `.pdf` por `.dwg`.
- A exportação mantém, no Layout, apenas o conteúdo da folha e as viewports cujo centro pertence à área da folha.
- No Model, usa a união das regiões vistas pelas viewports. Blocos, Xrefs e entidades não derivadas de `Curve` permanecem inteiros quando aparecem ao menos parcialmente. Linhas, arcos, círculos, elipses, splines, polilinhas e demais `Curve` são divididos em todas as bordas e conservam somente os trechos visíveis. Sem viewport de Model válida, o Model é esvaziado.
- A visibilidade também respeita `Entity.Visible`, layer globalmente desligada/congelada e layers congeladas por viewport. Uma entidade só usa a região de uma viewport quando sua layer está visível nela.
- Antes de apagar ou recortar, a cópia em memória registra `IsOff`, `IsFrozen` e `IsLocked` de todas as layers, destrava/descongela temporariamente e restaura exatamente os estados originais antes do `Commit`. A visibilidade é sempre decidida pelo estado registrado, não pelo estado temporário de edição.

## Validações e comportamentos importantes

- Apenas o **layout ativo no momento da abertura** é mapeado. Para outro layout, ative-o e execute o comando novamente.
- Se não houver blocos reconhecidos, nenhuma folha será listada.
- Erros de formato, escala, rotação, sobreposição ou nome impedem a geração da respectiva folha.
- É possível gerar somente PDF, somente DWG ou ambos, marcando as opções da folha.
- Caso já existam arquivos com os mesmos nomes na pasta de saída, o comando solicita confirmação para sobrescrevê-los.
- O botão **Zoom** serve para conferir visualmente a janela calculada para a folha selecionada antes de gerar os arquivos.

## Checklist rápido antes de usar

- [ ] Layout correto ativo (não Model).
- [ ] Blocos `CEP-*` inseridos no Paper Space.
- [ ] Blocos em 1:1, rotação de 90° e tamanho correspondente ao formato.
- [ ] Contorno de impressão na layer `502-CEP-FOR-06`.
- [ ] Folhas sem sobreposição.
- [ ] Nomes de PDF únicos para as folhas selecionadas.
- [ ] Plotter PDF, CTB e pasta de saída definidos.
