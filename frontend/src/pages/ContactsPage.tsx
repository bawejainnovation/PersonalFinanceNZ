import { useEffect, useMemo, useRef, useState } from "react";
import { api, type Account, type Contact, type ContactDetail } from "../lib/api";
import { formatCurrency, formatDate } from "../lib/format";

type ContactSort = "amount_desc" | "amount_asc";

export function ContactsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [contacts, setContacts] = useState<Contact[]>([]);
  const [selectedContactId, setSelectedContactId] = useState<string | null>(null);
  const [detail, setDetail] = useState<ContactDetail | null>(null);
  const [selectedAccountIds, setSelectedAccountIds] = useState<string[]>([]);
  const [sortBy, setSortBy] = useState<ContactSort>("amount_desc");
  const [error, setError] = useState<string | null>(null);
  const detailPanelRef = useRef<HTMLDivElement | null>(null);
  const detailRequestSequenceRef = useRef(0);

  useEffect(() => {
    void loadAccountsAndContacts();
  }, []);

  useEffect(() => {
    if (selectedAccountIds.length > 0 || accounts.length > 0) {
      void loadContacts();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedAccountIds.join(","), accounts.length]);

  useEffect(() => {
    if (!selectedContactId) {
      return;
    }

    void loadDetail(selectedContactId);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedContactId, selectedAccountIds.join(",")]);

  async function loadAccountsAndContacts() {
    setError(null);
    try {
      const accountData = await api.getAccounts();
      setAccounts(accountData);
      setSelectedAccountIds(accountData.map((account) => account.id));

      const query = new URLSearchParams();
      query.set("accountIds", accountData.map((account) => account.id).join(","));
      const contactData = await api.getContacts(query);
      setContacts(contactData);
      if (contactData.length > 0 && !selectedContactId) {
        setSelectedContactId(contactData[0].id);
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load contacts");
    }
  }

  async function loadContacts() {
    setError(null);
    try {
      const query = new URLSearchParams();
      query.set("accountIds", selectedAccountIds.join(","));

      const data = await api.getContacts(query);
      setContacts(data);
      if (data.length === 0) {
        setSelectedContactId(null);
        setDetail(null);
        return;
      }

      if (!selectedContactId || !data.some((contact) => contact.id === selectedContactId)) {
        setSelectedContactId(data[0].id);
      }
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load contacts");
    }
  }

  async function loadDetail(contactId: string) {
    setError(null);
    try {
      const requestSequence = ++detailRequestSequenceRef.current;
      const query = new URLSearchParams();
      query.set("accountIds", selectedAccountIds.join(","));
      const data = await api.getContactDetail(contactId, query);
      if (requestSequence !== detailRequestSequenceRef.current) {
        return;
      }
      setDetail(data);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load contact details");
    }
  }

  function selectContact(contactId: string) {
    if (contactId === selectedContactId && detailPanelRef.current) {
      detailPanelRef.current.scrollTop = 0;
      return;
    }

    setSelectedContactId(contactId);
  }

  useEffect(() => {
    if (!detailPanelRef.current) {
      return;
    }

    detailPanelRef.current.scrollTop = 0;
  }, [selectedContactId]);

  const sortedContacts = useMemo(() => {
    const cloned = [...contacts];
    cloned.sort((left, right) => {
      const leftAmount = Math.abs(left.moneyIn) + Math.abs(left.moneyOut);
      const rightAmount = Math.abs(right.moneyIn) + Math.abs(right.moneyOut);

      if (sortBy === "amount_asc") {
        return leftAmount - rightAmount;
      }

      return rightAmount - leftAmount;
    });
    return cloned;
  }, [contacts, sortBy]);

  function onAccountFilterChange(event: React.ChangeEvent<HTMLSelectElement>) {
    const values = Array.from(event.target.selectedOptions).map((option) => option.value);
    setSelectedAccountIds(values);
  }

  const maxValue = useMemo(() => {
    if (!detail) {
      return 1;
    }

    return Math.max(
      1,
      ...detail.monthlyCashflow.map((row) => Math.max(row.moneyIn, row.moneyOut)),
    );
  }, [detail]);

  return (
    <div className="page-space">
      {error ? <p className="error-card">{error}</p> : null}

      <div className="two-column-grid">
        <section className="card contacts-list-card">
          <div className="filters-title-row">
            <h2>Contacts directory</h2>
            <div className="actions-inline">
              <label>
                Order by amount
                <select value={sortBy} onChange={(event) => setSortBy(event.target.value as ContactSort)}>
                  <option value="amount_desc">Highest first</option>
                  <option value="amount_asc">Lowest first</option>
                </select>
              </label>
              <label>
                Accounts
                <select multiple value={selectedAccountIds} onChange={onAccountFilterChange}>
                  {accounts.map((account) => (
                    <option key={account.id} value={account.id}>
                      {account.customDescription || account.name}
                    </option>
                  ))}
                </select>
              </label>
            </div>
          </div>

          <div className="list-stack contacts-list">
            {sortedContacts.map((contact) => (
              <button
                className={`contact-item ${contact.id === selectedContactId ? "active" : ""}`}
                key={contact.id}
                onClick={() => selectContact(contact.id)}
                type="button"
              >
                <span>{contact.displayName}</span>
                <span className="soft-copy">
                  {contact.transactionCount} txns | In {formatCurrency(contact.moneyIn)} | Out {formatCurrency(contact.moneyOut)}
                </span>
              </button>
            ))}
          </div>
        </section>

        <section className="card contacts-detail-card">
          <h2>{detail?.displayName || "Contact details"}</h2>
          <p className="soft-copy">Confidence: {detail?.confidence || "-"}</p>

          <div className="contact-detail-panel" ref={detailPanelRef}>
            <div className="bar-chart-grid">
              {(detail?.monthlyCashflow ?? []).map((row) => (
                <div className="bar-group" key={`${row.year}-${row.month}`}>
                  <div className="bar-stack">
                    <div className="bar-in" style={{ height: `${(row.moneyIn / maxValue) * 120}px` }} />
                    <div className="bar-out" style={{ height: `${(row.moneyOut / maxValue) * 120}px` }} />
                  </div>
                  <p className="bar-label">{`${row.year}-${`${row.month}`.padStart(2, "0")}`}</p>
                </div>
              ))}
            </div>

            <div className="table-card">
              <h3>Transactions</h3>
              <table>
                <thead>
                  <tr>
                    <th>Date</th>
                    <th>Description</th>
                    <th>Account</th>
                    <th>Direction</th>
                    <th>Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {(detail?.transactions ?? []).map((transaction) => (
                    <tr key={transaction.transactionId}>
                      <td>{formatDate(transaction.transactionDateUtc)}</td>
                      <td>{transaction.description}</td>
                      <td>{transaction.accountName}</td>
                      <td>{transaction.direction}</td>
                      <td>{formatCurrency(transaction.amount)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </section>
      </div>
    </div>
  );
}
