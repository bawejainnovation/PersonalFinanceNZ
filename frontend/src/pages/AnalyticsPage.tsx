import { useEffect, useMemo, useState } from "react";
import {
  api,
  type Account,
  type AccountBalance,
  type CategoryCashflow,
  type CategoryType,
  type Contact,
  type MonthlyCategoryOverview,
} from "../lib/api";
import { formatCurrency, toDateInput } from "../lib/format";

export function AnalyticsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [balances, setBalances] = useState<AccountBalance[]>([]);
  const [cashflow, setCashflow] = useState<CategoryCashflow[]>([]);
  const [monthlyRows, setMonthlyRows] = useState<MonthlyCategoryOverview[]>([]);
  const [categoryType, setCategoryType] = useState<CategoryType>("TransactionType");
  const [fromDate, setFromDate] = useState(toDateInput(new Date(new Date().getFullYear(), new Date().getMonth() - 6, 1)));
  const [toDate, setToDate] = useState(toDateInput(new Date()));
  const [includedAccountIds, setIncludedAccountIds] = useState<string[]>([]);
  const [excludedAccountIds, setExcludedAccountIds] = useState<string[]>([]);
  const [selectedContactIds, setSelectedContactIds] = useState<string[]>([]);
  const [experimentalTransferMatching, setExperimentalTransferMatching] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void loadStaticData();
  }, []);

  useEffect(() => {
    void load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [categoryType, fromDate, toDate, includedAccountIds.join(","), excludedAccountIds.join(","), selectedContactIds.join(","), experimentalTransferMatching]);

  async function loadStaticData() {
    try {
      const [accountData, contactData] = await Promise.all([api.getAccounts(), api.getContacts()]);
      setAccounts(accountData);
      setContacts(contactData);
      if (includedAccountIds.length === 0) {
        setIncludedAccountIds(accountData.map((account) => account.id));
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed loading static analytics data");
    }
  }

  async function load() {
    setError(null);

    try {
      const query = new URLSearchParams({
        categoryType,
        fromDate,
        toDate,
      });

      if (includedAccountIds.length > 0) {
        query.set("accountIds", includedAccountIds.join(","));
      }

      if (excludedAccountIds.length > 0) {
        query.set("excludedAccountIds", excludedAccountIds.join(","));
      }

      if (selectedContactIds.length > 0) {
        query.set("contactIds", selectedContactIds.join(","));
      }
      query.set("experimentalTransferMatching", experimentalTransferMatching ? "true" : "false");

      const [cashflowData, monthlyData, balanceData] = await Promise.all([
        api.getCategoryCashflow(query),
        api.getMonthlyOverview(query),
        api.getAccountBalances(query),
      ]);

      setCashflow(cashflowData);
      setMonthlyRows(monthlyData);
      setBalances(balanceData);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Analytics load failed");
    }
  }

  function onMultiSelectChange(
    event: React.ChangeEvent<HTMLSelectElement>,
    setter: React.Dispatch<React.SetStateAction<string[]>>,
  ) {
    const values = Array.from(event.target.selectedOptions).map((option) => option.value);
    setter(values);
  }

  const totals = useMemo(() => {
    return cashflow.reduce(
      (accumulator, row) => {
        accumulator.moneyIn += row.moneyIn;
        accumulator.moneyOut += row.moneyOut;
        return accumulator;
      },
      { moneyIn: 0, moneyOut: 0 },
    );
  }, [cashflow]);

  const monthlyBars = useMemo(() => {
    const grouped = new Map<string, { label: string; moneyIn: number; moneyOut: number }>();
    for (const row of monthlyRows) {
      const key = `${row.year}-${`${row.month}`.padStart(2, "0")}`;
      const existing = grouped.get(key) ?? { label: key, moneyIn: 0, moneyOut: 0 };
      existing.moneyIn += row.moneyIn;
      existing.moneyOut += row.moneyOut;
      grouped.set(key, existing);
    }
    return Array.from(grouped.values()).sort((a, b) => a.label.localeCompare(b.label));
  }, [monthlyRows]);

  const maxMonthlyValue = useMemo(
    () => Math.max(1, ...monthlyBars.map((row) => Math.max(row.moneyIn, row.moneyOut))),
    [monthlyBars],
  );

  return (
    <div className="page-space">
      <section className="card">
        <div className="filters-title-row">
          <h2>Cashflow by category</h2>
          <div className="actions-inline">
            <label>
              Category view
              <select value={categoryType} onChange={(event) => setCategoryType(event.target.value as CategoryType)}>
                <option value="TransactionType">Transaction Type</option>
                <option value="SpendType">Type of spend</option>
              </select>
            </label>
            <label>
              From
              <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
            </label>
            <label>
              To
              <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
            </label>
            <label className="checkbox-field" htmlFor="analyticsExperimentalTransferMatching">
              <input
                id="analyticsExperimentalTransferMatching"
                checked={experimentalTransferMatching}
                onChange={(event) => setExperimentalTransferMatching(event.target.checked)}
                type="checkbox"
              />
              Experimental transfer matching
            </label>
          </div>
        </div>

        <div className="filters-grid">
          <label>
            Include accounts
            <select multiple value={includedAccountIds} onChange={(event) => onMultiSelectChange(event, setIncludedAccountIds)}>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.customDescription || account.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Exclude accounts
            <select multiple value={excludedAccountIds} onChange={(event) => onMultiSelectChange(event, setExcludedAccountIds)}>
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.customDescription || account.name}
                </option>
              ))}
            </select>
          </label>
          <label>
            Contacts
            <select multiple value={selectedContactIds} onChange={(event) => onMultiSelectChange(event, setSelectedContactIds)}>
              {contacts.map((contact) => (
                <option key={contact.id} value={contact.id}>
                  {contact.displayName}
                </option>
              ))}
            </select>
          </label>
        </div>

        <div className="kpi-grid">
          <article className="kpi-card">
            <p>Money in</p>
            <strong>{formatCurrency(totals.moneyIn)}</strong>
          </article>
          <article className="kpi-card">
            <p>Money out</p>
            <strong>{formatCurrency(totals.moneyOut)}</strong>
          </article>
          <article className="kpi-card">
            <p>Net</p>
            <strong>{formatCurrency(totals.moneyIn - totals.moneyOut)}</strong>
          </article>
        </div>
      </section>

      {error ? <p className="error-card">{error}</p> : null}

      <section className="card table-card">
        <h3>Account balances</h3>
        <table>
          <thead>
            <tr>
              <th>Account</th>
              <th>Number</th>
              <th>Balance</th>
            </tr>
          </thead>
          <tbody>
            {balances.map((row) => (
              <tr key={row.accountId}>
                <td>{row.accountName}</td>
                <td>{row.accountNumber || "-"}</td>
                <td>{row.currentBalance !== undefined ? formatCurrency(row.currentBalance, row.currency) : "Not available"}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="card table-card">
        <h3>Category totals</h3>
        <table>
          <thead>
            <tr>
              <th>Category</th>
              <th>Money in</th>
              <th>Money out</th>
              <th>Net</th>
            </tr>
          </thead>
          <tbody>
            {cashflow.map((row) => (
              <tr key={`${row.categoryId ?? "none"}-${row.categoryName}`}>
                <td>{row.categoryName}</td>
                <td>{formatCurrency(row.moneyIn)}</td>
                <td>{formatCurrency(row.moneyOut)}</td>
                <td>{formatCurrency(row.net)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="card">
        <h3>Monthly in/out bar chart</h3>
        <div className="bar-chart-grid">
          {monthlyBars.map((row) => (
            <div className="bar-group" key={row.label}>
              <div className="bar-stack">
                <div className="bar-in" style={{ height: `${(row.moneyIn / maxMonthlyValue) * 120}px` }} title={`In: ${formatCurrency(row.moneyIn)}`} />
                <div className="bar-out" style={{ height: `${(row.moneyOut / maxMonthlyValue) * 120}px` }} title={`Out: ${formatCurrency(row.moneyOut)}`} />
              </div>
              <p className="bar-label">{row.label}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="card table-card">
        <h3>Monthly breakdown</h3>
        <table>
          <thead>
            <tr>
              <th>Month</th>
              <th>Category</th>
              <th>Money in</th>
              <th>Money out</th>
            </tr>
          </thead>
          <tbody>
            {monthlyRows.map((row) => (
              <tr key={`${row.year}-${row.month}-${row.categoryId ?? "none"}`}>
                <td>{`${row.year}-${`${row.month}`.padStart(2, "0")}`}</td>
                <td>{row.categoryName}</td>
                <td>{formatCurrency(row.moneyIn)}</td>
                <td>{formatCurrency(row.moneyOut)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>
    </div>
  );
}
