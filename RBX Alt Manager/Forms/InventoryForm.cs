using System;
using System.Drawing;
using System.Windows.Forms;
using RBX_Alt_Manager.Modules;

namespace RBX_Alt_Manager.Forms
{
    /// <summary>
    /// Inventory display form - isolated from account manager for clean separation of concerns
    /// </summary>
    public class InventoryForm : Form
    {
        private readonly InventoryManager _inventoryManager;
        private readonly AccountStateManager _stateManager;
        
        private ModernTabControl _mainTabControl;
        private Panel _inventoryPanel;
        private FlowLayoutPanel _itemsPanel;
        private Label _itemCountLabel;
        private ComboBox _categoryFilter;
        private long _currentUserId;

        public InventoryForm(InventoryManager inventoryManager, AccountStateManager stateManager)
        {
            _inventoryManager = inventoryManager;
            _stateManager = stateManager;

            InitializeComponents();
            SetupStyling();
            
            // Subscribe to inventory updates
            _inventoryManager.InventoryUpdated += OnInventoryUpdated;
        }

        private void InitializeComponents()
        {
            Text = "Inventory Manager";
            Size = new Size(900, 600);
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(700, 500);

            // Main tab control for navigation
            _mainTabControl = new ModernTabControl
            {
                Dock = DockStyle.Fill,
                TabHeight = 40
            };

            // Inventory tab
            var inventoryTab = new TabPage("My Inventory");
            _inventoryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(15)
            };

            // Header panel
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 60,
                BackColor = Color.Transparent
            };

            _itemCountLabel = new Label
            {
                Text = "Items: 0",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 60, 60),
                AutoSize = true,
                Location = new Point(0, 10)
            };

            _categoryFilter = new ComboBox
            {
                Font = new Font("Segoe UI", 9f),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Size = new Size(200, 28),
                Location = new Point(0, 40)
            };
            _categoryFilter.Items.Add("All Categories");
            _categoryFilter.SelectedIndex = 0;
            _categoryFilter.SelectedIndexChanged += CategoryFilter_SelectedIndexChanged;

            headerPanel.Controls.Add(_itemCountLabel);
            headerPanel.Controls.Add(_categoryFilter);

            // Items scrollable panel
            var itemsScrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            _itemsPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(5)
            };

            itemsScrollPanel.Controls.Add(_itemsPanel);
            _inventoryPanel.Controls.Add(itemsScrollPanel);
            _inventoryPanel.Controls.Add(headerPanel);
            inventoryTab.Controls.Add(_inventoryPanel);

            _mainTabControl.TabPages.Add(inventoryTab);
            Controls.Add(_mainTabControl);
        }

        private void SetupStyling()
        {
            BackColor = Color.FromArgb(245, 245, 245);
            Font = new Font("Segoe UI", 9f);
        }

        /// <summary>
        /// Sets the current user whose inventory to display
        /// </summary>
        public void SetCurrentUser(long userId, string username)
        {
            _currentUserId = userId;
            Text = $"Inventory Manager - {username}";
            LoadInventory();
        }

        /// <summary>
        /// Loads inventory for the current user
        /// </summary>
        private void LoadInventory()
        {
            var items = _inventoryManager.GetInventory(_currentUserId);
            DisplayItems(items);
        }

        /// <summary>
        /// Displays items in the items panel
        /// </summary>
        private void DisplayItems(System.Collections.Generic.List<InventoryItem> items)
        {
            _itemsPanel.Controls.Clear();

            string selectedCategory = _categoryFilter.SelectedItem?.ToString() ?? "All Categories";
            
            var filteredItems = selectedCategory == "All Categories" 
                ? items 
                : items.FindAll(i => i.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase));

            _itemCountLabel.Text = $"Items: {filteredItems.Count}";

            foreach (var item in filteredItems)
            {
                var itemCard = CreateItemCard(item);
                _itemsPanel.Controls.Add(itemCard);
            }

            // Update category filter options
            UpdateCategoryFilter(items);
        }

        /// <summary>
        /// Creates a visual card for an inventory item
        /// </summary>
        private Control CreateItemCard(InventoryItem item)
        {
            var card = new ModernPanel
            {
                Size = new Size(180, 200),
                Margin = new Padding(8),
                CornerRadius = 10,
                GradientStart = Color.White,
                GradientEnd = Color.FromArgb(250, 250, 250)
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 3,
                ColumnCount = 1,
                Padding = new Padding(12),
                BackColor = Color.Transparent
            };

            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30f));

            // Item icon placeholder
            var iconPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 240, 240),
                Margin = new Padding(0, 0, 0, 8)
            };
            
            if (!string.IsNullOrEmpty(item.IconUrl))
            {
                // Could load image from URL here
                var iconLabel = new Label
                {
                    Text = "📦",
                    Font = new Font("Segoe UI Emoji", 32f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                iconPanel.Controls.Add(iconLabel);
            }
            else
            {
                var iconLabel = new Label
                {
                    Text = GetCategoryIcon(item.Category),
                    Font = new Font("Segoe UI Emoji", 32f),
                    TextAlign = ContentAlignment.MiddleCenter,
                    Dock = DockStyle.Fill
                };
                iconPanel.Controls.Add(iconLabel);
            }

            // Item info
            var infoPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var nameLabel = new Label
            {
                Text = item.Name,
                Font = new Font("Segoe UI", 10f, FontStyle.SemiBold),
                ForeColor = Color.FromArgb(50, 50, 50),
                AutoSize = false,
                Size = new Size(150, 40),
                Location = new Point(0, 0)
            };

            var categoryLabel = new Label
            {
                Text = item.Category,
                Font = new Font("Segoe UI", 8f),
                ForeColor = Color.FromArgb(120, 120, 120),
                AutoSize = true,
                Location = new Point(0, 38)
            };

            infoPanel.Controls.Add(nameLabel);
            infoPanel.Controls.Add(categoryLabel);

            // Quantity badge
            var quantityPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };

            var quantityBadge = new Label
            {
                Text = $"Qty: {item.Quantity}",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(0, 120, 215),
                AutoSize = true,
                Padding = new Padding(8, 4, 8, 4),
                Location = new Point(0, 5)
            };

            quantityPanel.Controls.Add(quantityBadge);

            layout.Controls.Add(iconPanel, 0, 0);
            layout.Controls.Add(infoPanel, 0, 1);
            layout.Controls.Add(quantityPanel, 0, 2);

            card.Controls.Add(layout);
            return card;
        }

        private static string GetCategoryIcon(string category)
        {
            return category.ToLower() switch
            {
                "chests" or "chest" => "🎁",
                "keys" or "key" => "🔑",
                "rerolls" or "reroll" => "🔄",
                "shards" or "shard" => "💎",
                "resources" or "resource" => "📦",
                _ => "📋"
            };
        }

        private void UpdateCategoryFilter(System.Collections.Generic.List<InventoryItem> items)
        {
            var categories = new System.Collections.Generic.HashSet<string>();
            foreach (var item in items)
            {
                categories.Add(item.Category);
            }

            var currentSelection = _categoryFilter.SelectedItem?.ToString();
            
            _categoryFilter.BeginUpdate();
            _categoryFilter.Items.Clear();
            _categoryFilter.Items.Add("All Categories");
            
            foreach (var category in categories)
            {
                _categoryFilter.Items.Add(category);
            }

            if (currentSelection != null && _categoryFilter.Items.Contains(currentSelection))
            {
                _categoryFilter.SelectedItem = currentSelection;
            }
            else
            {
                _categoryFilter.SelectedIndex = 0;
            }
            
            _categoryFilter.EndUpdate();
        }

        private void CategoryFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadInventory();
        }

        private void OnInventoryUpdated(object sender, InventoryUpdateEventArgs e)
        {
            if (e.UserId == _currentUserId)
            {
                InvokeIfRequired(() => LoadInventory());
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (InvokeRequired)
            {
                Invoke(action);
            }
            else
            {
                action();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _inventoryManager.InventoryUpdated -= OnInventoryUpdated;
            base.OnFormClosing(e);
        }
    }
}
