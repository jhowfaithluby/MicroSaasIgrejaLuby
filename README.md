# Guia de Desenvolvimento MicroSaaS Igreja

Este documento reúne as instruções oficiais para criar e evoluir o projeto **MicroSaaS Igreja** utilizando uma API em **.NET 8** integrada a um front-end em **React 18 + TypeScript**. As orientações contemplam práticas modernas de engenharia de software, arquitetura limpa, segurança avançada, testes automatizados e experiência do usuário.

## 1. Visão Geral da Solução
- **Back-end:** ASP.NET Core 8 minimal APIs ou controllers tradicionais, seguindo os princípios de Clean Architecture (Camadas Domain → Application → Infrastructure → Presentation).
- **Front-end:** React 18 com TypeScript, componentização reutilizável, design system compartilhado e atenção a acessibilidade (WCAG 2.1) e UX.
- **Banco de Dados:** SQL Server LocalDB (ou Docker local) via Entity Framework Core 8 com Migrations versionadas. O banco permanecerá local até nova instrução.
- **Comunicação:** RESTful APIs com versionamento (ex.: `/api/v1/...`). Documentação automática com Swagger/Swashbuckle.

## 2. Estrutura de Pastas Recomendada
```
/infra/                # IaC, Docker, scripts de provisionamento
/src
  /Api                 # Projeto ASP.NET Core (Presentation)
  /Application         # Casos de uso, DTOs, validações, mediators
  /Domain              # Entidades, agregados, Value Objects, enums
  /Infrastructure      # Persistência, repositórios, integrações externas
  /WebApp              # Front-end React + TypeScript
/tests
  /Api.Tests           # Testes de unidade e integração do back-end
  /WebApp.Tests        # Testes do front-end (unitários, integração e e2e)
```

## 3. Criação do Back-end
1. `dotnet new webapi -n MicroSaas.Api`
2. `dotnet new classlib -n MicroSaas.Domain`
3. `dotnet new classlib -n MicroSaas.Application`
4. `dotnet new classlib -n MicroSaas.Infrastructure`
5. Referenciar projetos conforme dependências (Api → Application → Domain, Application ↔ Domain, Infrastructure → Domain).
6. Configurar **Entity Framework Core 8** com **DbContext** no projeto Infrastructure.
7. Criar migrations e aplicar no banco local:
   ```bash
   dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/Api
   dotnet ef database update -p src/Infrastructure -s src/Api
   ```

### 3.1. Boas Práticas de Código Limpo
- Seguir padrões SOLID, DDD e Clean Architecture.
- Utilizar `record` para DTOs/Value Objects imutáveis.
- Injetar dependências via DI nativa (`Microsoft.Extensions.DependencyInjection`).
- Validar entradas com **FluentValidation**.
- Mapear objetos com **AutoMapper**.
- Utilizar **MediatR** para orquestração de casos de uso (CQRS quando aplicável).
- Centralizar exceções via middleware de tratamento e retorno padronizado (RFC 7807 - ProblemDetails).

### 3.2. Segurança e Autenticação
- Implementar **JWT Bearer** com refresh tokens e políticas de autorização.
- Integrar **ASP.NET Core Identity** com hashing seguro (PBKDF2/Argon2) e MFA opcional.
- Aplicar políticas CORS restritivas, rate limiting e `Content-Security-Policy`.
- Validar dados sensíveis, usar Secrets Manager para credenciais e proteger connection strings.
- Realizar varreduras OWASP (ex.: ZAP) e configurar headers de segurança (HSTS, X-Content-Type-Options, X-Frame-Options).

### 3.3. Observabilidade e Logs
- Utilizar **Serilog** com sinks para arquivo local (`logs/`) e console.
- Padronizar correlação de requisições com `Activity`/`TraceId`.
- Integrar **OpenTelemetry** para métricas e tracing (exporter OTLP pronto para upgrade futuro).
- Monitorar saúde com `/health` utilizando **AspNetCore.HealthChecks** (checagens de banco, storage, fila).

## 4. Criação do Front-end React
1. `npx create-react-app webapp --template typescript` ou usar Vite (`npm create vite@latest webapp -- --template react-ts`).
2. Estruturar pastas em **Atomic Design** ou Feature Slices.
3. Adotar **ESLint + Prettier** com regras estritas e **Husky** para hooks de `pre-commit`.
4. Utilizar **React Router v6**, **Redux Toolkit** ou **Zustand** para estado global e **React Query** para dados remotos.
5. Criar design system com **Chakra UI** ou **Material UI**, garantindo responsividade (Mobile First) e componentes acessíveis.
6. Configurar **Storybook** para documentar componentes.

### 4.1. Boas Práticas de UX/UI
- Aplicar heurísticas de Nielsen, feedback visual, loading states e mensagens de erro claras.
- Garantir contraste adequado, navegação por teclado e suporte a leitores de tela.
- Criar fluxo de autenticação consistente com o back-end (login, registro, recuperação de senha, MFA).

## 5. Testes Automatizados
- **Back-end:** `xUnit` + `FluentAssertions` para testes unitários; `WebApplicationFactory` para testes de integração; usar banco em memória (`Respawn`) e `Testcontainers` para testes mais realistas.
- **Front-end:** `Vitest`/`Jest` + `React Testing Library`; Cypress Playwright para E2E.
- Cobertura mínima de 80%. Cada funcionalidade nova deve incluir testes unitários e de integração correspondentes.
- Configurar pipelines (GitHub Actions/Azure DevOps) para executar linters, testes e análise estática (SonarQube) a cada PR.

## 6. Fluxo de Desenvolvimento
1. Criar uma branch feature a partir de `main`.
2. Implementar a funcionalidade seguindo TDD quando possível.
3. Executar testes unitários, integração e E2E relevantes.
4. Rodar o projeto (API + WebApp) após cada tarefa para garantir estabilidade.
5. Registrar logs gerados em `logs/` e revisar alertas.
6. Submeter PR com descrição detalhada, checklist de testes e anexar evidências (prints/logs). Solicitar code review cruzado.
7. Após merge, atualizar `CHANGELOG.md` e documentações pertinentes.

## 7. Segurança Avançada e Compliance
- Revisar periodicamente dependências com `dotnet list package --vulnerable` e `npm audit`.
- Habilitar `DataProtection` com persistência em arquivo seguro.
- Configurar `Security.txt`, políticas de senha forte e bloqueio após tentativas inválidas.
- Aplicar LGPD: consentimento, política de privacidade, mascaramento/anônimização de dados sensíveis.

## 8. Escalabilidade e Modernização
- Preparar o projeto para contêineres (Dockerfile multi-stage para API e front).
- Adotar arquitetura orientada a eventos quando necessário (ex.: `MassTransit` com RabbitMQ).
- Planejar feature toggles (ex.: `Microsoft.FeatureManagement`).
- Implementar caching distribuído (Redis) e estratégias de paged queries.

## 9. Documentação e Colaboração
- Manter documentação atualizada no repositório (`/docs`).
- Criar diagramas C4 (Contexto, Container, Componentes) e atualizar quando houver mudanças estruturais.
- Registrar decisões arquiteturais em ADRs.
- Realizar retrospectivas técnicas e reavaliar periodicamente estas instruções para incorporar melhorias contínuas.

## 10. Checklist Inicial
- [ ] Instalar SDK .NET 8 e Node.js LTS.
- [ ] Configurar ferramentas de qualidade (ESLint, Stylelint, Analyzer Roslyn, SonarQube).
- [ ] Inicializar repositório git com `.editorconfig`, `.gitignore` e templates de PR/Issues.
- [ ] Configurar Secrets (User Secrets no back-end e `.env.local` no front).
- [ ] Definir estratégia de versionamento semântico e branch naming convention.

> **Revisão Contínua:** Reavalie este guia periodicamente para incluir novas práticas, tecnologias e requisitos de negócio. Atualize instruções sempre que surgirem mudanças significativas (ex.: alteração do banco para cloud, novos mecanismos de segurança, padrões de logging ou ferramentas de monitoramento).
