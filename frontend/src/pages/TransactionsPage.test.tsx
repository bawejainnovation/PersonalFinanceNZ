import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, describe, expect, test, vi } from "vitest";
import { TransactionsPage } from "./TransactionsPage";

function mockJsonResponse(body: unknown) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status: 200,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

describe("TransactionsPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  test("renders transactions from API", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          {
            id: "acc-1",
            akahuAccountId: "a1",
            name: "Everyday",
            accountNumber: "01-0000-0000000-00",
            currency: "NZD",
            transactionCount: 1,
          },
        ]);
      }

      if (url.endsWith("/api/banks")) {
        return mockJsonResponse([{ key: "anz", name: "ANZ", logoPath: "/banks/anz.svg" }]);
      }

      if (url.includes("/api/categories")) {
        return mockJsonResponse([]);
      }

      if (url.includes("/api/contacts")) {
        return mockJsonResponse([]);
      }

      if (url.includes("/api/transactions")) {
        return mockJsonResponse([
          {
            id: "txn-1",
            akahuTransactionId: "t1",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: -25.5,
            direction: "Out",
            description: "Coffee",
            transactionDateUtc: "2025-01-10T00:00:00Z",
            isBankTransfer: false,
          },
        ]);
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <TransactionsPage />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText("Coffee")).toBeInTheDocument();
    });
  });

  test("updates transaction query when transfer and experimental toggles change", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);
      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          {
            id: "acc-1",
            akahuAccountId: "a1",
            name: "Everyday",
            accountNumber: "01-0000-0000000-00",
            currency: "NZD",
            transactionCount: 1,
          },
        ]);
      }
      if (url.endsWith("/api/banks")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/categories")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/contacts")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/transactions")) {
        return mockJsonResponse([]);
      }
      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <TransactionsPage />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/api/transactions"),
        expect.any(Object),
      );
    });

    fireEvent.click(screen.getByTestId("include-transfer-checkbox"));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("includeBankTransfers=false"),
        expect.any(Object),
      );
    });

    fireEvent.click(screen.getByTestId("experimental-transfer-matching-checkbox"));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("experimentalTransferMatching=true"),
        expect.any(Object),
      );
    });
  });

  test("advanced mode splits money in/out/transfers with totals and keeps transfers out of in/out columns", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          {
            id: "acc-1",
            akahuAccountId: "a1",
            name: "Everyday",
            accountNumber: "01-0000-0000000-00",
            currency: "NZD",
            transactionCount: 4,
          },
        ]);
      }

      if (url.endsWith("/api/banks")) {
        return mockJsonResponse([]);
      }

      if (url.includes("/api/categories")) {
        return mockJsonResponse([]);
      }

      if (url.includes("/api/contacts")) {
        return mockJsonResponse([]);
      }

      if (url.includes("/api/transactions")) {
        return mockJsonResponse([
          {
            id: "txn-in-1",
            akahuTransactionId: "t-in-1",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: 100,
            direction: "In",
            description: "Salary small",
            transactionDateUtc: "2025-01-10T00:00:00Z",
            isBankTransfer: false,
          },
          {
            id: "txn-in-2",
            akahuTransactionId: "t-in-2",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: 200,
            direction: 1,
            description: "Salary big",
            transactionDateUtc: "2025-01-09T00:00:00Z",
            isBankTransfer: false,
          },
          {
            id: "txn-out-1",
            akahuTransactionId: "t-out-1",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: -12,
            direction: 2,
            description: "Coffee",
            transactionDateUtc: "2025-01-08T00:00:00Z",
            isBankTransfer: false,
          },
          {
            id: "txn-transfer-out",
            akahuTransactionId: "t-transfer-out",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: -75,
            direction: 2,
            description: "Transfer out",
            transactionDateUtc: "2025-01-07T00:00:00Z",
            isBankTransfer: true,
          },
          {
            id: "txn-transfer-in",
            akahuTransactionId: "t-transfer-in",
            accountId: "acc-1",
            accountName: "Everyday",
            amount: 75,
            direction: 1,
            description: "Transfer in",
            transactionDateUtc: "2025-01-06T00:00:00Z",
            isBankTransfer: true,
          },
        ]);
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <TransactionsPage />
      </BrowserRouter>,
    );

    fireEvent.change(await screen.findByLabelText("View mode"), { target: { value: "advanced" } });

    const moneyInList = await screen.findByTestId("money-in-list");
    const moneyOutList = await screen.findByTestId("money-out-list");
    const bankTransferList = await screen.findByTestId("bank-transfer-list");

    await waitFor(() => {
      expect(moneyInList).toHaveTextContent("Salary small");
      expect(moneyInList).toHaveTextContent("Salary big");
      expect(moneyOutList).toHaveTextContent("Coffee");
      expect(bankTransferList).toHaveTextContent("Transfer out");
      expect(bankTransferList).toHaveTextContent("Transfer in");
    });

    expect(moneyInList).not.toHaveTextContent("Transfer in");
    expect(moneyOutList).not.toHaveTextContent("Transfer out");
    expect(screen.getByText("Total: $300.00")).toBeInTheDocument();
    expect(screen.getByText("Total: $12.00")).toBeInTheDocument();
    expect(screen.getByText("In: $75.00 | Out: $75.00")).toBeInTheDocument();

    const inOrderSelect = screen.getByLabelText("Money In order");
    fireEvent.change(inOrderSelect, { target: { value: "amount_desc" } });

    await waitFor(() => {
      const descriptions = Array.from(moneyInList.querySelectorAll("[data-testid='transaction-description']")).map((node) =>
        node.textContent?.trim(),
      );
      expect(descriptions[0]).toBe("Salary big");
      expect(descriptions[1]).toBe("Salary small");
    });
  });

  test("exports all transactions csv", async () => {
    const createObjectUrlSpy = vi.spyOn(URL, "createObjectURL").mockReturnValue("blob:test-url");
    const revokeObjectUrlSpy = vi.spyOn(URL, "revokeObjectURL").mockImplementation(() => {});

    const clickSpy = vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => {});

    const fetchMock = vi.fn((input: RequestInfo | URL) => {
      const url = String(input);

      if (url.endsWith("/api/accounts")) {
        return mockJsonResponse([
          {
            id: "acc-1",
            akahuAccountId: "a1",
            name: "Everyday",
            accountNumber: "01-0000-0000000-00",
            currency: "NZD",
            transactionCount: 1,
          },
        ]);
      }
      if (url.endsWith("/api/banks")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/categories")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/contacts")) {
        return mockJsonResponse([]);
      }
      if (url.includes("/api/transactions?")) {
        return mockJsonResponse([]);
      }
      if (url.endsWith("/api/transactions/export/csv")) {
        return Promise.resolve(new Response("header,row", { status: 200, headers: { "Content-Type": "text/csv" } }));
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <TransactionsPage />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/transactions?"), expect.any(Object));
    });

    fireEvent.click(screen.getByTestId("export-csv-btn"));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(expect.stringContaining("/api/transactions/export/csv"), expect.any(Object));
      expect(createObjectUrlSpy).toHaveBeenCalled();
      expect(clickSpy).toHaveBeenCalled();
      expect(revokeObjectUrlSpy).toHaveBeenCalledWith("blob:test-url");
    });

    clickSpy.mockRestore();
    createObjectUrlSpy.mockRestore();
    revokeObjectUrlSpy.mockRestore();
  });
});
