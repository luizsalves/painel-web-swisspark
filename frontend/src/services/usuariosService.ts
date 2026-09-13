import api from "./api";
import type { AlteracaoUsuario, NovoUsuario, Usuario } from "../interfaces/Usuario";

const RECURSO = "/usuarios";

export async function listarUsuarios(): Promise<Usuario[]> {
  const { data } = await api.get<Usuario[]>(RECURSO);
  return data;
}

export async function criarUsuario(dados: NovoUsuario): Promise<Usuario> {
  const { data } = await api.post<Usuario>(RECURSO, dados);
  return data;
}

export async function atualizarUsuario(id: string, dados: AlteracaoUsuario): Promise<Usuario> {
  const { data } = await api.put<Usuario>(`${RECURSO}/${id}`, dados);
  return data;
}

/** Usuário não é excluído, é desativado — a auditoria depende do registro continuar existindo. */
export async function alterarStatusUsuario(id: string, isActive: boolean): Promise<Usuario> {
  const { data } = await api.patch<Usuario>(`${RECURSO}/${id}/status`, { isActive });
  return data;
}

/** Define uma senha provisória para outra pessoa: ela é obrigada a trocar no próximo acesso. */
export async function redefinirSenhaUsuario(id: string, novaSenha: string): Promise<Usuario> {
  const { data } = await api.post<Usuario>(`${RECURSO}/${id}/senha`, { novaSenha });
  return data;
}
