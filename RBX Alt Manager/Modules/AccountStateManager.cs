using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace RBX_Alt_Manager.Modules
{
    /// <summary>
    /// Represents the connection state of an account
    /// </summary>
    public enum AccountConnectionState
    {
        Online,
        Offline,
        Error,
        Reconnecting
    }

    /// <summary>
    /// Event arguments for account status changes
    /// </summary>
    public class AccountStatusEventArgs : EventArgs
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public AccountConnectionState State { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }

        public AccountStatusEventArgs(long userId, string username, AccountConnectionState state, string message = "")
        {
            UserId = userId;
            Username = username;
            State = state;
            Message = message;
            Timestamp = DateTime.Now;
        }
    }

    /// <summary>
    /// Centralized state manager for account connection states
    /// </summary>
    public class AccountStateManager
    {
        private readonly Dictionary<long, AccountConnectionState> _states = new Dictionary<long, AccountConnectionState>();
        private readonly object _lock = new object();

        public event EventHandler<AccountStatusEventArgs> StatusChanged;

        /// <summary>
        /// Gets the current state of an account
        /// </summary>
        public AccountConnectionState GetState(long userId)
        {
            lock (_lock)
            {
                return _states.TryGetValue(userId, out var state) ? state : AccountConnectionState.Offline;
            }
        }

        /// <summary>
        /// Updates the state of an account and triggers event if changed
        /// </summary>
        public void UpdateState(long userId, string username, AccountConnectionState newState, string message = "")
        {
            lock (_lock)
            {
                var currentState = _states.TryGetValue(userId, out var existing) ? existing : AccountConnectionState.Offline;
                
                if (currentState != newState)
                {
                    _states[userId] = newState;
                    OnStatusChanged(new AccountStatusEventArgs(userId, username, newState, message));
                }
            }
        }

        /// <summary>
        /// Sets account to online state
        /// </summary>
        public void SetOnline(long userId, string username)
        {
            UpdateState(userId, username, AccountConnectionState.Online, "Account is online");
        }

        /// <summary>
        /// Sets account to offline state
        /// </summary>
        public void SetOffline(long userId, string username)
        {
            UpdateState(userId, username, AccountConnectionState.Offline, "Account is offline");
        }

        /// <summary>
        /// Sets account to error state
        /// </summary>
        public void SetError(long userId, string username, string errorMessage)
        {
            UpdateState(userId, username, AccountConnectionState.Error, errorMessage);
        }

        /// <summary>
        /// Sets account to reconnecting state
        /// </summary>
        public void SetReconnecting(long userId, string username)
        {
            UpdateState(userId, username, AccountConnectionState.Reconnecting, "Account is reconnecting");
        }

        protected virtual void OnStatusChanged(AccountStatusEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }
    }
}
