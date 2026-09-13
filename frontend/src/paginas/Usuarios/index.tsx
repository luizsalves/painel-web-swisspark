import { useCallback, useEffect, useMemo, useState } from "react";
import { KeyRound, Pencil, Search, UserPlus } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import AppLayout from "../../componentes/layout/AppLayout";
import Button from "../../componentes/ui/Button";
import Input from "../../componentes/ui/Input";
import LoadingSpinner from "../../componentes/ui/LoadingSpinner";
import Modal from "../../componentes/ui/Modal";
import Select from "../../componentes/ui/Select";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import { useAuth } from "../../contexts/AuthContext";
import { extrairMensagemDeErro } from "../../services/api";
import {
  alterarStatusUsuario,
  atualizarUsuario,
  criarUsuario,
  listarUsuarios,
  redefinirSenhaUsuario,
} from "../../services/usuariosService";
import type { UserRole } from "../../interfaces/Auth";
import type { Usuario } from "../../interfaces/Usuario";

const OPCOES_DE_PERFIL: { value: UserRole; label: string }[] = [
  { value: "Administrador", label: "Administrador" },
  { value: "Gerente", label: "Gerente" },
  { value: "Analista", label: "Analista" },
];

const TAMANHO_MINIMO_DE_SENHA = 8;

type Formulario = { modo: "criar" } | { modo: "editar"; usuario: Usuario };

function ordenarPorNome(usuarios: Usuario[]): Usuario[] {
  return [...usuarios].sort((a, b) => a.name.localeCompare(b.name, "pt-BR"));
}

export default function Usuarios() {
  const { user } = useAuth();

  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [carregando, setCarregando] = useState(true);
  const [erroDaLista, setErroDaLista] = useState<string | null>(null);
  const [busca, setBusca] = useState("");

  const [formulario, setFormulario] = useState<Formulario | null>(null);
  const [nome, setNome] = useState("");
  const [email, setEmail] = useState("");
  const [perfil, setPerfil] = useState<UserRole>("Analista");
  const [senhaInicial, setSenhaInicial] = useState("");
  const [erroDoFormulario, setErroDoFormulario] = useState<string | null>(null);
  const [salvando, setSalvando] = useState(false);

  const [alvoDaConfirmacao, setAlvoDaConfirmacao] = useState<Usuario | null>(null);
  const [erroDaConfirmacao, setErroDaConfirmacao] = useState<string | null>(null);
  const [alterandoStatus, setAlterandoStatus] = useState(false);

  const [alvoDaSenha, setAlvoDaSenha] = useState<Usuario | null>(null);
  const [novaSenha, setNovaSenha] = useState("");
  const [erroDaSenha, setErroDaSenha] = useState<string | null>(null);
  const [redefinindoSenha, setRedefinindoSenha] = useState(false);

  const carregarLista = useCallback(async () => {
    setCarregando(true);
    setErroDaLista(null);

    try {
      setUsuarios(ordenarPorNome(await listarUsuarios()));
    } catch (erro) {
      setErroDaLista(extrairMensagemDeErro(erro, "Não foi possível carregar os usuários."));
    } finally {
      setCarregando(false);
    }
  }, []);

  useEffect(() => {
    void carregarLista();
  }, [carregarLista]);

  const listaFiltrada = useMemo(() => {
    const termo = busca.trim().toLocaleLowerCase("pt-BR");
    if (!termo) return usuarios;

    return usuarios.filter(
      (usuario) =>
        usuario.name.toLocaleLowerCase("pt-BR").includes(termo) ||
        usuario.email.toLocaleLowerCase("pt-BR").includes(termo) ||
        usuario.role.toLocaleLowerCase("pt-BR").includes(termo)
    );
  }, [usuarios, busca]);

  function abrirCriacao() {
    setFormulario({ modo: "criar" });
    setNome("");
    setEmail("");
    setPerfil("Analista");
    setSenhaInicial("");
    setErroDoFormulario(null);
  }

  function abrirEdicao(usuario: Usuario) {
    setFormulario({ modo: "editar", usuario });
    setNome(usuario.name);
    setEmail(usuario.email);
    setPerfil(usuario.role);
    setSenhaInicial("");
    setErroDoFormulario(null);
  }

  function fecharFormulario() {
    setFormulario(null);
    setSenhaInicial("");
    setErroDoFormulario(null);
  }

  function guardarNaLista(salvo: Usuario) {
    setUsuarios((atuais) => {
      const jaEstava = atuais.some((usuario) => usuario.id === salvo.id);

      return ordenarPorNome(
        jaEstava
          ? atuais.map((usuario) => (usuario.id === salvo.id ? salvo : usuario))
          : [...atuais, salvo]
      );
    });
  }

  async function salvar() {
    if (!formulario) return;

    if (formulario.modo === "criar" && senhaInicial.length < TAMANHO_MINIMO_DE_SENHA) {
      setErroDoFormulario(`A senha inicial precisa ter pelo menos ${TAMANHO_MINIMO_DE_SENHA} caracteres.`);
      return;
    }

    setSalvando(true);
    setErroDoFormulario(null);

    try {
      const salvo =
        formulario.modo === "criar"
          ? await criarUsuario({ name: nome, email, role: perfil, senhaInicial })
          : await atualizarUsuario(formulario.usuario.id, { name: nome, role: perfil });

      guardarNaLista(salvo);
      fecharFormulario();
    } catch (erro) {
      setErroDoFormulario(extrairMensagemDeErro(erro, "Não foi possível salvar o usuário."));
    } finally {
      setSalvando(false);
    }
  }

  async function confirmarAlteracaoDeStatus() {
    if (!alvoDaConfirmacao) return;

    setAlterandoStatus(true);
    setErroDaConfirmacao(null);

    try {
      const salvo = await alterarStatusUsuario(alvoDaConfirmacao.id, !alvoDaConfirmacao.isActive);
      guardarNaLista(salvo);
      setAlvoDaConfirmacao(null);
    } catch (erro) {
      setErroDaConfirmacao(extrairMensagemDeErro(erro, "Não foi possível alterar o acesso."));
    } finally {
      setAlterandoStatus(false);
    }
  }

  async function confirmarRedefinicaoDeSenha() {
    if (!alvoDaSenha) return;

    if (novaSenha.length < TAMANHO_MINIMO_DE_SENHA) {
      setErroDaSenha(`A senha precisa ter pelo menos ${TAMANHO_MINIMO_DE_SENHA} caracteres.`);
      return;
    }

    setRedefinindoSenha(true);
    setErroDaSenha(null);

    try {
      guardarNaLista(await redefinirSenhaUsuario(alvoDaSenha.id, novaSenha));
      setAlvoDaSenha(null);
      setNovaSenha("");
    } catch (erro) {
      setErroDaSenha(extrairMensagemDeErro(erro, "Não foi possível redefinir a senha."));
    } finally {
      setRedefinindoSenha(false);
    }
  }

  const colunas: TableColumn<Usuario>[] = [
    {
      key: "name",
      header: "Nome",
      render: (linha) => (
        <div className="sp-usuarios__pessoa">
          <span className="sp-usuarios__avatar">{linha.avatarInitials}</span>
          <span>{linha.name}</span>
        </div>
      ),
    },
    { key: "email", header: "E-mail", render: (linha) => linha.email },
    { key: "role", header: "Perfil", render: (linha) => linha.role },
    {
      key: "status",
      header: "Status",
      render: (linha) => (
        <div className="sp-usuarios__status">
          <StatusBadge
            label={linha.isActive ? "Ativo" : "Inativo"}
            tone={linha.isActive ? "green" : "neutral"}
          />
          {linha.isActive && linha.mustChangePassword && (
            <span className="sp-usuarios__pendente">senha provisória</span>
          )}
        </div>
      ),
    },
    {
      key: "acoes",
      header: "Ações",
      align: "right",
      render: (linha) => {
        const ehVoceMesmo = linha.id === user?.id;

        return (
          <div className="sp-usuarios__acoes">
            <Button
              type="button"
              variant="ghost"
              icon={<Pencil size={15} />}
              onClick={() => abrirEdicao(linha)}
            >
              Editar
            </Button>

            <Button
              type="button"
              variant="ghost"
              icon={<KeyRound size={15} />}
              onClick={() => {
                setErroDaSenha(null);
                setNovaSenha("");
                setAlvoDaSenha(linha);
              }}
            >
              Senha
            </Button>

            <Button
              type="button"
              variant={linha.isActive ? "danger" : "secondary"}
              disabled={ehVoceMesmo && linha.isActive}
              title={
                ehVoceMesmo && linha.isActive
                  ? "Você não pode desativar o seu próprio acesso."
                  : undefined
              }
              onClick={() => {
                setErroDaConfirmacao(null);
                setAlvoDaConfirmacao(linha);
              }}
            >
              {linha.isActive ? "Desativar" : "Ativar"}
            </Button>
          </div>
        );
      },
    },
  ];

  const ehEdicao = formulario?.modo === "editar";

  return (
    <AppLayout>
      <AppHeader title="Usuários" subtitle="Controle quem entra no painel e com qual perfil" />

      <main className="sp-management">
        <div className="sp-management__toolbar">
          <div className="sp-management__search">
            <Input
              aria-label="Buscar usuários"
              placeholder="Buscar por nome, e-mail ou perfil"
              value={busca}
              onChange={(evento) => setBusca(evento.target.value)}
              leadingIcon={<Search size={18} />}
            />
          </div>

          <div className="sp-management__acoes">
            <span className="sp-management__count">
              {listaFiltrada.length} {listaFiltrada.length === 1 ? "usuário" : "usuários"}
            </span>
            <Button type="button" icon={<UserPlus size={16} />} onClick={abrirCriacao}>
              Novo usuário
            </Button>
          </div>
        </div>

        {erroDaLista && (
          <p className="sp-usuarios__erro" role="alert">
            {erroDaLista}
          </p>
        )}

        <section className="sp-management__table" aria-label="Lista de usuários">
          {carregando ? (
            <div className="sp-usuarios__carregando">
              <LoadingSpinner size={28} />
            </div>
          ) : (
            <Table
              columns={colunas}
              data={listaFiltrada}
              getRowId={(linha) => linha.id}
              emptyMessage="Nenhum usuário encontrado."
            />
          )}
        </section>
      </main>

      <Modal
        isOpen={formulario !== null}
        onClose={fecharFormulario}
        title={ehEdicao ? "Editar usuário" : "Novo usuário"}
        description={
          ehEdicao
            ? "O e-mail não muda: é por ele que a pessoa entra."
            : "A senha que você definir é provisória — a pessoa é obrigada a trocar no primeiro acesso."
        }
      >
        <form
          className="sp-usuarios__form"
          onSubmit={(evento) => {
            evento.preventDefault();
            void salvar();
          }}
        >
          <Input
            label="Nome"
            name="nome"
            placeholder="Nome completo"
            value={nome}
            onChange={(evento) => setNome(evento.target.value)}
            autoComplete="off"
          />

          <Input
            label="E-mail corporativo"
            name="email"
            type="email"
            placeholder="nome.sobrenome@swisspark.com.br"
            value={email}
            onChange={(evento) => setEmail(evento.target.value)}
            disabled={ehEdicao}
            autoComplete="off"
          />

          <Select
            label="Perfil"
            name="perfil"
            options={OPCOES_DE_PERFIL}
            value={perfil}
            onChange={(evento) => setPerfil(evento.target.value as UserRole)}
          />

          {!ehEdicao && (
            <Input
              label="Senha provisória"
              name="senhaInicial"
              type="text"
              placeholder={`Mínimo de ${TAMANHO_MINIMO_DE_SENHA} caracteres`}
              value={senhaInicial}
              onChange={(evento) => setSenhaInicial(evento.target.value)}
              autoComplete="off"
            />
          )}

          {erroDoFormulario && (
            <p className="sp-usuarios__erro" role="alert">
              {erroDoFormulario}
            </p>
          )}

          <div className="sp-usuarios__form-acoes">
            <Button type="button" variant="secondary" onClick={fecharFormulario}>
              Cancelar
            </Button>
            <Button type="submit" isLoading={salvando}>
              Salvar
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={alvoDaSenha !== null}
        onClose={() => setAlvoDaSenha(null)}
        title="Redefinir senha"
        description={`${alvoDaSenha?.name} entra com esta senha e é obrigado a trocar em seguida.`}
      >
        <form
          className="sp-usuarios__form"
          onSubmit={(evento) => {
            evento.preventDefault();
            void confirmarRedefinicaoDeSenha();
          }}
        >
          <Input
            label="Senha provisória"
            name="novaSenha"
            type="text"
            placeholder={`Mínimo de ${TAMANHO_MINIMO_DE_SENHA} caracteres`}
            value={novaSenha}
            onChange={(evento) => setNovaSenha(evento.target.value)}
            autoComplete="off"
          />

          {erroDaSenha && (
            <p className="sp-usuarios__erro" role="alert">
              {erroDaSenha}
            </p>
          )}

          <div className="sp-usuarios__form-acoes">
            <Button type="button" variant="secondary" onClick={() => setAlvoDaSenha(null)}>
              Cancelar
            </Button>
            <Button type="submit" isLoading={redefinindoSenha}>
              Redefinir
            </Button>
          </div>
        </form>
      </Modal>

      <Modal
        isOpen={alvoDaConfirmacao !== null}
        onClose={() => setAlvoDaConfirmacao(null)}
        title={alvoDaConfirmacao?.isActive ? "Desativar acesso" : "Reativar acesso"}
        description={
          alvoDaConfirmacao?.isActive
            ? `${alvoDaConfirmacao?.name} perde o acesso na próxima ação que fizer no painel.`
            : `${alvoDaConfirmacao?.name} volta a conseguir entrar.`
        }
      >
        <div className="sp-usuarios__form">
          {erroDaConfirmacao && (
            <p className="sp-usuarios__erro" role="alert">
              {erroDaConfirmacao}
            </p>
          )}

          <div className="sp-usuarios__form-acoes">
            <Button type="button" variant="secondary" onClick={() => setAlvoDaConfirmacao(null)}>
              Cancelar
            </Button>
            <Button
              type="button"
              variant={alvoDaConfirmacao?.isActive ? "danger" : "primary"}
              isLoading={alterandoStatus}
              onClick={() => void confirmarAlteracaoDeStatus()}
            >
              {alvoDaConfirmacao?.isActive ? "Desativar" : "Ativar"}
            </Button>
          </div>
        </div>
      </Modal>
    </AppLayout>
  );
}
