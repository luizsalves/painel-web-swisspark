# Contexto do Projeto — Painel Web SwissPark

> Documento gerado a partir de uma conversa de planejamento de arquitetura, para dar contexto completo ao trabalho que será feito na aba Code do Claude Desktop.

## 1. O que é o projeto

**Painel Web SwissPark** é um sistema de conciliação contábil. A tela principal é o **"Fechamento Mensal"**, onde o usuário:

- Seleciona **Empresa**, **Conta Contábil** e **Período (Mês/Ano)**
- Vê indicadores: Saldo Razão, Composição do Saldo, Diferença, Status Atual, Responsável
- Navega por abas: **Lançamentos**, **Pendências**, **Observações**, **Histórico**
- Executa ações: Preparar, Pré-conferir, Conferir, Realizar Fechamento, Gerar Excel

Outras áreas do menu lateral: Dashboard, Empresas, Contas Contábeis, Usuários, Relatórios, Pendências, Configurações, Auditoria.

**Stack definida:**
- Frontend: React + TypeScript
- Backend: C# (.NET)
- Banco de dados: **SQL Server** (fonte real dos dados)

**Caminho do projeto na máquina:**
```
C:\Projetos\SwissPark\painel-web-swisspark
```
(contém as pastas de backend e frontend)

Há também um projeto de referência/aprendizado anterior em:
```
C:\Projetos\react-integrando-api-restaurante-main\alfood-main
```
usado como exemplo de separação de `pages`, `components` e `interfaces` no React.

E um projeto modelo usado por um dev sênior da equipe (ainda não analisado em detalhe):
```
C:\Projetos\modelo-projeto\portal-powerbi-embedded
```

---

## 2. Histórico das decisões de arquitetura

### 2.1 Ideia inicial
Expor uma API própria em cima de um banco Postgres, usando C# como backend.

### 2.2 Segunda ideia — BFF consumindo múltiplas APIs de ERP
A ideia evoluiu para: o C# funcionaria como um **BFF (Backend for Frontend)**, consumindo **várias APIs de um ERP**, cruzando (fazendo *joins*) os dados em memória via LINQ, aplicando regras de negócio, e expondo **endpoints únicos e já tratados** para o React consumir — em vez do frontend ter que chamar várias APIs e cruzar dados sozinho.

Estrutura de pastas pensada para esse cenário (backend):
```
MeuProjeto.Api/
├── Controllers/
├── Services/
│   ├── Erp/              ← HttpClients para cada API do ERP
│   └── BusinessLogic/    ← joins e regras de negócio
├── DTOs/
│   ├── Erp/              ← DTOs "crus" espelhando a resposta do ERP
│   └── ViewModels/       ← DTOs tratados, prontos pro front
├── Http/                 ← config de HttpClient (base URL, timeout, retry)
└── Program.cs
```

### 2.3 MUDANÇA DE ESCOPO (decisão final, atual)
**O projeto não vai mais consumir APIs do ERP.** Em vez disso, vai **acessar diretamente um banco de dados SQL Server** (usuário e senha), que é a fonte real dos dados.

**O que isso elimina:**
- ❌ `Services/Erp/` (HttpClients para chamadas ao ERP)
- ❌ `DTOs/Erp/` (DTOs que espelhavam resposta JSON de API externa)
- ❌ `Http/ErpHttpClientOptions.cs`
- ❌ Orquestração de múltiplas chamadas HTTP em paralelo (`Task.WhenAll`)

**O que entra no lugar:**
- ✅ Conexão direta ao SQL Server via **Entity Framework Core** (pacote `Microsoft.EntityFrameworkCore.SqlServer`)
- ✅ `AppDbContext` configurado com a connection string do SQL Server
- ✅ `Entities` que espelham as tabelas reais do banco
- ✅ Os "joins" que antes eram feitos em memória (depois de buscar de várias APIs) agora podem ser **queries LINQ traduzidas para SQL de verdade**, que o próprio banco resolve

**O que NÃO muda:**
- O padrão de camadas Controller → Service (com Interface) → DTO/ViewModel continua o mesmo
- Injeção de Dependência continua sendo usada do mesmo jeito
- O React não muda nada — ele só recebe o JSON pronto do endpoint, sem saber de onde os dados vieram

---

## 3. Arquitetura final do Backend (C#)

```
PainelWebSwissPark.Api/
├── Controllers/
│   ├── FechamentosController.cs
│   ├── LancamentosController.cs
│   ├── PendenciasController.cs
│   ├── EmpresasController.cs
│   ├── ContasContabeisController.cs
│   └── UsuariosController.cs
├── Services/
│   └── BusinessLogic/
│       ├── IFechamentosService.cs
│       ├── FechamentosService.cs
│       ├── IEmpresasService.cs
│       └── EmpresasService.cs
├── Data/
│   ├── AppDbContext.cs
│   └── Entities/
│       ├── Empresa.cs
│       ├── ContaContabil.cs
│       ├── Lancamento.cs
│       ├── Pendencia.cs
│       ├── Fechamento.cs
│       └── Usuario.cs
├── DTOs/
│   └── ViewModels/
│       ├── FechamentoViewModel.cs
│       ├── LancamentoViewModel.cs
│       └── PendenciaViewModel.cs
├── Program.cs
└── appsettings.json
```

### 3.1 Padrão obrigatório: Interface + Service + Controller + DI

Existe um `HealthController` no projeto que **não segue** esse padrão completo (porque só devolve um status fixo, sem lógica real nem acesso a dados). Ele **não deve ser usado como modelo** para os demais controllers.

O padrão correto, para qualquer endpoint que busque/processe dados de verdade, é baseado no `FechamentosController` (exemplo já existente no projeto):

```csharp
// 1. Interface — o "contrato"
public interface IFechamentosService
{
    Task<FechamentoViewModel> ObterFechamentoAsync(
        int empresaId, int contaId, int mes, int ano, CancellationToken cancellationToken);
}

// 2. Implementação — a lógica de negócio de verdade
public class FechamentosService : IFechamentosService
{
    private readonly AppDbContext _context;

    public FechamentosService(AppDbContext context) => _context = context;

    public async Task<FechamentoViewModel> ObterFechamentoAsync(
        int empresaId, int contaId, int mes, int ano, CancellationToken cancellationToken)
    {
        // busca no SQL Server, aplica joins/regras de negócio, retorna o ViewModel
    }
}

// 3. Controller — só recebe a requisição HTTP e devolve a resposta
public class FechamentosController(IFechamentosService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        int empresaId, int contaId, int mes, int ano, CancellationToken cancellationToken)
    {
        var fechamento = await service.ObterFechamentoAsync(empresaId, contaId, mes, ano, cancellationToken);
        return Ok(fechamento);
    }
}

// 4. Registro da dependência no Program.cs
builder.Services.AddScoped<IFechamentosService, FechamentosService>();
```

**Papel de cada peça, resumido:**

| Peça | Responsabilidade |
|---|---|
| **Controller** | Só a "porta HTTP": define a rota, recebe parâmetros da requisição, chama o Service, traduz o resultado em resposta HTTP (`Ok`, `NotFound`, `BadRequest`) |
| **Service** | Contém a lógica de negócio de verdade — busca dados, faz joins, aplica cálculos e regras. Não sabe nada sobre HTTP |
| **Interface** | O "contrato" que o Service implementa, permitindo que o Controller dependa de uma abstração (facilita testes e troca de implementação) |
| **Program.cs (DI)** | Registra qual classe concreta (`FechamentosService`) deve ser entregue sempre que alguém pedir a interface (`IFechamentosService`) |

Importante: o Controller **não decide em tempo real** qual método/serviço chamar — isso já foi definido no `Program.cs`, na inicialização da aplicação. Em tempo de execução, o Controller apenas executa código sequencial normal, chamando por nome o método do Service que ele já conhece via injeção.

### 3.2 Conexão com SQL Server

**Pacote necessário:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
```

**`appsettings.json`:**
```json
{
  "ConnectionStrings": {
    "SqlServer": "Server=SEU_SERVIDOR;Database=SwissPark;User Id=SEU_USUARIO;Password=SUA_SENHA;TrustServerCertificate=True"
  }
}
```

**`Program.cs`:**
```csharp
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("SqlServer")));
```

### 3.3 Swagger

O Swagger **documenta automaticamente** os endpoints já criados — ele não cria endpoint nenhum sozinho. A ordem certa é: criar o Controller primeiro, o Swagger detecta e exibe depois.

```csharp
// Program.cs
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// ...
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
```
Acesso: `https://localhost:XXXX/swagger`

### 3.4 Comunicação Frontend ↔ Backend

A comunicação é **sempre via API REST (HTTP/JSON)**. O React nunca acessa o banco de dados diretamente — por segurança (credenciais do banco ficariam expostas no navegador) e para manter a lógica de negócio centralizada no backend.

```
[React + TypeScript]
        ↕ HTTP/JSON (API REST)
[C# — Controllers → Services → DbContext]
        ↕ conexão direta (usuário/senha)
[SQL Server]
```

---

## 4. Arquitetura do Frontend (React + TypeScript)

Estrutura espelhando o menu lateral do painel:

```
src/
├── pages/
│   ├── FechamentoMensal/
│   │   └── FechamentoMensalPage.tsx
│   ├── Empresas/
│   │   └── EmpresasPage.tsx
│   ├── ContasContabeis/
│   │   └── ContasContabeisPage.tsx
│   └── Usuarios/
│       └── UsuariosPage.tsx
├── components/
│   ├── FechamentoMensal/
│   │   ├── FechamentoHeader.tsx       ← selects + cards de saldo
│   │   ├── FechamentoTabs.tsx         ← abas Lançamentos/Pendências/...
│   │   ├── LancamentosTable.tsx       ← tabela de lançamentos
│   │   ├── PendenciasList.tsx
│   │   └── AcoesPanel.tsx             ← botões Preparar/Conferir/Fechar
│   └── layout/
│       ├── Sidebar.tsx                ← menu lateral verde
│       └── Layout.tsx
├── interfaces/
│   ├── Fechamento.ts
│   ├── Lancamento.ts
│   ├── Empresa.ts
│   └── ContaContabil.ts
├── services/
│   ├── fechamentosService.ts
│   ├── empresasService.ts
│   └── api.ts                         ← configuração base do axios
├── hooks/
│   ├── useFechamentos.ts
│   └── useLancamentos.ts
└── App.tsx
```

**Regra de correspondência entre back e front:**

| C# (backend) | React (frontend) | Papel |
|---|---|---|
| `DTOs/ViewModels/*.cs` | `interfaces/*.ts` | Mesma "forma" de dado, espelhada nos dois lados |
| `Services/BusinessLogic/*.cs` | `services/*.ts` | Um faz a lógica, o outro só busca o resultado pronto |
| `Controllers/*.cs` | `hooks/use*.ts` | O hook "conversa" com o endpoint do controller |
| — | `pages/*/` | Onde a tela monta tudo, usando hooks + components |
| — | `components/*` | Peças de UI reutilizáveis, sem lógica de negócio |

**Exemplo de interface espelhando o ViewModel do C#:**
```typescript
// interfaces/Lancamento.ts
export interface Lancamento {
  data: string;
  historico: string;
  documento: string;
  numero: string;
  valor: number;
  tipo: 'Debito' | 'Credito';
}
```

**Exemplo de service consumindo a API:**
```typescript
// services/fechamentosService.ts
import { api } from './api';
import { Fechamento } from '../interfaces/Fechamento';

export async function getFechamento(
  empresaId: number, contaId: number, mes: number, ano: number
): Promise<Fechamento> {
  const { data } = await api.get<Fechamento>('/fechamentos', {
    params: { empresaId, contaId, mes, ano }
  });
  return data;
}
```

---

## 5. Fluxo completo, ponta a ponta (exemplo real)

1. **React**: usuário seleciona Empresa + Conta + Período, clica em "Carregar Dados"
2. O hook (`useFechamento`) dispara a chamada via `fechamentosService.ts`
3. **`FechamentosController`** (C#) recebe a requisição HTTP
4. **`FechamentosService`** (C#) monta a query no `AppDbContext`, busca no SQL Server já com joins/cálculos (ex: Diferença = Saldo Razão − Composição do Saldo)
5. Retorna um `FechamentoViewModel` já pronto
6. **React** recebe o JSON e distribui para os componentes (`LancamentosTable`, `PendenciasList`, cards de saldo)

---

## 6. Próximos passos em aberto

- [ ] Obter a connection string real do SQL Server (servidor, usuário, senha, nome do banco)
- [ ] Criar as `Entities` correspondentes às tabelas reais do banco
- [ ] Criar os Controllers/Services para cada área (Fechamentos, Lançamentos, Pendências, Empresas, Contas Contábeis, Usuários)
- [ ] Configurar CORS no backend para permitir que o React (rodando em outra porta) chame a API
- [ ] Analisar o projeto modelo do time sênior (`portal-powerbi-embedded`) e comparar com esse padrão antes de seguir em frente
- [ ] Definir autenticação/autorização (login de usuários, perfis de acesso)
