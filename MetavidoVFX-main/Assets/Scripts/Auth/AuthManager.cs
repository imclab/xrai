// AuthManager.cs - Singleton for app-wide authentication state
// Part of Spec 013: UI/UX Conferencing System
//
// Provides global access to auth state across scenes.
// Initializes the appropriate auth provider based on platform/configuration.

using System;
using UnityEngine;

namespace XRRAI.Auth
{
    /// <summary>
    /// Singleton that manages authentication state across the application.
    /// Access via AuthManager.Instance from anywhere.
    /// </summary>
    public class AuthManager : MonoBehaviour
    {
        public static AuthManager Instance { get; private set; }

        [Header("Configuration")]
        [Tooltip("Use mock auth (for development). Set to false for production with Firebase.")]
        [SerializeField] bool _useMockAuth = true;

        [Tooltip("Persist auth state across scene loads")]
        [SerializeField] bool _dontDestroyOnLoad = true;

        // Auth provider
        IAuthProvider _authProvider;

        // Events
        public event Action<AuthUser> OnSignIn;
        public event Action OnSignOut;

        // Public properties
        public IAuthProvider AuthProvider => _authProvider;
        public AuthUser CurrentUser => _authProvider?.CurrentUser;
        public bool IsSignedIn => CurrentUser != null;
        public string UserId => CurrentUser?.UserId;
        public string UserEmail => CurrentUser?.Email;
        public string UserDisplayName => CurrentUser?.DisplayName;

        void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Debug.Log("[AuthManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (_dontDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            InitializeAuthProvider();
        }

        void InitializeAuthProvider()
        {
            if (_useMockAuth)
            {
                _authProvider = new MockAuthProvider();
                Debug.Log("[AuthManager] Initialized with MockAuthProvider");
            }
            else
            {
                // TODO: Initialize Firebase when SDK is integrated
                // _authProvider = new FirebaseAuthProvider();
                _authProvider = new MockAuthProvider();
                Debug.LogWarning("[AuthManager] Firebase not available, using MockAuthProvider");
            }

            _authProvider.OnAuthStateChanged += HandleAuthStateChanged;
        }

        void HandleAuthStateChanged(AuthUser user)
        {
            if (user != null)
            {
                Debug.Log($"[AuthManager] User signed in: {user.Email}");
                OnSignIn?.Invoke(user);
            }
            else
            {
                Debug.Log("[AuthManager] User signed out");
                OnSignOut?.Invoke();
            }
        }

        void OnDestroy()
        {
            if (_authProvider != null)
                _authProvider.OnAuthStateChanged -= HandleAuthStateChanged;

            if (Instance == this)
                Instance = null;
        }

        #region Public API

        /// <summary>
        /// Sign in with email and password
        /// </summary>
        public async System.Threading.Tasks.Task<AuthResult> SignInAsync(string email, string password)
        {
            if (_authProvider == null)
                return AuthResult.Failed("Auth provider not initialized");

            return await _authProvider.SignInWithEmailAsync(email, password);
        }

        /// <summary>
        /// Create a new account
        /// </summary>
        public async System.Threading.Tasks.Task<AuthResult> CreateAccountAsync(string email, string password, string displayName)
        {
            if (_authProvider == null)
                return AuthResult.Failed("Auth provider not initialized");

            return await _authProvider.CreateAccountWithEmailAsync(email, password, displayName);
        }

        /// <summary>
        /// Sign in with a third-party provider
        /// </summary>
        public async System.Threading.Tasks.Task<AuthResult> SignInWithProviderAsync(string providerId)
        {
            if (_authProvider == null)
                return AuthResult.Failed("Auth provider not initialized");

            return await _authProvider.SignInWithProviderAsync(providerId);
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        public async System.Threading.Tasks.Task SignOutAsync()
        {
            if (_authProvider != null)
                await _authProvider.SignOutAsync();
        }

        /// <summary>
        /// Require authentication - returns current user or null if not signed in.
        /// Use in scenes that require auth.
        /// </summary>
        public AuthUser RequireAuth(Action onNotAuthenticated = null)
        {
            if (!IsSignedIn)
            {
                Debug.LogWarning("[AuthManager] User not authenticated");
                onNotAuthenticated?.Invoke();
                return null;
            }
            return CurrentUser;
        }

        #endregion

        #region Editor Helpers

        [ContextMenu("Debug Auth State")]
        void DebugAuthState()
        {
            Debug.Log("=== AuthManager State ===");
            Debug.Log($"IsSignedIn: {IsSignedIn}");
            Debug.Log($"Provider: {_authProvider?.ProviderId ?? "null"}");
            if (CurrentUser != null)
            {
                Debug.Log($"UserId: {CurrentUser.UserId}");
                Debug.Log($"Email: {CurrentUser.Email}");
                Debug.Log($"DisplayName: {CurrentUser.DisplayName}");
                Debug.Log($"IsEmailVerified: {CurrentUser.IsEmailVerified}");
            }
        }

        [ContextMenu("Sign Out (Debug)")]
        void DebugSignOut()
        {
            _ = SignOutAsync();
        }

        #endregion
    }
}
