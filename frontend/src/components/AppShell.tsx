import { NavLink } from "react-router-dom";

type Props = {
  children: React.ReactNode;
};

const navItems = [
  { label: "Transactions", to: "/transactions" },
  { label: "Accounts", to: "/accounts" },
  { label: "Contacts", to: "/contacts" },
  { label: "Categories", to: "/categories" },
  { label: "Analytics", to: "/analytics" },
];

export function AppShell({ children }: Props) {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand-panel">
          <p className="brand-eyebrow">Financial Insights</p>
          <h1>M&Ms Cashflow</h1>
        </div>
        <nav>
          {navItems.map((item) => (
            <NavLink
              key={item.to}
              className={({ isActive }) => `nav-link ${isActive ? "active" : ""}`}
              to={item.to}
            >
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <main className="content">{children}</main>

      <nav className="mobile-nav" aria-label="Mobile navigation">
        {navItems.map((item) => (
          <NavLink
            key={item.to}
            className={({ isActive }) => `mobile-nav-link ${isActive ? "active" : ""}`}
            to={item.to}
          >
            {item.label}
          </NavLink>
        ))}
      </nav>
    </div>
  );
}
