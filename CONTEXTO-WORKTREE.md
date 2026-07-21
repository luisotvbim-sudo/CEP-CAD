# Guia para agentes — git worktree

Use `git worktree` para ter pastas isoladas por branch, sem brigar pelo mesmo disco com outros agentes.

## Inicial (faca uma vez)

No diretorio principal do repositorio:

```powershell
git worktree add ../CTNCad-<seu-branch> -b <nome-do-branch>
```

Exemplo:
```powershell
git worktree add ../CTNCad-minha-feature -b feature/minha-feature
```

Isso cria `C:\Users\LuizOtavio\source\repos\CTNCad-minha-feature` com o novo branch, isolado.

## Usar um branch que ja existe

```powershell
git worktree add ../CTNCad-<nome> <branch-existente>
```

## Dia a dia

Trabalhe dentro da **sua** pasta (`CTNCad-<seu-branch>`). La voce faz `git add`, `git commit`, `git push` normalmente.

Antes de comecar o dia, atualize sua base:

```powershell
git fetch origin
git merge origin/main
```

## Commits que o agente A fez no branch dele

Aparecem no branch `main` quando o PR for mergeado. Para ver os commits de outro agente antes do merge:

```powershell
git fetch origin
git log origin/<branch-do-outro-agente>
```

## Ao terminar o branch (mergeado/deletado)

```powershell
# Volte para a pasta principal primeiro
cd C:\Users\LuizOtavio\source\repos\CTNCad

# Remova o worktree
git worktree remove ../CTNCad-<seu-branch>

# Delete a pasta manualmente se necessario
Remove-Item -Recurse -Force ../CTNCad-<seu-branch>
```

## Listar worktrees ativos

```powershell
git worktree list
```

## Regras

- Nunca de `git checkout` ou `git switch` dentro de um worktree. A pasta ja pertence ao branch.
- Nunca edite arquivos de outro worktree.
- Nao faca `git worktree add` com uma subpasta que ja existe dentro do repositorio principal.
