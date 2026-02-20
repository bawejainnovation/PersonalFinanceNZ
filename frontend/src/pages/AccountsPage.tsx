import { useEffect, useMemo, useState } from "react";
import { api, type Account, type Bank } from "../lib/api";
import { formatCurrency } from "../lib/format";

type ProfileDraft = {
  nzBankKey?: string;
  customDescription?: string;
};

export function AccountsPage() {
  const [accounts, setAccounts] = useState<Account[]>([]);
  const [banks, setBanks] = useState<Bank[]>([]);
  const [drafts, setDrafts] = useState<Record<string, ProfileDraft>>({});
  const [isSavingAll, setIsSavingAll] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  const bankMap = useMemo(() => new Map(banks.map((bank) => [bank.key, bank])), [banks]);

  async function load() {
    setError(null);

    try {
      const [accountResponse, bankResponse] = await Promise.all([api.getAccounts(), api.getBanks()]);
      setAccounts(accountResponse);
      setBanks(bankResponse);
      setDrafts(
        Object.fromEntries(
          accountResponse.map((account) => [
            account.id,
            {
              nzBankKey: account.nzBankKey,
              customDescription: account.customDescription,
            },
          ]),
        ),
      );
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load accounts");
    }
  }

  async function saveAll() {
    setIsSavingAll(true);
    setError(null);

    try {
      const payload = accounts.map((account) => {
        const draft = drafts[account.id] ?? {};
        return {
          accountId: account.id,
          nzBankKey: draft.nzBankKey,
          customDescription: draft.customDescription,
        };
      });

      await api.updateAccountProfiles(payload);
      await load();
    } catch (saveError) {
      setError(saveError instanceof Error ? saveError.message : "Failed to save account profiles");
    } finally {
      setIsSavingAll(false);
    }
  }

  return (
    <div className="page-space">
      <section className="card">
        <div className="filters-title-row">
          <div>
            <h2>Account Profiles</h2>
            <p className="soft-copy">
              Pick a common NZ bank and set a custom account description. The logo selection is reused in the transaction feed.
            </p>
          </div>
          <button className="primary-btn" disabled={isSavingAll} onClick={saveAll}>
            {isSavingAll ? "Saving..." : "Save all changes"}
          </button>
        </div>
      </section>

      {error ? <p className="error-card">{error}</p> : null}

      <section className="list-stack">
        {accounts.map((account) => {
          const draft = drafts[account.id] ?? {};
          const selectedBank = draft.nzBankKey ? bankMap.get(draft.nzBankKey) : undefined;

          return (
            <article key={account.id} className="card account-card">
              <div className="account-card-head">
                <div>
                  <h3>{account.name}</h3>
                  <p className="soft-copy">
                    Akahu ID: {account.akahuAccountId} | {account.institutionName || "Unknown institution"}
                  </p>
                  <p className="soft-copy">
                    Account number: {account.accountNumber || "Not available"} | Transactions: {account.transactionCount}
                  </p>
                  <p className="soft-copy">
                    Current balance: {account.currentBalance !== undefined ? formatCurrency(account.currentBalance, account.currency) : "Not available"}
                  </p>
                </div>
                {selectedBank ? <img src={selectedBank.logoPath} alt={selectedBank.name} className="bank-logo-large" /> : null}
              </div>

              <div className="account-grid">
                <label>
                  NZ bank
                  <select
                    value={draft.nzBankKey ?? ""}
                    onChange={(event) =>
                      setDrafts((current) => ({
                        ...current,
                        [account.id]: {
                          ...draft,
                          nzBankKey: event.target.value || undefined,
                        },
                      }))
                    }
                  >
                    <option value="">Unassigned</option>
                    {banks.map((bank) => (
                      <option key={bank.key} value={bank.key}>
                        {bank.name}
                      </option>
                    ))}
                  </select>
                </label>

                <label>
                  Account description
                  <input
                    placeholder="Everyday spending"
                    type="text"
                    value={draft.customDescription ?? ""}
                    onChange={(event) =>
                      setDrafts((current) => ({
                        ...current,
                        [account.id]: {
                          ...draft,
                          customDescription: event.target.value,
                        },
                      }))
                    }
                  />
                </label>
              </div>
            </article>
          );
        })}
      </section>
    </div>
  );
}
