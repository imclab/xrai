// MockAuthProvider.cs - Mock authentication for development/testing
// Part of Spec 013: UI/UX Conferencing System
//
// Simulates authentication without requiring Firebase SDK.
// Useful for UI development and Editor testing.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace XRRAI.Auth
{
    /// <summary>
    /// Mock authentication provider for development and testing.
    /// Simulates auth operations with configurable delays and stored accounts.
    /// </summary>
    public class MockAuthProvider : IAuthProvider
    {
        public string ProviderId => "mock";
        public bool IsAvailable => true;
        public AuthUser CurrentUser { get; private set; }

        public event Action<AuthUser> OnAuthStateChanged;

        // Simulated delay for async operations
        readonly int _simulatedDelayMs;

        // In-memory account storage
        readonly Dictionary<string, MockAccount> _accounts = new();

        class MockAccount
        {
            public string Email;
            public string Password;
            public string DisplayName;
            public string UserId;
            public DateTime CreatedAt;
        }

        public MockAuthProvider(int simulatedDelayMs = 500)
        {
            _simulatedDelayMs = simulatedDelayMs;

            // Pre-populate with test accounts
            AddTestAccount("test@example.com", "password123", "Test User");
            AddTestAccount("admin@h3m.app", "admin123", "Admin");
        }

        void AddTestAccount(string email, string password, string displayName)
        {
            _accounts[email.ToLower()] = new MockAccount
            {
                Email = email,
                Password = password,
                DisplayName = displayName,
                UserId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
        }

        async Task SimulateDelay()
        {
            if (_simulatedDelayMs > 0)
                await Task.Delay(_simulatedDelayMs);
        }

        public async Task<AuthResult> SignInWithEmailAsync(string email, string password)
        {
            await SimulateDelay();

            if (string.IsNullOrEmpty(email))
                return AuthResult.Failed("Email is required", AuthErrorCode.InvalidEmail);

            if (string.IsNullOrEmpty(password))
                return AuthResult.Failed("Password is required", AuthErrorCode.InvalidPassword);

            string key = email.ToLower();
            if (!_accounts.TryGetValue(key, out var account))
                return AuthResult.Failed("User not found", AuthErrorCode.UserNotFound);

            if (account.Password != password)
                return AuthResult.Failed("Invalid password", AuthErrorCode.InvalidPassword);

            CurrentUser = new AuthUser
            {
                UserId = account.UserId,
                Email = account.Email,
                DisplayName = account.DisplayName,
                IsEmailVerified = true,
                ProviderId = ProviderId
            };

            Debug.Log($"[MockAuth] Signed in: {CurrentUser.Email}");
            OnAuthStateChanged?.Invoke(CurrentUser);
            return AuthResult.Succeeded(CurrentUser);
        }

        public async Task<AuthResult> CreateAccountWithEmailAsync(string email, string password, string displayName)
        {
            await SimulateDelay();

            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                return AuthResult.Failed("Invalid email format", AuthErrorCode.InvalidEmail);

            if (string.IsNullOrEmpty(password) || password.Length < 6)
                return AuthResult.Failed("Password must be at least 6 characters", AuthErrorCode.WeakPassword);

            string key = email.ToLower();
            if (_accounts.ContainsKey(key))
                return AuthResult.Failed("Email already in use", AuthErrorCode.EmailAlreadyInUse);

            var account = new MockAccount
            {
                Email = email,
                Password = password,
                DisplayName = displayName ?? email.Split('@')[0],
                UserId = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow
            };
            _accounts[key] = account;

            CurrentUser = new AuthUser
            {
                UserId = account.UserId,
                Email = account.Email,
                DisplayName = account.DisplayName,
                IsEmailVerified = false,
                ProviderId = ProviderId
            };

            Debug.Log($"[MockAuth] Created account: {CurrentUser.Email}");
            OnAuthStateChanged?.Invoke(CurrentUser);
            return AuthResult.Succeeded(CurrentUser);
        }

        public async Task<AuthResult> SignInWithProviderAsync(string providerId)
        {
            await SimulateDelay();

            // Simulate OAuth flow - create a user with provider-specific data
            CurrentUser = new AuthUser
            {
                UserId = Guid.NewGuid().ToString(),
                Email = $"user_{DateTime.Now.Ticks}@{providerId}.mock",
                DisplayName = $"{providerId} User",
                IsEmailVerified = true,
                ProviderId = providerId
            };

            Debug.Log($"[MockAuth] Signed in with {providerId}: {CurrentUser.DisplayName}");
            OnAuthStateChanged?.Invoke(CurrentUser);
            return AuthResult.Succeeded(CurrentUser);
        }

        public async Task SignOutAsync()
        {
            await SimulateDelay();

            Debug.Log($"[MockAuth] Signed out: {CurrentUser?.Email}");
            CurrentUser = null;
            OnAuthStateChanged?.Invoke(null);
        }

        public async Task<AuthResult> SendPasswordResetEmailAsync(string email)
        {
            await SimulateDelay();

            if (string.IsNullOrEmpty(email))
                return AuthResult.Failed("Email is required", AuthErrorCode.InvalidEmail);

            string key = email.ToLower();
            if (!_accounts.ContainsKey(key))
                return AuthResult.Failed("User not found", AuthErrorCode.UserNotFound);

            Debug.Log($"[MockAuth] Password reset email sent to: {email}");
            return AuthResult.Succeeded(null);
        }

        public async Task<AuthResult> UpdateProfileAsync(string displayName, string photoUrl)
        {
            await SimulateDelay();

            if (CurrentUser == null)
                return AuthResult.Failed("Not signed in", AuthErrorCode.Unknown);

            if (!string.IsNullOrEmpty(displayName))
                CurrentUser.DisplayName = displayName;

            if (!string.IsNullOrEmpty(photoUrl))
                CurrentUser.PhotoUrl = photoUrl;

            // Update stored account
            string key = CurrentUser.Email.ToLower();
            if (_accounts.TryGetValue(key, out var account))
            {
                account.DisplayName = CurrentUser.DisplayName;
            }

            Debug.Log($"[MockAuth] Profile updated: {CurrentUser.DisplayName}");
            OnAuthStateChanged?.Invoke(CurrentUser);
            return AuthResult.Succeeded(CurrentUser);
        }
    }
}
