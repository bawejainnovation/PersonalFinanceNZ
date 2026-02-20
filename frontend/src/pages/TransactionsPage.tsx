import { useEffect, useMemo, useRef, useState } from "react";
import { api, type Account, type Bank, type Category, type Contact, type Transaction } from "../lib/api";
import { formatCurrency, formatDate, toDateInput } from "../lib/format";

type DirectionFilter = "all" | "in" | "out";
type TransactionSort = "date_desc" | "amount_desc";
type ViewMode = "simple" | "advanced";

type EditDraft = {
  transactionTypeCategoryId?: string;
  spendTypeCategoryId?: string;
  note?: string;
};

export function TransactionsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [banks, setBanks] = useState<Bank[]>([]);
  const [categories, setCategories] = useState<Category[]>([]);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [transactions, setTransactions] = useState<Transaction[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isExporting, setIsExporting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [syncMonthsBack, setSyncMonthsBack] = useState(6);

  const today = useMemo(() => new Date(), []);
  const [fromDate, setFromDate] = useState(toDateInput(new Date(today.getFullYear(), today.getMonth() - 6, today.getDate())));
  const [toDate, setToDate] = useState(toDateInput(today));
  const [selectedAccountIds, setSelectedAccountIds] = useState<string[]>([]);
  const [includeBankTransfers, setIncludeBankTransfers] = useState(true);
  const [direction, setDirection] = useState<DirectionFilter>("all");
  const [selectedTransactionTypeIds, setSelectedTransactionTypeIds] = useState<string[]>([]);
  const [selectedSpendTypeIds, setSelectedSpendTypeIds] = useState<string[]>([]);
  const [selectedContactIds, setSelectedContactIds] = useState<string[]>([]);
  const [experimentalTransferMatching, setExperimentalTransferMatching] = useState(false);
  const [viewMode, setViewMode] = useState<ViewMode>("simple");
  const [moneyInSort, setMoneyInSort] = useState<TransactionSort>("date_desc");
  const [moneyOutSort, setMoneyOutSort] = useState<TransactionSort>("date_desc");

  const [edits, setEdits] = useState<Record<string, EditDraft>>({});
  const [savingId, setSavingId] = useState<string | null>(null);
  const transactionsRequestSequenceRef = useRef(0);

  const transactionTypeCategories = categories.filter((category) => category.categoryType === "TransactionType");
  const spendTypeCategories = categories.filter((category) => category.categoryType === "SpendType");

  const bankMap = useMemo(() => {
    return new Map(banks.map((bank) => [bank.key, bank]));
  }, [banks]);

  const sortedMoneyInTransactions = useMemo(() => {
    return sortTransactions(
      transactions.filter((transaction) => isMoneyIn(transaction)),
      moneyInSort,
    );
  }, [transactions, moneyInSort]);

  const sortedMoneyOutTransactions = useMemo(() => {
    return sortTransactions(
      transactions.filter((transaction) => isMoneyOut(transaction)),
      moneyOutSort,
    );
  }, [transactions, moneyOutSort]);

  const sortedTransferTransactions = useMemo(() => {
    return sortTransactions(
      transactions.filter((transaction) => transaction.isBankTransfer),
      "date_desc",
    );
  }, [transactions]);

  const moneyInDisplayTransactions = useMemo(
    () => sortedMoneyInTransactions.filter((transaction) => !transaction.isBankTransfer),
    [sortedMoneyInTransactions],
  );

  const moneyOutDisplayTransactions = useMemo(
    () => sortedMoneyOutTransactions.filter((transaction) => !transaction.isBankTransfer),
    [sortedMoneyOutTransactions],
  );

  const moneyInTotal = useMemo(
    () => moneyInDisplayTransactions.reduce((sum, transaction) => sum + Math.abs(transaction.amount), 0),
    [moneyInDisplayTransactions],
  );

  const moneyOutTotal = useMemo(
    () => moneyOutDisplayTransactions.reduce((sum, transaction) => sum + Math.abs(transaction.amount), 0),
    [moneyOutDisplayTransactions],
  );

  const transferInTotal = useMemo(
    () =>
      sortedTransferTransactions
        .filter((transaction) => isMoneyIn(transaction))
        .reduce((sum, transaction) => sum + Math.abs(transaction.amount), 0),
    [sortedTransferTransactions],
  );

  const transferOutTotal = useMemo(
    () =>
      sortedTransferTransactions
        .filter((transaction) => isMoneyOut(transaction))
        .reduce((sum, transaction) => sum + Math.abs(transaction.amount), 0),
    [sortedTransferTransactions],
  );

  useEffect(() => {
    void loadBaseData();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  useEffect(() => {
    if (accounts.length === 0) {
      return;
    }

    void loadTransactions();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [accounts.length, fromDate, toDate, includeBankTransfers, direction, selectedAccountIds.join(","), selectedTransactionTypeIds.join(","), selectedSpendTypeIds.join(","), selectedContactIds.join(","), experimentalTransferMatching]);

  async function loadBaseData() {
    setIsLoading(true);
    setError(null);

    try {
      const [loadedAccounts, loadedBanks, loadedCategories, loadedContacts] = await Promise.all([
        api.getAccounts(),
        api.getBanks(),
        api.getCategories(),
        api.getContacts(),
      ]);

      setAccounts(loadedAccounts);
      setBanks(loadedBanks);
      setCategories(loadedCategories);
      setContacts(loadedContacts);

      if (selectedAccountIds.length === 0) {
        setSelectedAccountIds(loadedAccounts.map((account) => account.id));
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load screen");
    } finally {
      setIsLoading(false);
    }
  }

  async function loadTransactions() {
    setError(null);

    try {
      const requestSequence = ++transactionsRequestSequenceRef.current;
      const query = new URLSearchParams();
      if (selectedAccountIds.length > 0) {
        query.set("accountIds", selectedAccountIds.join(","));
      }

      query.set("fromDate", fromDate);
      query.set("toDate", toDate);
      query.set("includeBankTransfers", includeBankTransfers ? "true" : "false");
      query.set("direction", direction);
      query.set("experimentalTransferMatching", experimentalTransferMatching ? "true" : "false");

      if (selectedTransactionTypeIds.length > 0) {
        query.set("transactionTypeCategoryIds", selectedTransactionTypeIds.join(","));
      }

      if (selectedSpendTypeIds.length > 0) {
        query.set("spendTypeCategoryIds", selectedSpendTypeIds.join(","));
      }

      if (selectedContactIds.length > 0) {
        query.set("contactIds", selectedContactIds.join(","));
      }

      const data = await api.getTransactions(query);
      if (requestSequence !== transactionsRequestSequenceRef.current) {
        return;
      }
      setTransactions(data);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load transactions");
    }
  }

  async function refreshCache() {
    setIsRefreshing(true);
    setError(null);

    try {
      await api.sync({ monthsBack: syncMonthsBack });
      await loadBaseData();
      await loadTransactions();
    } catch (refreshError) {
      setError(refreshError instanceof Error ? refreshError.message : "Refresh failed");
    } finally {
      setIsRefreshing(false);
    }
  }

  async function seedDemoData() {
    try {
      await api.seedDevData();
      await loadBaseData();
      await loadTransactions();
    } catch (seedError) {
      setError(seedError instanceof Error ? seedError.message : "Seed failed");
    }
  }

  async function exportCsv() {
    setIsExporting(true);
    setError(null);

    try {
      const blob = await api.exportAllTransactionsCsv();
      const downloadUrl = URL.createObjectURL(blob);
      const link = document.createElement("a");
      link.href = downloadUrl;
      link.download = `transactions-export-${new Date().toISOString().slice(0, 10)}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(downloadUrl);
    } catch (exportError) {
      setError(exportError instanceof Error ? exportError.message : "CSV export failed");
    } finally {
      setIsExporting(false);
    }
  }

  function onMultiSelectChange(event: React.ChangeEvent<HTMLSelectElement>, setter: (value: string[]) => void) {
    const values = Array.from(event.target.selectedOptions).map((option) => option.value);
    setter(values);
  }

  function getDraft(transaction: Transaction): EditDraft {
    return edits[transaction.id] ?? {
      transactionTypeCategoryId: transaction.transactionTypeCategoryId,
      spendTypeCategoryId: transaction.spendTypeCategoryId,
      note: transaction.note,
    };
  }

  async function saveAnnotation(transaction: Transaction) {
    const draft = getDraft(transaction);
    setSavingId(transaction.id);

    try {
      await api.updateTransactionAnnotation(transaction.id, {
        transactionTypeCategoryId: draft.transactionTypeCategoryId || undefined,
        spendTypeCategoryId: draft.spendTypeCategoryId || undefined,
        note: draft.note?.trim() || undefined,
      });

      await loadTransactions();
      setEdits((current) => {
        const next = { ...current };
        delete next[transaction.id];
        return next;
      });
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Annotation update failed");
    } finally {
      setSavingId(null);
    }
  }

  function sortTransactions(data: Transaction[], sortBy: TransactionSort): Transaction[] {
    const cloned = [...data];
    cloned.sort((left, right) => {
      if (sortBy === "amount_desc") {
        return Math.abs(right.amount) - Math.abs(left.amount);
      }

      return new Date(right.transactionDateUtc).getTime() - new Date(left.transactionDateUtc).getTime();
    });
    return cloned;
  }

  function isMoneyIn(transaction: Transaction): boolean {
    return transaction.direction === "In" || transaction.direction === 1;
  }

  function isMoneyOut(transaction: Transaction): boolean {
    return transaction.direction === "Out" || transaction.direction === 2;
  }

  function renderTransactionCard(transaction: Transaction) {
    const draft = getDraft(transaction);
    const bank = transaction.bankKey ? bankMap.get(transaction.bankKey) : undefined;

    return (
      <article key={transaction.id} className="transaction-row" data-testid="transaction-row">
        <div className="transaction-core">
          <div className="bank-badge-wrap">
            {bank ? <img src={bank.logoPath} alt={bank.name} className="bank-logo" /> : <span className="bank-logo-placeholder">Bank</span>}
          </div>

          <div>
            <p className="transaction-title" data-testid="transaction-description">
              {transaction.description}
            </p>
            <p className="transaction-meta">
              {formatDate(transaction.transactionDateUtc)} | {transaction.accountDescription || transaction.accountName}
            </p>
            {transaction.contactName ? <p className="transaction-meta">Contact: {transaction.contactName}</p> : null}
            {transaction.isBankTransfer ? <span className="transfer-badge">Bank transfers</span> : null}
          </div>

          <p className={`amount-tag ${isMoneyIn(transaction) ? "in" : "out"}`}>
            {formatCurrency(transaction.amount)}
          </p>
        </div>

        <div className="annotation-panel">
          <label>
            Transaction Type
            <select
              value={draft.transactionTypeCategoryId ?? ""}
              onChange={(event) =>
                setEdits((current) => ({
                  ...current,
                  [transaction.id]: {
                    ...draft,
                    transactionTypeCategoryId: event.target.value || undefined,
                  },
                }))
              }
            >
              <option value="">Unclassified</option>
              {transactionTypeCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            Type of spend
            <select
              value={draft.spendTypeCategoryId ?? ""}
              onChange={(event) =>
                setEdits((current) => ({
                  ...current,
                  [transaction.id]: {
                    ...draft,
                    spendTypeCategoryId: event.target.value || undefined,
                  },
                }))
              }
            >
              <option value="">Unclassified</option>
              {spendTypeCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            Note
            <input
              type="text"
              value={draft.note ?? ""}
              onChange={(event) =>
                setEdits((current) => ({
                  ...current,
                  [transaction.id]: {
                    ...draft,
                    note: event.target.value,
                  },
                }))
              }
            />
          </label>

          <button
            className="subtle-btn"
            disabled={savingId === transaction.id}
            onClick={() => saveAnnotation(transaction)}
          >
            {savingId === transaction.id ? "Saving..." : "Save annotation"}
          </button>
        </div>
      </article>
    );
  }

  if (isLoading) {
    return <p className="status-card">Loading dashboard...</p>;
  }

  return (
    <div className="page-space" data-testid="transactions-page">
      <section className="card filters-card">
        <div className="filters-title-row">
          <h2>Transaction Feed</h2>
          <div className="actions-inline">
            <label className="compact-field">
              Sync months
              <input
                min={1}
                max={24}
                type="number"
                value={syncMonthsBack}
                onChange={(event) => setSyncMonthsBack(Number(event.target.value))}
              />
            </label>
            <button className="primary-btn" disabled={isRefreshing} onClick={refreshCache} data-testid="refresh-cache-btn">
              {isRefreshing ? "Refreshing..." : "Refresh Cache"}
            </button>
            <button className="subtle-btn" onClick={seedDemoData}>
              Seed Demo Data
            </button>
            <button className="subtle-btn" onClick={exportCsv} disabled={isExporting} data-testid="export-csv-btn">
              {isExporting ? "Exporting..." : "Export CSV"}
            </button>
            <label>
              View mode
              <select value={viewMode} onChange={(event) => setViewMode(event.target.value as ViewMode)} aria-label="View mode">
                <option value="simple">Simple</option>
                <option value="advanced">Advanced</option>
              </select>
            </label>
            <label className="checkbox-field" htmlFor="experimentalTransferMatching">
              <input
                id="experimentalTransferMatching"
                checked={experimentalTransferMatching}
                onChange={(event) => setExperimentalTransferMatching(event.target.checked)}
                type="checkbox"
                data-testid="experimental-transfer-matching-checkbox"
              />
              Experimental transfer matching
            </label>
          </div>
        </div>

        <div className="filters-grid">
          <label>
            From
            <input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} />
          </label>
          <label>
            To
            <input type="date" value={toDate} onChange={(event) => setToDate(event.target.value)} />
          </label>
          <label>
            Money direction
            <select value={direction} onChange={(event) => setDirection(event.target.value as DirectionFilter)}>
              <option value="all">All</option>
              <option value="in">Money In</option>
              <option value="out">Money Out</option>
            </select>
          </label>
          <label className="checkbox-field" htmlFor="includeTransfers">
            <input
              id="includeTransfers"
              checked={includeBankTransfers}
              onChange={(event) => setIncludeBankTransfers(event.target.checked)}
              type="checkbox"
              data-testid="include-transfer-checkbox"
            />
            Include bank transfers
          </label>
        </div>

        <div className="filters-grid">
          <label>
            Accounts
            <select
              multiple
              value={selectedAccountIds}
              onChange={(event) => onMultiSelectChange(event, setSelectedAccountIds)}
            >
              {accounts.map((account) => (
                <option key={account.id} value={account.id}>
                  {account.customDescription || account.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            Transaction Type
            <select
              multiple
              value={selectedTransactionTypeIds}
              onChange={(event) => onMultiSelectChange(event, setSelectedTransactionTypeIds)}
            >
              {transactionTypeCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            Type of spend
            <select
              multiple
              value={selectedSpendTypeIds}
              onChange={(event) => onMultiSelectChange(event, setSelectedSpendTypeIds)}
            >
              {spendTypeCategories.map((category) => (
                <option key={category.id} value={category.id}>
                  {category.name}
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
      </section>

      {error ? <p className="error-card">{error}</p> : null}

      {viewMode === "simple" ? (
        <section className="list-stack" data-testid="transaction-list">
          {transactions.map((transaction) => renderTransactionCard(transaction))}
          {transactions.length === 0 ? <p className="status-card">No transactions match your current filters.</p> : null}
        </section>
      ) : (
        <section className="transaction-dual-columns" data-testid="transaction-list">
          <article className="card transaction-column-card">
            <div className="filters-title-row transaction-column-head">
              <div>
                <h3>Money In</h3>
                <p className="soft-copy">Total: {formatCurrency(moneyInTotal)}</p>
              </div>
              <label>
                Order
                <select
                  aria-label="Money In order"
                  value={moneyInSort}
                  onChange={(event) => setMoneyInSort(event.target.value as TransactionSort)}
                >
                  <option value="date_desc">Date (newest)</option>
                  <option value="amount_desc">Amount (high to low)</option>
                </select>
              </label>
            </div>

            <div className="list-stack transaction-column-list" data-testid="money-in-list">
              {moneyInDisplayTransactions.map((transaction) => renderTransactionCard(transaction))}
              {moneyInDisplayTransactions.length === 0 ? <p className="status-card">No money in transactions.</p> : null}
            </div>
          </article>

          <article className="card transaction-column-card">
            <div className="filters-title-row transaction-column-head">
              <div>
                <h3>Money Out</h3>
                <p className="soft-copy">Total: {formatCurrency(moneyOutTotal)}</p>
              </div>
              <label>
                Order
                <select
                  aria-label="Money Out order"
                  value={moneyOutSort}
                  onChange={(event) => setMoneyOutSort(event.target.value as TransactionSort)}
                >
                  <option value="date_desc">Date (newest)</option>
                  <option value="amount_desc">Amount (high to low)</option>
                </select>
              </label>
            </div>

            <div className="list-stack transaction-column-list" data-testid="money-out-list">
              {moneyOutDisplayTransactions.map((transaction) => renderTransactionCard(transaction))}
              {moneyOutDisplayTransactions.length === 0 ? <p className="status-card">No money out transactions.</p> : null}
            </div>
          </article>

          <article className="card transaction-column-card">
            <div className="filters-title-row transaction-column-head">
              <div>
                <h3>Bank Transfers</h3>
                <p className="soft-copy">
                  In: {formatCurrency(transferInTotal)} | Out: {formatCurrency(transferOutTotal)}
                </p>
              </div>
            </div>

            <div className="list-stack transaction-column-list" data-testid="bank-transfer-list">
              {sortedTransferTransactions.map((transaction) => renderTransactionCard(transaction))}
              {sortedTransferTransactions.length === 0 ? <p className="status-card">No bank transfer transactions.</p> : null}
            </div>
          </article>
        </section>
      )}
    </div>
  );
}
