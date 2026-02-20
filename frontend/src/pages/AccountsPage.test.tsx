import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { BrowserRouter } from "react-router-dom";
import { afterEach, describe, expect, test, vi } from "vitest";
import { AccountsPage } from "./AccountsPage";

function mockJsonResponse(body: unknown, status = 200) {
  return Promise.resolve(
    new Response(JSON.stringify(body), {
      status,
      headers: { "Content-Type": "application/json" },
    }),
  );
}

describe("AccountsPage", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  test("submits bulk account profile updates", async () => {
    const fetchMock = vi.fn((input: RequestInfo | URL, init?: RequestInit) => {
      const url = String(input);

      if (url.endsWith("/api/accounts") && (!init?.method || init.method === "GET")) {
        return mockJsonResponse([
          {
            id: "account-1",
            akahuAccountId: "a-1",
            name: "Everyday",
            accountNumber: "01-0000-0000000-00",
            currency: "NZD",
            nzBankKey: "anz",
            customDescription: "old",
            transactionCount: 12,
          },
        ]);
      }

      if (url.endsWith("/api/banks")) {
        return mockJsonResponse([{ key: "anz", name: "ANZ", logoPath: "/banks/anz.svg" }]);
      }

      if (url.endsWith("/api/accounts/profiles") && init?.method === "PUT") {
        return mockJsonResponse([]);
      }

      return mockJsonResponse({});
    });

    vi.stubGlobal("fetch", fetchMock);

    render(
      <BrowserRouter>
        <AccountsPage />
      </BrowserRouter>,
    );

    await waitFor(() => {
      expect(screen.getByText("Account Profiles")).toBeInTheDocument();
    });

    const descriptionInput = screen.getByPlaceholderText("Everyday spending");
    fireEvent.change(descriptionInput, { target: { value: "new description" } });

    fireEvent.click(screen.getByRole("button", { name: "Save all changes" }));

    await waitFor(() => {
      expect(fetchMock).toHaveBeenCalledWith(
        expect.stringContaining("/api/accounts/profiles"),
        expect.objectContaining({ method: "PUT" }),
      );
    });
  });
});
