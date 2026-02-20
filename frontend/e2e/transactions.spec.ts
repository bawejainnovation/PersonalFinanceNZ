import { expect, test } from "@playwright/test";

test("transaction feed loads and filter toggles", async ({ page }) => {
  await page.goto("/transactions");

  await expect(page.getByTestId("transactions-page")).toBeVisible();
  await expect(page.getByTestId("transaction-list")).toBeVisible();

  const checkbox = page.getByTestId("include-transfer-checkbox");
  await expect(checkbox).toBeVisible();
  await checkbox.uncheck();
  await expect(checkbox).not.toBeChecked();

  await checkbox.check();
  await expect(checkbox).toBeChecked();
});
