// IAuthProvider.cs - Authentication provider interface
// Part of Spec 013: UI/UX Conferencing System
//
// Defines the contract for authentication providers (Firebase, Apple, Google, etc.)
// Implementations can be swapped or mocked for testing.

using System;
using System.Threading.Tasks;

namespace MetavidoVFX.Auth
{
    /// <summary>
    /// Represents a signed-in user
    /// </summary>
    public class AuthUser
    {
        public string UserId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string PhotoUrl { get; set; }
        public bool IsEmailVerified { get; set; }
        public string ProviderId { get; set; }

        public bool IsAnonymous => string.IsNullOrEmpty(Email);
    }

    /// <summary>
    /// Result of an authentication operation
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public AuthUser User { get; set; }
        public string ErrorMessage { get; set; }
        public AuthErrorCode ErrorCode { get; set; }

        public static AuthResult Succeeded(AuthUser user) => new AuthResult
        {
            Success = true,
            User = user
        };

        public static AuthResult Failed(string message, AuthErrorCode code = AuthErrorCode.Unknown) => new AuthResult
        {
            Success = false,
            ErrorMessage = message,
            ErrorCode = code
        };
    }

    /// <summary>
    /// Common authentication error codes
    /// </summary>
    public enum AuthErrorCode
    {
        Unknown,
        InvalidEmail,
        InvalidPassword,
        UserNotFound,
        EmailAlreadyInUse,
        WeakPassword,
        NetworkError,
        Cancelled,
        ExpiredToken,
        TooManyRequests,
        ProviderNotAvailable
    }

    /// <summary>
    /// Interface for authentication providers
    /// </summary>
    public interface IAuthProvider
    {
        /// <summary>
        /// Provider identifier (e.g., "firebase", "apple", "google")
        /// </summary>
        string ProviderId { get; }

        /// <summary>
        /// Whether this provider is available on current platform
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Currently signed-in user (null if not signed in)
        /// </summary>
        AuthUser CurrentUser { get; }

        /// <summary>
        /// Event fired when auth state changes
        /// </summary>
        event Action<AuthUser> OnAuthStateChanged;

        /// <summary>
        /// Sign in with email and password
        /// </summary>
        Task<AuthResult> SignInWithEmailAsync(string email, string password);

        /// <summary>
        /// Create a new account with email and password
        /// </summary>
        Task<AuthResult> CreateAccountWithEmailAsync(string email, string password, string displayName);

        /// <summary>
        /// Sign in with a third-party provider (Google, Apple, etc.)
        /// </summary>
        Task<AuthResult> SignInWithProviderAsync(string providerId);

        /// <summary>
        /// Sign out the current user
        /// </summary>
        Task SignOutAsync();

        /// <summary>
        /// Send password reset email
        /// </summary>
        Task<AuthResult> SendPasswordResetEmailAsync(string email);

        /// <summary>
        /// Update user profile
        /// </summary>
        Task<AuthResult> UpdateProfileAsync(string displayName, string photoUrl);
    }
}
