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

### Para gerar PDF

- Selecione uma impressora/plotter PDF disponível no ZWCAD.
- Se desejado, selecione um CTB disponível. Sem CTB, o comando usa a configuração aplicável do layout.
- Escolha uma pasta de saída com permissão de escrita. A pasta do DWG é sugerida automaticamente quando o desenho já foi salvo.
- Se a pasta sugerida não for alterada pelo usuário, o comando cria uma subpasta incremental `Emissão 01`, `Emissão 02`, `Emissão 03` e assim por diante. Se o usuário escolher outra pasta, ela é usada diretamente.

### Para gerar DWG por folha

- Salve o desenho atual antes de iniciar: a exportação parte do arquivo DWG salvo em disco.
- O DWG de saída recebe o mesmo nome da folha, trocando `.pdf` por `.dwg`.
- A exportação preserva o Model e mantém, no Layout, apenas o conteúdo da folha e os viewports que pertencem à área daquela folha.

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
- [ ] DWG salvo, se houver geração de DWG individual.
