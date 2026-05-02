using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using RBX_Alt_Manager.Classes;

namespace RBX_Alt_Manager.Modules
{
    /// <summary>
    /// Webhook API handler for incoming external data
    /// Routes payloads to account status and inventory pipelines
    /// </summary>
    public class WebhookHandler
    {
        private readonly AccountStateManager _accountStateManager;
        private readonly InventoryManager _inventoryManager;

        public event EventHandler<WebhookPayloadEventArgs> PayloadReceived;

        public WebhookHandler(AccountStateManager accountStateManager, InventoryManager inventoryManager)
        {
            _accountStateManager = accountStateManager;
            _inventoryManager = inventoryManager;
        }

        /// <summary>
        /// Validates and routes incoming webhook payload
        /// </summary>
        public async Task<WebhookResponse> ProcessPayload(string jsonPayload, string sourceIp = null)
        {
            try
            {
                // Validate JSON
                if (!Utilities.TryParseJson(jsonPayload, out JObject data))
                {
                    return new WebhookResponse(false, "Invalid JSON payload");
                }

                // Validate required fields
                if (!data.ContainsKey("type"))
                {
                    return new WebhookResponse(false, "Missing 'type' field in payload");
                }

                string payloadType = data["type"]?.ToString()?.ToLower();

                // Route to appropriate pipeline
                return payloadType switch
                {
                    "account_status" or "accountstatus" => await ProcessAccountStatus(data),
                    "inventory" or "inventory_update" => await ProcessInventory(data),
                    _ => new WebhookResponse(false, $"Unknown payload type: {payloadType}")
                };
            }
            catch (Exception ex)
            {
                Program.Logger.Error($"Webhook processing error: {ex.Message}{ex.StackTrace}");
                return new WebhookResponse(false, $"Processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes account status payloads
        /// </summary>
        private async Task<WebhookResponse> ProcessAccountStatus(JObject data)
        {
            try
            {
                long userId = GetLongProperty(data, "userId") ?? GetLongProperty(data, "user_id") ?? 0;
                string username = GetStringProperty(data, "username") ?? "";
                string status = GetStringProperty(data, "status")?.ToLower() ?? "offline";

                if (userId == 0)
                {
                    return new WebhookResponse(false, "Missing userId in account status payload");
                }

                // Evaluate connection state
                var connectionState = EvaluateConnectionState(status);

                // Update centralized state manager
                switch (connectionState)
                {
                    case AccountConnectionState.Online:
                        _accountStateManager.SetOnline(userId, username);
                        break;
                    case AccountConnectionState.Offline:
                        _accountStateManager.SetOffline(userId, username);
                        break;
                    case AccountConnectionState.Error:
                        string errorMessage = GetStringProperty(data, "error_message") ?? "Unknown error";
                        _accountStateManager.SetError(userId, username, errorMessage);
                        break;
                    case AccountConnectionState.Reconnecting:
                        _accountStateManager.SetReconnecting(userId, username);
                        break;
                }

                // Trigger UI update on main thread
                if (AccountManager.Instance != null)
                {
                    AccountManager.Instance.InvokeIfRequired(() =>
                    {
                        var account = AccountManager.AccountsList.Find(a => a.UserID == userId);
                        if (account != null)
                        {
                            AccountManager.Instance.AccountsView.RefreshObject(account);
                        }
                    });
                }

                OnPayloadReceived(new WebhookPayloadEventArgs("account_status", data));

                return new WebhookResponse(true, $"Account status updated: {username} -> {connectionState}");
            }
            catch (Exception ex)
            {
                return new WebhookResponse(false, $"Account status processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Processes inventory payloads
        /// </summary>
        private async Task<WebhookResponse> ProcessInventory(JObject data)
        {
            try
            {
                long userId = GetLongProperty(data, "userId") ?? GetLongProperty(data, "user_id") ?? 0;

                if (userId == 0)
                {
                    return new WebhookResponse(false, "Missing userId in inventory payload");
                }

                // Parse inventory items using InventoryProcessor
                var items = InventoryProcessor.ParseInventoryData(data);

                // Update inventory manager
                _inventoryManager.UpdateInventory(userId, items);

                OnPayloadReceived(new WebhookPayloadEventArgs("inventory", data));

                return new WebhookResponse(true, $"Inventory updated for user {userId}: {items.Count} items");
            }
            catch (Exception ex)
            {
                return new WebhookResponse(false, $"Inventory processing error: {ex.Message}");
            }
        }

        /// <summary>
        /// Evaluates connection state from status string
        /// </summary>
        private AccountConnectionState EvaluateConnectionState(string status)
        {
            return status switch
            {
                "online" or "connected" or "active" => AccountConnectionState.Online,
                "offline" or "disconnected" or "inactive" => AccountConnectionState.Offline,
                "error" or "failed" or "invalid" or "expired" => AccountConnectionState.Error,
                "reconnecting" or "retrying" or "pending" => AccountConnectionState.Reconnecting,
                _ => AccountConnectionState.Offline
            };
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

        private static long? GetLongProperty(JObject obj, string propertyName)
        {
            foreach (var prop in obj.Properties())
            {
                if (prop.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                    if (long.TryParse(prop.Value?.ToString(), out long value))
                        return value;
            }
            return null;
        }

        protected virtual void OnPayloadReceived(WebhookPayloadEventArgs e)
        {
            PayloadReceived?.Invoke(this, e);
        }
    }

    /// <summary>
    /// Event arguments for webhook payload events
    /// </summary>
    public class WebhookPayloadEventArgs : EventArgs
    {
        public string PayloadType { get; set; }
        public JObject Data { get; set; }
        public DateTime Timestamp { get; set; }

        public WebhookPayloadEventArgs(string payloadType, JObject data)
        {
            PayloadType = payloadType;
            Data = data;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Response from webhook processing
    /// </summary>
    public class WebhookResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public WebhookResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}
