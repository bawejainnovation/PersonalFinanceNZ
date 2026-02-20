import { Navigate, Route, Routes } from "react-router-dom";
import { AppShell } from "./components/AppShell";
import { AccountsPage } from "./pages/AccountsPage";
import { AnalyticsPage } from "./pages/AnalyticsPage";
import { CategoriesPage } from "./pages/CategoriesPage";
import { ContactsPage } from "./pages/ContactsPage";
import { TransactionsPage } from "./pages/TransactionsPage";

export default function App() {
  return (
    <AppShell>
      <Routes>
        <Route path="/transactions" element={<TransactionsPage />} />
        <Route path="/accounts" element={<AccountsPage />} />
        <Route path="/contacts" element={<ContactsPage />} />
        <Route path="/categories" element={<CategoriesPage />} />
        <Route path="/analytics" element={<AnalyticsPage />} />
        <Route path="*" element={<Navigate replace to="/transactions" />} />
      </Routes>
    </AppShell>
  );
}
