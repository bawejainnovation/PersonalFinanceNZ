export type CategoryType = "TransactionType" | "SpendType";
export type MoneyDirection = "In" | "Out" | 1 | 2;

export type Bank = {
  key: string;
  name: string;
  logoPath: string;
};

export type Account = {
  id: string;
  akahuAccountId: string;
  name: string;
  institutionName?: string;
  accountNumber?: string;
  currency: string;
  currentBalance?: number;
  nzBankKey?: string;
  customDescription?: string;
  lastSyncedAtUtc?: string;
  transactionCount: number;
};

export type Contact = {
  id: string;
  displayName: string;
  confidence: string;
  transactionCount: number;
  moneyIn: number;
  moneyOut: number;
};

export type ContactDetail = {
  id: string;
  displayName: string;
  confidence: string;
  monthlyCashflow: Array<{ year: number; month: number; moneyIn: number; moneyOut: number }>;
  transactions: Array<{
    transactionId: string;
    transactionDateUtc: string;
    description: string;
    accountName: string;
    amount: number;
    direction: "In" | "Out";
  }>;
};

export type Category = {
  id: string;
  categoryType: CategoryType;
  name: string;
};

export type Transaction = {
  id: string;
  akahuTransactionId: string;
  accountId: string;
  accountName: string;
  accountDescription?: string;
  bankKey?: string;
  amount: number;
  direction: MoneyDirection;
  description: string;
  merchantName?: string;
  transactionDateUtc: string;
  isBankTransfer: boolean;
  transactionTypeCategoryId?: string;
  transactionTypeCategoryName?: string;
  spendTypeCategoryId?: string;
  spendTypeCategoryName?: string;
  note?: string;
  contactId?: string;
  contactName?: string;
};

export type CategoryCashflow = {
  categoryId?: string;
  categoryName: string;
  categoryType: CategoryType;
  moneyIn: number;
  moneyOut: number;
  net: number;
};

export type MonthlyCategoryOverview = {
  year: number;
  month: number;
  categoryId?: string;
  categoryName: string;
  categoryType: CategoryType;
  moneyIn: number;
  moneyOut: number;
};

export type AccountBalance = {
  accountId: string;
  accountName: string;
  accountNumber?: string;
  currentBalance?: number;
  currency: string;
};

const apiBase = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5072";

async function apiFetch<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${apiBase}${path}`, {
    headers: {
      "Content-Type": "application/json",
      ...(init?.headers ?? {}),
    },
    ...init,
  });

  if (!response.ok) {
    const errorText = await response.text();
    throw new Error(errorText || `Request failed: ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const api = {
  exportAllTransactionsCsv: async () => {
    const response = await fetch(`${apiBase}/api/transactions/export/csv`, {
      method: "GET",
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(errorText || `Request failed: ${response.status}`);
    }

    return response.blob();
  },
  getBanks: () => apiFetch<Bank[]>("/api/banks"),
  getAccounts: () => apiFetch<Account[]>("/api/accounts"),
  updateAccountProfile: (id: string, payload: { nzBankKey?: string; customDescription?: string }) =>
    apiFetch<Account>(`/api/accounts/${id}/profile`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  updateAccountProfiles: (items: Array<{ accountId: string; nzBankKey?: string; customDescription?: string }>) =>
    apiFetch<Account[]>("/api/accounts/profiles", {
      method: "PUT",
      body: JSON.stringify({ items }),
    }),
  sync: (payload: { monthsBack?: number; fromDate?: string; toDate?: string }) =>
    apiFetch<{ accountsSynced: number; transactionsSynced: number }>("/api/sync", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
  getTransactions: (query: URLSearchParams) => apiFetch<Transaction[]>(`/api/transactions?${query.toString()}`),
  updateTransactionAnnotation: (
    id: string,
    payload: { transactionTypeCategoryId?: string; spendTypeCategoryId?: string; note?: string },
  ) =>
    apiFetch<void>(`/api/transactions/${id}/annotation`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  getContacts: (query?: URLSearchParams) =>
    apiFetch<Contact[]>(query ? `/api/contacts?${query.toString()}` : "/api/contacts"),
  getContactDetail: (id: string, query?: URLSearchParams) =>
    apiFetch<ContactDetail>(query ? `/api/contacts/${id}?${query.toString()}` : `/api/contacts/${id}`),
  getCategories: (categoryType?: CategoryType) =>
    apiFetch<Category[]>(
      categoryType ? `/api/categories?categoryType=${encodeURIComponent(categoryType)}` : "/api/categories",
    ),
  createCategory: (payload: { categoryType: CategoryType; name: string }) =>
    apiFetch<Category>("/api/categories", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
  updateCategory: (id: string, payload: { name: string }) =>
    apiFetch<Category>(`/api/categories/${id}`, {
      method: "PUT",
      body: JSON.stringify(payload),
    }),
  deleteCategory: (id: string) =>
    apiFetch<void>(`/api/categories/${id}`, {
      method: "DELETE",
    }),
  getCategoryCashflow: (query: URLSearchParams) =>
    apiFetch<CategoryCashflow[]>(`/api/analytics/category-cashflow?${query.toString()}`),
  getMonthlyOverview: (query: URLSearchParams) =>
    apiFetch<MonthlyCategoryOverview[]>(`/api/analytics/monthly-overview?${query.toString()}`),
  getAccountBalances: (query: URLSearchParams) =>
    apiFetch<AccountBalance[]>(`/api/analytics/account-balances?${query.toString()}`),
  seedDevData: () =>
    apiFetch<{ status: string }>("/api/dev/seed", {
      method: "POST",
      body: JSON.stringify({}),
    }),
};
