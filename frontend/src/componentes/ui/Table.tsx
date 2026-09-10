import type { ReactNode } from "react";

export interface TableColumn<T> {
  key: string;
  header: string;
  render: (row: T) => ReactNode;
  align?: "left" | "right" | "center";
}

interface TableProps<T> {
  columns: TableColumn<T>[];
  data: T[];
  getRowId: (row: T) => string;
  emptyMessage?: string;
}

export default function Table<T>({ columns, data, getRowId, emptyMessage }: TableProps<T>) {
  return (
    <div className="sp-table-wrapper">
      <table className="sp-table">
        <thead>
          <tr>
            {columns.map((column) => (
              <th key={column.key} style={{ textAlign: column.align ?? "left" }}>
                {column.header}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {data.length === 0 ? (
            <tr>
              <td colSpan={columns.length} className="sp-table__empty">
                {emptyMessage ?? "Nenhum registro encontrado."}
              </td>
            </tr>
          ) : (
            data.map((row) => (
              <tr key={getRowId(row)}>
                {columns.map((column) => (
                  <td key={column.key} style={{ textAlign: column.align ?? "left" }}>
                    {column.render(row)}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
      </table>
    </div>
  );
}

