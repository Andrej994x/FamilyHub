import { useCallback, useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useFamily } from '../hooks/useFamily';
import { shoppingService } from '../services/shoppingService';
import { getApiErrorMessage } from '../utils/apiError';
import { itemCategoryKey } from '../utils/labels';
import { ShoppingListDialog } from '../components/ShoppingListDialog';
import { ItemCategory } from '../types';
import type { ShoppingItemResponse, ShoppingListResponse } from '../types';

const CATEGORY_VALUES = [
  ItemCategory.Grocery,
  ItemCategory.Pharmacy,
  ItemCategory.Home,
  ItemCategory.Child,
  ItemCategory.Other,
];

export default function Shopping() {
  const { t } = useTranslation();
  const { family } = useFamily();
  const familyId = family?.id ?? '';

  const [lists, setLists] = useState<ShoppingListResponse[]>([]);
  const [selectedListId, setSelectedListId] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [listDialogOpen, setListDialogOpen] = useState(false);

  // Quick-add form.
  const [itemName, setItemName] = useState('');
  const [itemCategory, setItemCategory] = useState<number>(ItemCategory.Grocery);
  const [adding, setAdding] = useState(false);

  const load = useCallback(async () => {
    if (!familyId) {
      return;
    }
    setLoading(true);
    setError(null);
    try {
      const data = await shoppingService.getLists(familyId);
      setLists(data);
      setSelectedListId((prev) =>
        prev && data.some((l) => l.id === prev) ? prev : (data[0]?.id ?? null),
      );
    } catch (err) {
      setError(getApiErrorMessage(err, t('shopping.errors.loadFailed')));
    } finally {
      setLoading(false);
    }
  }, [familyId, t]);

  useEffect(() => {
    void load();
  }, [load]);

  const selectedList = useMemo(
    () => lists.find((l) => l.id === selectedListId) ?? null,
    [lists, selectedListId],
  );

  // Replace the item array of one list in place — keeps the UI snappy without a full reload.
  const updateItems = useCallback(
    (listId: string, updater: (items: ShoppingItemResponse[]) => ShoppingItemResponse[]) => {
      setLists((prev) =>
        prev.map((l) => (l.id === listId ? { ...l, items: updater(l.items) } : l)),
      );
    },
    [],
  );

  const { groups, purchased } = useMemo(() => {
    const items = selectedList?.items ?? [];
    const active = items.filter((i) => !i.isPurchased);
    const done = items.filter((i) => i.isPurchased);

    const map = new Map<number, ShoppingItemResponse[]>();
    for (const item of active) {
      const arr = map.get(item.category) ?? [];
      arr.push(item);
      map.set(item.category, arr);
    }
    const groups = [...map.entries()]
      .sort((a, b) => a[0] - b[0])
      .map(([category, catItems]) => ({
        category,
        items: catItems.sort((a, b) => a.createdAt.localeCompare(b.createdAt)),
      }));

    return { groups, purchased: done };
  }, [selectedList]);

  const handleAddItem = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!itemName.trim() || !selectedListId) {
      return;
    }
    setAdding(true);
    setError(null);
    try {
      const item = await shoppingService.addItem(selectedListId, {
        name: itemName.trim(),
        quantity: null,
        category: itemCategory,
      });
      updateItems(selectedListId, (items) => [...items, item]);
      setItemName('');
    } catch (err) {
      setError(getApiErrorMessage(err, t('shopping.errors.addItemFailed')));
    } finally {
      setAdding(false);
    }
  };

  const handleToggle = async (item: ShoppingItemResponse) => {
    const listId = item.shoppingListId;
    // Optimistic flip for instant feedback.
    updateItems(listId, (items) =>
      items.map((i) => (i.id === item.id ? { ...i, isPurchased: !i.isPurchased } : i)),
    );
    try {
      const updated = await shoppingService.toggleItem(listId, item.id);
      updateItems(listId, (items) => items.map((i) => (i.id === item.id ? updated : i)));
    } catch (err) {
      // Revert on failure.
      updateItems(listId, (items) =>
        items.map((i) => (i.id === item.id ? { ...i, isPurchased: item.isPurchased } : i)),
      );
      setError(getApiErrorMessage(err, t('shopping.errors.updateItemFailed')));
    }
  };

  const handleDelete = async (item: ShoppingItemResponse) => {
    try {
      await shoppingService.deleteItem(item.shoppingListId, item.id);
      updateItems(item.shoppingListId, (items) => items.filter((i) => i.id !== item.id));
    } catch (err) {
      setError(getApiErrorMessage(err, t('shopping.errors.deleteItemFailed')));
    }
  };

  const handleClearPurchased = async () => {
    if (!selectedListId || !window.confirm(t('shopping.clearPurchasedConfirm'))) {
      return;
    }
    try {
      await shoppingService.clearPurchased(selectedListId);
      updateItems(selectedListId, (items) => items.filter((i) => !i.isPurchased));
    } catch (err) {
      setError(getApiErrorMessage(err, t('shopping.errors.clearPurchasedFailed')));
    }
  };

  const handleListCreated = (list: ShoppingListResponse) => {
    setLists((prev) => [...prev, list]);
    setSelectedListId(list.id);
  };

  const activeCount = (list: ShoppingListResponse) =>
    list.items.filter((i) => !i.isPurchased).length;

  if (!family) {
    return null;
  }

  return (
    <div className="mx-auto max-w-2xl">
      <header className="mb-4 flex items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('nav.shopping')}</h1>
          <p className="mt-1 text-sm text-gray-500">{t('pages.shopping.subtitle')}</p>
        </div>
        <button
          type="button"
          onClick={() => setListDialogOpen(true)}
          className="shrink-0 rounded-lg bg-brand-600 px-3 py-2 text-sm font-medium text-white hover:bg-brand-700"
        >
          {t('shopping.newList')}
        </button>
      </header>

      {error && (
        <div className="mb-4 rounded-lg bg-red-50 px-3 py-2 text-sm text-red-700">{error}</div>
      )}

      {loading ? (
        <p className="text-sm text-gray-400">{t('common.loading')}</p>
      ) : lists.length === 0 ? (
        <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center">
          <p className="text-sm text-gray-400">{t('shopping.noLists')}</p>
          <button
            type="button"
            onClick={() => setListDialogOpen(true)}
            className="mt-4 rounded-lg bg-brand-600 px-4 py-2 text-sm font-medium text-white hover:bg-brand-700"
          >
            {t('shopping.createFirstList')}
          </button>
        </div>
      ) : (
        <>
          {/* List selector */}
          <div className="no-scrollbar mb-4 flex gap-2 overflow-x-auto pb-1">
            {lists.map((list) => {
              const active = list.id === selectedListId;
              const count = activeCount(list);
              return (
                <button
                  key={list.id}
                  type="button"
                  onClick={() => setSelectedListId(list.id)}
                  className={`flex shrink-0 items-center gap-2 whitespace-nowrap rounded-full px-4 py-2 text-sm font-medium transition ${
                    active
                      ? 'bg-brand-600 text-white'
                      : 'bg-white text-gray-700 ring-1 ring-gray-200 hover:bg-gray-50'
                  }`}
                >
                  {list.name}
                  {count > 0 && (
                    <span
                      className={`rounded-full px-1.5 text-xs ${
                        active ? 'bg-white/25' : 'bg-gray-100 text-gray-500'
                      }`}
                    >
                      {count}
                    </span>
                  )}
                </button>
              );
            })}
          </div>

          {/* Items */}
          {selectedList && (
            <div className="space-y-5">
              {groups.length === 0 && purchased.length === 0 ? (
                <div className="rounded-xl border-2 border-dashed border-gray-200 bg-white p-10 text-center text-sm text-gray-400">
                  {t('shopping.emptyList')}
                </div>
              ) : (
                <>
                  {/* Active items, grouped by category */}
                  {groups.map((group) => (
                    <section key={group.category}>
                      <h2 className="mb-1.5 px-1 text-xs font-semibold uppercase tracking-wide text-gray-400">
                        {t(itemCategoryKey(group.category))}
                      </h2>
                      <ul className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
                        {group.items.map((item) => (
                          <ItemRow
                            key={item.id}
                            item={item}
                            onToggle={handleToggle}
                            onDelete={handleDelete}
                            toggleLabel={t('shopping.markPurchased')}
                            deleteLabel={t('common.delete')}
                          />
                        ))}
                      </ul>
                    </section>
                  ))}

                  {/* Purchased items, shown separately */}
                  {purchased.length > 0 && (
                    <section>
                      <div className="mb-1.5 flex items-center justify-between px-1">
                        <h2 className="text-xs font-semibold uppercase tracking-wide text-gray-400">
                          {t('shopping.purchased', { count: purchased.length })}
                        </h2>
                        <button
                          type="button"
                          onClick={handleClearPurchased}
                          className="text-xs font-medium text-red-600 hover:underline"
                        >
                          {t('shopping.clearPurchased')}
                        </button>
                      </div>
                      <ul className="divide-y divide-gray-100 overflow-hidden rounded-xl border border-gray-200 bg-white">
                        {purchased.map((item) => (
                          <ItemRow
                            key={item.id}
                            item={item}
                            onToggle={handleToggle}
                            onDelete={handleDelete}
                            toggleLabel={t('shopping.markNotPurchased')}
                            deleteLabel={t('common.delete')}
                          />
                        ))}
                      </ul>
                    </section>
                  )}
                </>
              )}

              {/* Quick-add bar — pinned within thumb reach, above the mobile nav. */}
              <form
                onSubmit={handleAddItem}
                className="sticky bottom-[calc(5rem+env(safe-area-inset-bottom))] z-10 flex gap-2 rounded-xl border border-gray-200 bg-white p-2 shadow-lg md:bottom-4"
              >
                <input
                  type="text"
                  value={itemName}
                  onChange={(e) => setItemName(e.target.value)}
                  placeholder={t('shopping.addItemPlaceholder')}
                  aria-label={t('shopping.addItemPlaceholder')}
                  className="min-w-0 flex-1 rounded-lg border border-gray-300 px-3 py-2.5 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
                />
                <select
                  value={itemCategory}
                  onChange={(e) => setItemCategory(Number(e.target.value))}
                  aria-label={t('shopping.dialog.category')}
                  className="rounded-lg border border-gray-300 bg-white px-2 py-2.5 text-sm outline-none focus:border-brand-500 focus:ring-2 focus:ring-brand-100"
                >
                  {CATEGORY_VALUES.map((value) => (
                    <option key={value} value={value}>
                      {t(itemCategoryKey(value))}
                    </option>
                  ))}
                </select>
                <button
                  type="submit"
                  disabled={adding || !itemName.trim()}
                  aria-label={t('shopping.addItem')}
                  className="shrink-0 rounded-lg bg-brand-600 px-4 py-2.5 text-sm font-semibold text-white hover:bg-brand-700 disabled:opacity-50"
                >
                  <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2} stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" d="M12 4.5v15m7.5-7.5h-15" />
                  </svg>
                </button>
              </form>
            </div>
          )}
        </>
      )}

      <ShoppingListDialog
        open={listDialogOpen}
        familyId={familyId}
        onClose={() => setListDialogOpen(false)}
        onCreated={handleListCreated}
      />
    </div>
  );
}

interface ItemRowProps {
  item: ShoppingItemResponse;
  onToggle: (item: ShoppingItemResponse) => void;
  onDelete: (item: ShoppingItemResponse) => void;
  toggleLabel: string;
  deleteLabel: string;
}

function ItemRow({ item, onToggle, onDelete, toggleLabel, deleteLabel }: ItemRowProps) {
  return (
    <li className="flex items-center gap-3 p-3">
      <button
        type="button"
        onClick={() => onToggle(item)}
        aria-label={toggleLabel}
        aria-pressed={item.isPurchased}
        className={`flex h-9 w-9 shrink-0 items-center justify-center rounded-full border-2 transition ${
          item.isPurchased
            ? 'border-brand-600 bg-brand-600 text-white'
            : 'border-gray-300 text-transparent hover:border-brand-400'
        }`}
      >
        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={2.5} stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" d="M4.5 12.75l6 6 9-13.5" />
        </svg>
      </button>

      <div className="min-w-0 flex-1">
        <p
          className={`truncate text-sm ${
            item.isPurchased ? 'text-gray-400 line-through' : 'font-medium text-gray-900'
          }`}
        >
          {item.name}
        </p>
        {item.quantity && <p className="truncate text-xs text-gray-400">{item.quantity}</p>}
      </div>

      <button
        type="button"
        onClick={() => onDelete(item)}
        aria-label={deleteLabel}
        className="shrink-0 rounded-lg p-2 text-gray-300 hover:bg-gray-50 hover:text-red-600"
      >
        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" strokeWidth={1.7} stroke="currentColor">
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            d="M14.74 9l-.346 9m-4.788 0L9.26 9m9.968-3.21c.342.052.682.107 1.022.166m-1.022-.165L18.16 19.673a2.25 2.25 0 01-2.244 2.077H8.084a2.25 2.25 0 01-2.244-2.077L4.772 5.79m14.456 0a48.108 48.108 0 00-3.478-.397m-12 .562c.34-.059.68-.114 1.022-.165m0 0a48.11 48.11 0 013.478-.397m7.5 0v-.916c0-1.18-.91-2.164-2.09-2.201a51.964 51.964 0 00-3.32 0c-1.18.037-2.09 1.022-2.09 2.201v.916m7.5 0a48.667 48.667 0 00-7.5 0" />
        </svg>
      </button>
    </li>
  );
}
