import { useEffect, useMemo, useState } from "react";
import { api, type Category, type CategoryType } from "../lib/api";

export function CategoriesPage() {
  const [categories, setCategories] = useState<Category[]>([]);
  const [newTypeName, setNewTypeName] = useState("");
  const [newSpendName, setNewSpendName] = useState("");
  const [editing, setEditing] = useState<Record<string, string>>({});
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    void load();
  }, []);

  const transactionTypes = useMemo(
    () => categories.filter((category) => category.categoryType === "TransactionType"),
    [categories],
  );
  const spendTypes = useMemo(() => categories.filter((category) => category.categoryType === "SpendType"), [categories]);

  async function load() {
    setError(null);

    try {
      const response = await api.getCategories();
      setCategories(response);
    } catch (loadError) {
      setError(loadError instanceof Error ? loadError.message : "Failed to load categories");
    }
  }

  async function createCategory(categoryType: CategoryType, name: string) {
    if (!name.trim()) {
      return;
    }

    try {
      await api.createCategory({ categoryType, name: name.trim() });
      if (categoryType === "TransactionType") {
        setNewTypeName("");
      } else {
        setNewSpendName("");
      }
      await load();
    } catch (createError) {
      setError(createError instanceof Error ? createError.message : "Create failed");
    }
  }

  async function saveCategory(category: Category) {
    const updatedName = editing[category.id]?.trim();
    if (!updatedName) {
      return;
    }

    try {
      await api.updateCategory(category.id, { name: updatedName });
      await load();
    } catch (updateError) {
      setError(updateError instanceof Error ? updateError.message : "Update failed");
    }
  }

  async function deleteCategory(category: Category) {
    try {
      await api.deleteCategory(category.id);
      await load();
    } catch (deleteError) {
      setError(deleteError instanceof Error ? deleteError.message : "Delete failed");
    }
  }

  function renderCategoryList(label: string, list: Category[], type: CategoryType, newName: string, setNewName: (value: string) => void) {
    return (
      <section className="card category-card">
        <h2>{label}</h2>
        <div className="inline-form">
          <input
            placeholder={`Add ${label.toLowerCase()} category`}
            value={newName}
            onChange={(event) => setNewName(event.target.value)}
          />
          <button className="primary-btn" onClick={() => createCategory(type, newName)}>
            Add
          </button>
        </div>

        <div className="list-stack">
          {list.map((category) => (
            <div key={category.id} className="category-row">
              <input
                value={editing[category.id] ?? category.name}
                onChange={(event) => setEditing((current) => ({ ...current, [category.id]: event.target.value }))}
              />
              <button className="subtle-btn" onClick={() => saveCategory(category)}>
                Save
              </button>
              <button className="danger-btn" onClick={() => deleteCategory(category)}>
                Delete
              </button>
            </div>
          ))}

          {list.length === 0 ? <p className="soft-copy">No categories yet.</p> : null}
        </div>
      </section>
    );
  }

  return (
    <div className="page-space">
      {error ? <p className="error-card">{error}</p> : null}
      <div className="two-column-grid">
        {renderCategoryList("Transaction Type", transactionTypes, "TransactionType", newTypeName, setNewTypeName)}
        {renderCategoryList("Type of spend", spendTypes, "SpendType", newSpendName, setNewSpendName)}
      </div>
    </div>
  );
}
