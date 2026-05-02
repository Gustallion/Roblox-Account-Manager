using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RBX_Alt_Manager.Modules
{
    /// <summary>
    /// Represents an inventory item with its quantity and metadata
    /// </summary>
    public class InventoryItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public int Quantity { get; set; }
        public string IconUrl { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public InventoryItem() { }

        public InventoryItem(string id, string name, string category, int quantity)
        {
            Id = id;
            Name = name;
            Category = category;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Event arguments for inventory updates
    /// </summary>
    public class InventoryUpdateEventArgs : EventArgs
    {
        public long UserId { get; set; }
        public List<InventoryItem> Items { get; set; }
        public DateTime Timestamp { get; set; }

        public InventoryUpdateEventArgs(long userId, List<InventoryItem> items)
        {
            UserId = userId;
            Items = items;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Manages inventory data for real-time rendering
    /// </summary>
    public class InventoryManager
    {
        private readonly Dictionary<long, List<InventoryItem>> _inventories = new Dictionary<long, List<InventoryItem>>();
        private readonly object _lock = new object();

        public event EventHandler<InventoryUpdateEventArgs> InventoryUpdated;

        /// <summary>
        /// Gets all items for a specific user
        /// </summary>
        public List<InventoryItem> GetInventory(long userId)
        {
            lock (_lock)
            {
                return _inventories.TryGetValue(userId, out var items) ? new List<InventoryItem>(items) : new List<InventoryItem>();
            }
        }

        /// <summary>
        /// Gets items by category for a specific user
        /// </summary>
        public List<InventoryItem> GetItemsByCategory(long userId, string category)
        {
            lock (_lock)
            {
                if (!_inventories.TryGetValue(userId, out var items))
                    return new List<InventoryItem>();

                return items.FindAll(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            }
        }

        /// <summary>
        /// Updates the entire inventory for a user
        /// </summary>
        public void UpdateInventory(long userId, List<InventoryItem> items)
        {
            lock (_lock)
            {
                _inventories[userId] = items;
                OnInventoryUpdated(new InventoryUpdateEventArgs(userId, items));
            }
        }

        /// <summary>
        /// Adds or updates a single item in the inventory
        /// </summary>
        public void AddOrUpdateItem(long userId, InventoryItem item)
        {
            lock (_lock)
            {
                if (!_inventories.ContainsKey(userId))
                    _inventories[userId] = new List<InventoryItem>();

                var existingItem = _inventories[userId].Find(i => i.Id == item.Id);
                if (existingItem != null)
                {
                    existingItem.Quantity = item.Quantity;
                    existingItem.Name = item.Name;
                    existingItem.Category = item.Category;
                }
                else
                {
                    _inventories[userId].Add(item);
                }

                OnInventoryUpdated(new InventoryUpdateEventArgs(userId, _inventories[userId]));
            }
        }

        /// <summary>
        /// Removes an item from the inventory
        /// </summary>
        public void RemoveItem(long userId, string itemId)
        {
            lock (_lock)
            {
                if (_inventories.TryGetValue(userId, out var items))
                {
                    items.RemoveAll(i => i.Id == itemId);
                    OnInventoryUpdated(new InventoryUpdateEventArgs(userId, items));
                }
            }
        }

        /// <summary>
        /// Clears all inventory data
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _inventories.Clear();
            }
        }

        protected virtual void OnInventoryUpdated(InventoryUpdateEventArgs e)
        {
            InventoryUpdated?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Parses structured inventory data from webhook payloads
    /// </summary>
    public static class InventoryProcessor
    {
        /// <summary>
        /// Parses inventory data from a JObject
        /// </summary>
        public static List<InventoryItem> ParseInventoryData(JObject data)
        {
            var items = new List<InventoryItem>();

            if (data == null)
                return items;

            // Handle different inventory data formats
            if (data.ContainsKey("items"))
            {
                var itemsArray = data["items"];
                if (itemsArray is JArray array)
                {
                    foreach (var itemToken in array)
                    {
                        if (itemToken is JObject itemObj)
                        {
                            var item = ParseSingleItem(itemObj);
                            if (item != null)
                                items.Add(item);
                        }
                    }
                }
            }
            else if (data.ContainsKey("inventory"))
            {
                var inventoryObj = data["inventory"];
                if (inventoryObj is JObject invObj)
                {
                    foreach (var property in invObj.Properties())
                    {
                        var item = ParseItemFromProperty(property);
                        if (item != null)
                            items.Add(item);
                    }
                }
            }

            return items;
        }

        private static InventoryItem ParseSingleItem(JObject itemObj)
        {
            try
            {
                var id = GetStringProperty(itemObj, "id") ?? GetStringProperty(itemObj, "itemId") ?? "";
                var name = GetStringProperty(itemObj, "name") ?? GetStringProperty(itemObj, "itemName") ?? id;
                var category = GetStringProperty(itemObj, "category") ?? GetStringProperty(itemObj, "type") ?? "Uncategorized";
                var quantity = GetIntProperty(itemObj, "quantity") ?? GetIntProperty(itemObj, "count") ?? 1;
                var iconUrl = GetStringProperty(itemObj, "icon") ?? GetStringProperty(itemObj, "iconUrl") ?? "";

                var item = new InventoryItem(id, name, category, quantity)
                {
                    IconUrl = iconUrl
                };

                // Store additional metadata
                foreach (var prop in itemObj.Properties())
                {
                    if (!new[] { "id", "itemId", "name", "itemName", "category", "type", "quantity", "count", "icon", "iconUrl" }
                        .Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        item.Metadata[prop.Name] = prop.Value;
                    }
                }

                return item;
            }
            catch
            {
                return null;
            }
        }

        private static InventoryItem ParseItemFromProperty(JProperty property)
        {
            try
            {
                string id = property.Name;
                string name = property.Name;
                int quantity = 1;
                string category = "Uncategorized";

                if (property.Value is JObject valueObj)
                {
                    name = GetStringProperty(valueObj, "name") ?? name;
                    quantity = GetIntProperty(valueObj, "quantity") ?? GetIntProperty(valueObj, "count") ?? 1;
                    category = GetStringProperty(valueObj, "category") ?? category;
                }
                else if (property.Value is JValue value && int.TryParse(value.ToString(), out int qty))
                {
                    quantity = qty;
                }

                return new InventoryItem(id, name, category, quantity);
            }
            catch
            {
                return null;
            }
        }

        private static string GetStringProperty(JObject obj, string propertyName)
        {
            foreach (var prop in obj.Properties())
            {
                if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    return prop.Value?.ToString();
            }
            return null;
        }

        private static int? GetIntProperty(JObject obj, string propertyName)
        {
            foreach (var prop in obj.Properties())
            {
                if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    if (int.TryParse(prop.Value?.ToString(), out int value))
                        return value;
            }
            return null;
        }
    }
}
