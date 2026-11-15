using System;
using System.Collections.Generic;

/// <summary>Inventario reutilizable por identificador; no depende de escenas ni de la interfaz.</summary>
public sealed class ProjectInventory
{
    readonly Dictionary<string, int> items = new Dictionary<string, int>(StringComparer.Ordinal);
    public event Action Changed;
    public int Quantity(string itemId) => itemId != null && items.TryGetValue(itemId, out int value) ? value : 0;
    public void Add(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId)) throw new ArgumentException("Falta identificador de proyecto.", nameof(itemId));
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        items[itemId] = checked(Quantity(itemId) + quantity);
        Changed?.Invoke();
    }
    public bool TryRemove(string itemId, int quantity = 1)
    {
        if (quantity < 1) throw new ArgumentOutOfRangeException(nameof(quantity));
        int current = Quantity(itemId);
        if (current < quantity) return false;
        if (current == quantity) items.Remove(itemId);
        else items[itemId] = current - quantity;
        Changed?.Invoke();
        return true;
    }
}
