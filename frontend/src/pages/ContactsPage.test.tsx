import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BrowserRouter } from "react-router-dom";
import { afterEach, describe, expect, test, vi } from "vitest";
import { ContactsPage } from "./ContactsPage";

function mockJsonResponse(body: unknown, status = 200) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

describe("ContactsPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  test("updates contacts query when accounts are deselected", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          { id: "a-1", akahuAccountId: "acc1", name: "A1", currency: "NZD", transactionCount: 1 },
          { id: "a-2", akahuAccountId: "acc2", name: "A2", currency: "NZD", transactionCount: 1 },
        ]);
      }

      if (url.includes("/api/contacts?")) {
        return mockJsonResponse([]);
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <ContactsPage />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/contacts?accountIds=a-1%2Ca-2"), expect.any(Object));
    });

    const accountSelect = screen.getByLabelText("Accounts");
    await userEvent.deselectOptions(accountSelect, ["a-1", "a-2"]);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/contacts?accountIds="), expect.any(Object));
    });
  });

  test("keeps selected contact and updates right panel details on click", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          { id: "a-1", akahuAccountId: "acc1", name: "A1", currency: "NZD", transactionCount: 1 },
        ]);
      }

      if (url.includes("/api/contacts?")) {
        return mockJsonResponse([
          { id: "c-1", displayName: "Alpha", confidence: "high", transactionCount: 2, moneyIn: 200, moneyOut: 100 },
          { id: "c-2", displayName: "Beta", confidence: "high", transactionCount: 3, moneyIn: 250, moneyOut: 40 },
        ]);
      }

      if (url.includes("/api/contacts/c-1?")) {
        return mockJsonResponse({
          id: "c-1",
          displayName: "Alpha",
          confidence: "high",
          monthlyCashflow: [],
          transactions: [],
        });
      }

      if (url.includes("/api/contacts/c-2?")) {
        return mockJsonResponse({
          id: "c-2",
          displayName: "Beta",
          confidence: "high",
          monthlyCashflow: [],
          transactions: [],
        });
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <ContactsPage />
      </BrowserRouter>,
    );

    const betaButton = await screen.findByRole("button", { name: /beta/i });
    await userEvent.click(betaButton);

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/contacts/c-2?accountIds=a-1"), expect.any(Object));
    });

    await waitFor(() => {
      expect(screen.getByRole("heading", { name: "Beta" })).toBeInTheDocument();
    });

    expect(betaButton.className).toContain("active");
  });
});
