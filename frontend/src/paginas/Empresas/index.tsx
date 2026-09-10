import { useMemo, useState } from "react";
import { Search } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import Input from "../../componentes/ui/Input";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import AppLayout from "../../componentes/layout/AppLayout";
import { COMPANY_RECORDS } from "../../services/mockApi";
import type { CompanyRecord } from "../../interfaces/Company";

const columns: TableColumn<CompanyRecord>[] = [
  { key: "legalName", header: "Empresa", render: (row) => row.legalName },
  { key: "document", header: "CNPJ", render: (row) => row.document },
  { key: "location", header: "Cidade/UF", render: (row) => row.location },
  {
    key: "accountCount",
    header: "Contas Contábeis",
    render: (row) => row.accountCount,
    align: "center",
  },
  {
    key: "status",
    header: "Status",
    render: (row) => (
      <StatusBadge label={row.status} tone={row.status === "Ativa" ? "green" : "neutral"} />
    ),
  },
];

export default function Empresas() {
  const [search, setSearch] = useState("");

  const records = useMemo(() => {
    const query = search.trim().toLocaleLowerCase("pt-BR");
    if (!query) return COMPANY_RECORDS;

    return COMPANY_RECORDS.filter(
      (record) =>
        record.legalName.toLocaleLowerCase("pt-BR").includes(query) ||
        record.document.includes(query) ||
        record.location.toLocaleLowerCase("pt-BR").includes(query)
    );
  }, [search]);

  return (
    <AppLayout>
      <AppHeader
        title="Empresas"
        subtitle="Consulte as empresas cadastradas no painel de conciliação"
      />

      <main className="sp-management">
        <div className="sp-management__toolbar">
          <div className="sp-management__search">
            <Input
              aria-label="Buscar empresas"
              placeholder="Buscar por empresa, CNPJ ou cidade"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              leadingIcon={<Search size={18} />}
            />
          </div>
          <span className="sp-management__count">
            {records.length} {records.length === 1 ? "empresa" : "empresas"}
          </span>
        </div>

        <section className="sp-management__table" aria-label="Lista de empresas">
          <Table
            columns={columns}
            data={records}
            getRowId={(row) => row.id}
            emptyMessage="Nenhuma empresa encontrada."
          />
        </section>
      </main>
    </AppLayout>
  );
}
