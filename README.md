# Developer Evaluation Project — Sales Management Portal

Este repositório contém a implementação da **Sales API** e do **Sales Portal (Frontend)** do DeveloperStore. A solução estende o template padrão utilizando práticas de **Clean Architecture**, **Domain-Driven Design (DDD)**, **CQRS** e **Auditoria baseada em Eventos**.

---

## 1. Arquitetura da Solução

A estrutura segue o padrão de isolamento de responsabilidades do template original:

```
 ┌────────────────────────────────────────────────────────┐
 │            Navegador Web (Interface)                   │
 └──────────────────────────┬─────────────────────────────┘
                            │ (Chamadas HTTP)
                            ▼
 ┌────────────────────────────────────────────────────────┐
 │       Container Frontend Angular (Porta 80 / 4200)      │
 │  ┌──────────────────────────────────────────────────┐  │
 │  │        SPA Angular (Aplicação Client)            │  │
 │  └───────────────────────┬──────────────────────────┘  │
 │                          │ (Chamadas relativas /api/*)
 │                          ▼
 │  ┌──────────────────────────────────────────────────┐  │
 │  │      Reverse Proxy Nginx (Roteamento Interno)    │  │
 │  └───────────────────────┬──────────────────────────┘  │
 └──────────────────────────┼─────────────────────────────┘
                            │ (Rede interna Docker)
                            ▼
 ┌────────────────────────────────────────────────────────┐
 │       Container Backend ASP.NET Core (Porta 8080)       │
 │  ┌──────────────────────────────────────────────────┐  │
 │  │            Controllers REST (Web API)            │  │
 │  └───────────────────────┬──────────────────────────┘  │
 │                          │ (Commands / Queries)
 │                          ▼
 │  ┌──────────────────────────────────────────────────┐  │
 │  │      MediatR Pipeline (Validation Behavior)      │  │
 │  └───────────────────────┬──────────────────────────┘  │
 │                          │ (Execução do Handler)
 │                          ▼
 │  ┌──────────────────────────────────────────────────┐  │
 │  │             Camada de Aplicação (CQRS)           │  │
 │  └───────────────┬───────────────────┬──────────────┘  │
 │                  │                   │                 │
 │                  │ (Persistência)    │ (Eventos)       │
 │                  ▼                   ▼                 │
 │            ┌───────────┐       ┌───────────┐           │
 │            │  Domain   │       │  Domain   │           │
 │            │  Aggregate│       │  Events   │           │
 │            └─────┬─────┘       └─────┬─────┘           │
 └──────────────────┼───────────────────┼─────────────────┘
                    │                   │
                    ▼                   ▼
 ┌─────────────────────┐     ┌─────────────────────┐
 │    PostgreSQL 13    │     │     MongoDB 8.0     │
 │ (Transações ACID)   │     │ (Trilha de Audit)   │
 │    Porta 5432       │     │    Porta 27017      │
 └─────────────────────┘     └─────────────────────┘
```

---

## 2. Justificativas das Decisões Técnicas

Para atender aos critérios do desafio de forma robusta e profissional, tomamos as seguintes decisões de design:

### A. Reconciliação no Domínio (`Sale.ReconcileItems`)
* **Problema:** A atualização de uma venda (`UpdateSaleCommand`) envolve tratar itens que foram adicionados, atualizados em quantidade/preço, removidos ou cancelados. Deixar essa lógica no Handler tornaria a aplicação imperativa e anêmica.
* **Solução:** Movemos a lógica de reconciliação diretamente para a entidade `Sale` (Aggregate Root). Ela compara o estado atual com a lista desejada, adiciona novos itens, altera quantidades executando as regras de limite (máximo de 20 unidades) e recalcula os descontos de forma centralizada. Isso garante que a integridade física e as regras de negócio de venda sejam validadas em uma única transação no domínio.

### B. Estratégia de Eventos de Domínio Imutáveis
* **Problema:** Notificar outros componentes do sistema sobre alterações de estado (`SaleCreatedEvent`, `SaleModifiedEvent`, `SaleCancelledEvent`) sem acoplar os consumidores às entidades de banco de dados (que são mutáveis).
* **Solução:** Modelamos os eventos de domínio como classes imutáveis (com `get-only properties`). Eles não referenciam entidades do Entity Framework Core. Em vez disso, copiam os dados primitivos necessários no momento em que o evento ocorre. Isso previne efeitos colaterais e garante a segurança dos dados trafegados.

### C. Trilha de Auditoria Isolada com MongoDB NoSQL
* **Problema:** Registrar o histórico de todas as modificações de vendas sem inflar as tabelas relacionais do PostgreSQL e sem concorrer por locks de banco de dados durante consultas de auditoria.
* **Solução:** Adotamos o **MongoDB** exclusivamente como repositório de logs de auditoria de eventos de domínio. A gravação do log é feita de forma assíncrona por meio do `LoggingEventPublisher`. Para garantir que uma falha na escrita do log NoSQL não interrompa a venda do cliente no PostgreSQL (consistência eventual), o publicador trata exceções de forma isolada, registrando avisos para tratamento posterior.

### D. Reverse Proxy com Nginx no Container Frontend
* **Problema:** Evitar problemas de CORS (Cross-Origin Resource Sharing) no navegador e a necessidade de configurar IPs/portas absolutas do backend no código Angular em diferentes ambientes.
* **Solução:** O container do frontend roda um servidor **Nginx** configurado como proxy reverso. As requisições direcionadas para `/api/*` são interceptadas e encaminhadas na rede interna do Docker para o container do backend. Para desenvolvimento local fora do container, o Angular CLI utiliza a diretiva `proxy.conf.json`, fornecendo o mesmo comportamento de URLs relativas.

### E. Validação Centralizada no MediatR (`ValidationBehavior`)
* **Problema:** Evitar validações manuais redundantes dentro de cada Command Handler, poluindo a regra de negócio da aplicação com tratamento de erros de input.
* **Solução:** Registramos um pipeline behavior genérico (`ValidationBehavior<,>`). Ele intercepta todos os comandos antes da execução de seus Handlers, dispara os respectivos validadores do `FluentValidation` e lança uma `ValidationException` se houver falhas. As exceções são capturadas globalmente pelo `ValidationExceptionMiddleware` da API para retornar respostas limpas em formato JSON.

---

## 3. Tecnologias Utilizadas

* **Backend:** .NET 8.0, EF Core 8, MediatR, FluentValidation, AutoMapper, PostgreSQL, MongoDB, Redis, xUnit.
* **Frontend:** Angular 21, Angular Material.
* **DevOps:** Docker, Docker Compose, Nginx.

---

## 4. Regras de Negócio de Vendas

* **Descontos por Item:**
  * Menos de 4 itens: **Sem desconto**.
  * De 4 a 9 itens: **10% de desconto**.
  * De 10 a 20 itens: **20% de desconto**.
  * Acima de 20 itens: **Rejeitado pelo domínio** (limite máximo permitido).
* **Cancelamento:**
  * O cancelamento de uma venda realiza o cancelamento lógico em cascata de todos os seus itens associados.
  * Itens cancelados têm seus valores zerados e não influenciam no valor consolidado da venda.

---

## 5. Como Executar a Solução (Docker Compose)

### Execução Completa
Na raiz do repositório, inicie todos os containers (bancos de dados e aplicações):
```bash
docker compose up --build
```
Este comando realiza o build das imagens, cria as tabelas do banco relacional, aplica as migrações automaticamente no startup do backend e sobe os seguintes endereços:
* **Portal Frontend (Angular):** [http://localhost](http://localhost) (ou port 4200)
* **Swagger API (Backend):** [http://localhost:5119/swagger](http://localhost:5119/swagger)

### Fluxo de Teste da API (Login)
O banco de dados PostgreSQL inicia sem usuários cadastrados. Para testar o portal administrativo:
1. Cadastre um usuário fazendo `POST` para `http://localhost:5119/api/users`:
   ```json
   {
     "username": "avaliador",
     "password": "Password123!",
     "phone": "+5511999999999",
     "email": "avaliador@example.com",
     "status": "Active",
     "role": "Admin"
   }
   ```
2. Acesse [http://localhost](http://localhost), insira o email `avaliador@example.com` e a senha `Password123!` para realizar o login e navegar nas telas de vendas.

---

## 6. Como Executar os Testes Automatizados

Na pasta `template/backend/`, execute:
```bash
dotnet test Ambev.DeveloperEvaluation.sln
```
* Os testes de integração de API utilizam `WebApplicationFactory` com banco de dados em memória (`InMemoryDatabase`) e publicação de auditoria mockada, permitindo a execução rápida e isolada de todos os cenários sem dependências de containers externos.
