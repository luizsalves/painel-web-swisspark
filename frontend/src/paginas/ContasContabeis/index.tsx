import { useMemo, useState } from "react";
import { Search } from "lucide-react";
import AppHeader from "../../componentes/layout/AppHeader";
import Input from "../../componentes/ui/Input";
import StatusBadge from "../../componentes/ui/StatusBadge";
import Table from "../../componentes/ui/Table";
import type { TableColumn } from "../../componentes/ui/Table";
import AppLayout from "../../componentes/layout/AppLayout";
import { ACCOUNTING_ACCOUNTS, COMPANY_OPTIONS } from "../../services/mockApi";
import type { AccountingAccount } from "../../interfaces/Company";

const columns: TableColumn<AccountingAccount>[] = [
  { key: "code", header: "Código", render: (row) => row.code },
  { key: "description", header: "Descrição", render: (row) => row.description },
  { key: "company", header: "Empresa", render: (row) => row.company },
  { key: "priority", header: "Prioridade", render: (row) => <StatusBadge label={row.priority} /> },
  { key: "status", header: "Status", render: (row) => <StatusBadge label={row.status} /> },
  { key: "responsible", header: "Responsável", render: (row) => row.responsible },
];

export default function ContasContabeis() {
  const [company, setCompany] = useState("todas");
  const [search, setSearch] = useState("");

  const records = useMemo(() => {
    const query = search.trim().toLocaleLowerCase("pt-BR");

    return ACCOUNTING_ACCOUNTS.filter((record) => {
      const matchesCompany = company === "todas" || record.companyValue === company;
      const matchesSearch =
        !query ||
        record.code.includes(query) ||
        record.description.toLocaleLowerCase("pt-BR").includes(query) ||
        record.responsible.toLocaleLowerCase("pt-BR").includes(query);

      return matchesCompany && matchesSearch;
    });
  }, [company, search]);

  return (
    <AppLayout>
      <AppHeader
        title="Contas Contábeis"
        subtitle="Consulte as contas e o andamento de cada conciliação"
        company={company}
        companyOptions={COMPANY_OPTIONS}
        onCompanyChange={setCompany}
      />

      <main className="sp-management">
        <div className="sp-management__toolbar">
          <div className="sp-management__search">
            <Input
              aria-label="Buscar contas contábeis"
              placeholder="Buscar por código, descrição ou responsável"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              leadingIcon={<Search size={18} />}
            />
          </div>
          <span className="sp-management__count">
            {records.length} {records.length === 1 ? "conta" : "contas"}
          </span>
        </div>

        <section className="sp-management__table" aria-label="Lista de contas contábeis">
          <Table
            columns={columns}
            data={records}
            getRowId={(row) => row.id}
            emptyMessage="Nenhuma conta contábil encontrada."
          />
        </section>
      </main>
    </AppLayout>
  );
}
