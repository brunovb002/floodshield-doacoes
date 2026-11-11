# FloodShield Doações API

API em .NET para organizar doações para abrigos e pontos de coleta durante enchentes.
Faz parte do projeto **FloodShield** (Global Solution 2026/2 - CitizenTech, FIAP).

## O problema

Quando uma cidade alaga, a vontade de ajudar aparece rápido, mas as doações chegam desencontradas:
um abrigo recebe 500 pacotes de roupa e fica sem água, outro do lado não recebe nada.
Quem quer doar não sabe o que está faltando nem onde.

## A solução

Os gestores (Defesa Civil, prefeitura, responsáveis pelos abrigos) cadastram os pontos de coleta
e o que cada um está precisando, com quantidade e urgência. O cidadão vê **onde a doação dele faz
mais falta**, promete o que vai levar e recebe um código de entrega. Quando ele entrega, o gestor
confirma com o código e o saldo da necessidade é atualizado na hora.

ODS relacionados: **9** (indústria, inovação e infraestrutura), **11** (cidades e comunidades sustentáveis)
e **13** (ação contra a mudança do clima).

## Tecnologias

- .NET 10 com ASP.NET Core Minimal API
- Entity Framework Core + SQL Server (Azure SQL na nuvem)
- Autenticação JWT com dois papéis: `Cidadao` e `Gestor`
- OpenAPI + Scalar para a documentação interativa
- Rate limiting e health checks nativos do ASP.NET Core
- ViaCEP para preencher o endereço dos pontos de coleta
- xUnit, Moq e `WebApplicationFactory` nos testes

## Funcionalidades

| Recurso | O que faz |
|---|---|
| Auth | Cadastro (sempre como cidadão), login com JWT, ver/editar/excluir a própria conta |
| Pontos de coleta | CRUD (gestor). O endereço é preenchido pelo CEP. Se o ViaCEP estiver fora, aceita o endereço digitado |
| Itens | CRUD do catálogo (gestor): água, alimento, higiene, roupa, pet... |
| Necessidades | CRUD (gestor). Cada necessidade mostra necessário, entregue, prometido e quanto falta |
| Prioridades | `GET /api/necessidades/prioritarias`: necessidades abertas ordenadas por urgência e pelo que falta |
| Doações | O cidadão promete, edita ou exclui enquanto não entregou. O gestor confirma a entrega com o código ou cancela |

### Regras principais

- Não dá para doar mais do que falta (`422`).
- Quando o entregue completa a quantidade, a necessidade vira `Atendida` sozinha.
- Duas pessoas doando ao mesmo tempo a última unidade: só uma passa (a linha da necessidade fica travada durante a gravação).
- Ponto com necessidades, item em uso, necessidade com doações e conta com doações não podem ser excluídos (`409`).
- O cidadão só enxerga as próprias doações. O gestor enxerga todas.

### Idempotência

O `POST /api/doacoes` exige o header `Idempotency-Key`. Se o app mandar a mesma requisição duas vezes
(toque duplo, internet caindo e reenviando), a segunda volta com a mesma resposta da primeira e o header
`Idempotent-Replayed: true`, sem criar outra doação. A mesma chave com um corpo diferente retorna `409`.

### Rate limiting

| Rota | Limite |
|---|---|
| `/api/auth/*` | 5 por minuto por IP |
| `POST /api/doacoes` | 10 por minuto por usuário |
| Resto da API | 100 por minuto por IP |

Acima do limite a resposta é `429` com o header `Retry-After`. Os limites ficam no `appsettings.json` (seção `RateLimit`).

### Health checks

- `GET /health/live`: a API está no ar.
- `GET /health/ready`: verifica o banco e o ViaCEP. Sem ViaCEP fica `Degraded` (a API continua funcionando); sem banco fica `Unhealthy` (`503`).

## Endpoints

A documentação completa fica em `/scalar` com a API rodando. Resumo:

| Método | Rota | Quem |
|---|---|---|
| POST | `/api/auth/registrar` | público |
| POST | `/api/auth/login` | público |
| GET / PUT / DELETE | `/api/usuarios/me` | logado (DELETE só cidadão) |
| GET | `/api/pontos-coleta`, `/api/pontos-coleta/{id}` | logado |
| POST / PUT / DELETE | `/api/pontos-coleta` | gestor |
| GET | `/api/itens`, `/api/itens/{id}` | logado |
| POST / PUT / DELETE | `/api/itens` | gestor |
| GET | `/api/necessidades`, `/api/necessidades/{id}`, `/api/necessidades/prioritarias` | logado |
| POST / PUT / DELETE | `/api/necessidades` | gestor |
| PATCH | `/api/necessidades/{id}/cancelar` | gestor |
| POST | `/api/doacoes` | cidadão |
| GET | `/api/doacoes`, `/api/doacoes/{id}` | logado |
| PUT / DELETE | `/api/doacoes/{id}` | cidadão (dono) |
| PATCH | `/api/doacoes/{id}/confirmar-entrega`, `/api/doacoes/{id}/cancelar` | gestor |

As listagens aceitam filtros, `ordenarPor`, `direcao=desc`, `pagina` e `tamanhoPagina` (máximo 50).

## Como rodar

Pré-requisitos: .NET 10 SDK e Docker.

1. Suba um SQL Server:

```
docker run -d --name floodshield-sql -e "ACCEPT_EULA=Y" -e "MSSQL_SA_PASSWORD=<sua-senha-forte>" -p 1433:1433 mcr.microsoft.com/mssql/server:2022-latest
```

2. Configure os segredos (ficam fora do repositório):

```
cd src/FloodShield.Doacoes.Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=FloodShieldDoacoes;User Id=sa;Password=<sua-senha-forte>;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "<chave-com-pelo-menos-32-caracteres>"
dotnet user-secrets set "Gestor:Email" "gestor@floodshield.com"
dotnet user-secrets set "Gestor:Senha" "<senha-do-gestor>"
```

3. Crie o banco e rode a API:

```
dotnet ef database update --project src/FloodShield.Doacoes.Api
dotnet run --project src/FloodShield.Doacoes.Api
```

A documentação abre em `http://localhost:5125/scalar`.

### Usuários de teste

- **Gestor:** criado automaticamente na primeira execução com o e-mail e a senha de `Gestor:Email` e `Gestor:Senha`.
- **Cidadão:** é só se cadastrar em `POST /api/auth/registrar`.

Na nuvem os mesmos valores vêm de variáveis de ambiente (`ConnectionStrings__DefaultConnection`, `Jwt__Key`, `Gestor__Email`, `Gestor__Senha`).

## Testes

```
dotnet test
```

- `FloodShield.Doacoes.UnitTests`: regras de saldo, prioridade, status, quantidade e código de entrega, e o health check do ViaCEP com Moq.
- `FloodShield.Doacoes.IntegrationTests`: sobe a API com `WebApplicationFactory`, SQLite em memória e um ViaCEP falso, e testa login, permissões, CRUD, doações, idempotência, health check e rate limiting.

## Integrantes

- Bruno Vinicius Barbosa - RM 566366
- Guilherme de Andrade Martini - RM 566087
- Nathan Gonçalves Pereira Mendes - RM 564666
- Raphael Gomes Mancera - RM 562279
