# MicroSaaS Igreja - Visão Arquitetural

## Camadas

- **Domain**: contém as entidades e regras de negócio centrais (`Member`).
- **Application**: orquestra casos de uso via MediatR (commands/queries) e aplica validações com FluentValidation.
- **Infrastructure**: implementação de persistência (EF Core + SQLite por padrão ou SQL Server sob configuração), identidade (ASP.NET Core Identity) e repositórios concretos.
- **Api (Presentation)**: expõe Minimal APIs com documentação Swagger, tratamento padronizado de erros e health checks.
- **WebApp**: front-end React 18 + TypeScript consumindo a API via React Query, com fluxo de autenticação (login/registro) e gerenciamento de tokens JWT.

## Fluxo de Cadastro de Membros

1. O usuário preenche o formulário no front-end (`MemberForm`).
2. O `useCreateMember` chama `POST /api/v1/members`.
3. A API valida o comando (`CreateMemberCommandValidator`) via pipeline e salva no banco usando o `MemberRepository`.
4. O front invalida o cache e atualiza a listagem (`MemberList`).

## Banco de Dados & Segurança

- EF Core configura a tabela `Members` com índice único em e-mail, além das tabelas padrão do Identity e `RefreshTokens`.
- A autenticação utiliza **ASP.NET Core Identity** com tokens JWT (`/api/v1/auth/login`, `/api/v1/auth/refresh`, `/api/v1/auth/logout`).
- Tokens de refresh são persistidos com hash e vinculados ao usuário, permitindo revogação.
- As migrations podem ser geradas via `dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api`.
- Há _seeding_ opcional de usuário administrador em `Seed:Admin` no `appsettings.json`.

## Observabilidade e Segurança

- Serilog registra requisições (`UseSerilogRequestLogging`).
- OpenTelemetry exporta métricas e traces para o console.
- CORS restrito para as origens cadastradas em `appsettings.json`.
- Rate limiting global aplicado às rotas (`fixed` window 50 req/10s).
- Health check disponível em `/health`.
- Cabeçalhos de segurança (HSTS, CSP, X-Frame-Options, etc.) aplicados a todas as respostas.

## Testes

- `tests/Api.Tests` executa testes de integração com banco SQLite in-memory.
- `src/WebApp/src/__tests__` cobre a experiência de cadastro no front com React Testing Library + Vitest.
