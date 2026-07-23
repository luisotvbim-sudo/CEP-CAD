# Contexto de compatibilidade e versionamento do ZWCAD

## Decisão

O ZWCAD 2024 é a versão-base obrigatória do CEP-CAD.

Todo código novo deve ser:

1. projetado com a API disponível no ZWCAD 2024;
2. compilado primeiro com `Debug2024 | x64`;
3. validado funcionalmente no ZWCAD 2024;
4. publicado para o ZWCAD 2024 por meio de `Release2024 | x64`.

Suportar o ZWCAD 2025 ou uma versão posterior não altera essa ordem. Uma funcionalidade que funciona somente em uma versão mais nova não está concluída enquanto o ZWCAD 2024 fizer parte do suporte oficial.

## Motivo

As dependências gerenciadas do ZWCAD não mantêm a mesma versão de assembly entre os hosts instalados:

| DLL | ZWCAD 2024 | ZWCAD 2025 |
|---|---:|---:|
| `ZwDatabaseMgd.dll` | `24.0.24.0` | `25.0.25.0` |
| `ZwManaged.dll` | `24.0.24.0` | `25.0.25.0` |
| `ZdWindows.dll` | `1.0.0.0` | `4.0.0.6` |

Por isso, não se deve assumir que um único binário compilado para o 2024 ou para o 2025 será carregado com segurança nos dois hosts.

## Estratégia de código

- Existe um único código-fonte compartilhado.
- Existe um único branch principal, `main`.
- O código comum usa apenas APIs disponíveis no ZWCAD 2024.
- Integrações específicas de versões mais novas ficam em adaptadores pequenos e isolados.
- Diretivas como `ZWCAD2024` e `ZWCAD2025` são permitidas apenas quando houver diferença real de API e uma abstração simples não resolver o problema.
- Não duplicar módulos, regras de negócio ou interfaces de usuário por versão.
- Não criar branches permanentes `2024` e `2025`.

O objetivo é manter a maior parte do código idêntica entre os hosts e restringir as diferenças às bordas que acessam a API do ZWCAD.

## Estratégia de build

Cada versão do ZWCAD deve possuir perfis e diretórios de saída separados:

| Host | Desenvolvimento | Distribuição |
|---|---|---|
| ZWCAD 2024 | `Debug2024` | `Release2024` |
| ZWCAD 2025 | `Debug2025` | `Release2025` |

Atualmente, `Debug2024` e `Release2024` são os perfis explícitos implementados e constituem o fluxo oficial. Quando o suporte ao ZWCAD 2025 for formalizado, seus builds devem receber os nomes explícitos `Debug2025` e `Release2025`; não devem reutilizar nem tornar ambíguos os perfis da versão-base.

Nunca misturar DLLs de referência ou artefatos de saída de versões diferentes no mesmo diretório.

## Estratégia de versão e publicação

O plugin possui uma única versão funcional, preferencialmente seguindo SemVer:

```text
MAJOR.MINOR.PATCH
```

Exemplo para a versão `1.3.0`:

```text
PluginConceito-1.3.0-ZWCAD2024.zip
PluginConceito-1.3.0-ZWCAD2025.zip
```

Os dois pacotes representam o mesmo código e a mesma funcionalidade, mas cada DLL é compilada contra as referências do host indicado no nome.

Use uma única tag Git, como `v1.3.0`, para os pacotes da mesma versão funcional.

## Ordem obrigatória de validação

1. Compilar `Debug2024 | x64`.
2. Testar carregamento, comandos e Ribbon no ZWCAD 2024.
3. Compilar `Release2024 | x64`.
4. Se houver suporte a outra versão, compilar o perfil específico dela.
5. Repetir os testes relevantes no host adicional.
6. Publicar um pacote separado para cada host validado.

Falhar no ZWCAD 2024 bloqueia a entrega. Falhar somente em uma versão adicional bloqueia apenas o pacote daquela versão, desde que a compatibilidade prometida seja ajustada de forma explícita.

## Regra para agentes e contribuidores

Antes de usar uma classe, método, enum ou propriedade da API do ZWCAD, considere o ZWCAD 2024 como fonte de verdade. Não programe primeiro contra o 2025 para depois tentar adaptar retroativamente.

Em caso de dúvida:

1. confirme a existência da API nas DLLs do ZWCAD 2024;
2. implemente a solução mais simples compatível com 2024;
3. isole qualquer diferença necessária para hosts posteriores;
4. mantenha a regra de negócio independente da versão do CAD.
