import { defineConfig } from "@playwright/test";

export default defineConfig({
  testDir: "./e2e",
  timeout: 30_000,
  expect: {
    timeout: 8_000,
  },
  use: {
    baseURL: "http://127.0.0.1:4173",
    trace: "retain-on-failure",
  },
  webServer: [
    {
      command:
        "ASPNETCORE_URLS=http://127.0.0.1:5072 ASPNETCORE_ENVIRONMENT=Development dotnet run --no-launch-profile --project ../backend/FinancialInsights.Api/FinancialInsights.Api.csproj",
      url: "http://127.0.0.1:5072/health",
      reuseExistingServer: true,
      cwd: ".",
      timeout: 120_000,
    },
    {
      command: "VITE_API_BASE_URL=http://127.0.0.1:5072 npm run dev -- --host 127.0.0.1 --port 4173",
      url: "http://127.0.0.1:4173",
      reuseExistingServer: true,
      cwd: ".",
      timeout: 120_000,
    },
  ],
});
