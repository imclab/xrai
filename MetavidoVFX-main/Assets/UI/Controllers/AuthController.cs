// AuthController.cs - Controller for AuthView.uxml
// Part of Spec 013: UI/UX Conferencing System
//
// Handles sign-in, sign-up, and social auth UI interactions.
// Uses IAuthProvider abstraction for easy provider swapping.

using System;
using UnityEngine;
using UnityEngine.UIElements;
using XRRAI.Auth;

namespace XRRAI.UI
{
    /// <summary>
    /// Controls the AuthView UI for login/signup flows.
    /// Attach to a GameObject with UIDocument referencing AuthView.uxml.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class AuthController : MonoBehaviour
    {
        [Header("Auth Configuration")]
        [Tooltip("Use mock auth for development/testing")]
        [SerializeField] bool _useMockAuth = true;

        [Tooltip("Scene to load after successful auth")]
        [SerializeField] string _nextSceneName = "Lobby";

        // Events (subscribe to these for auth state notifications)
        public event Action<AuthUser> OnAuthSuccess;
        public event Action<string> OnAuthError;

        // UI Elements
        UIDocument _document;
        VisualElement _root;

        // Tab buttons
        Button _tabSignIn;
        Button _tabSignUp;

        // Forms
        VisualElement _signInForm;
        VisualElement _signUpForm;

        // Sign In elements
        TextField _signInEmail;
        TextField _signInPassword;
        Button _btnSignIn;
        Button _btnForgotPassword;
        Label _signInError;

        // Sign Up elements
        TextField _signUpName;
        TextField _signUpEmail;
        TextField _signUpPassword;
        TextField _signUpConfirm;
        Button _btnSignUp;
        Label _signUpError;

        // Social buttons
        Button _btnGoogle;
        Button _btnApple;

        // Loading overlay
        VisualElement _loadingOverlay;

        // Auth provider
        IAuthProvider _authProvider;

        void Awake()
        {
            _document = GetComponent<UIDocument>();

            // Initialize auth provider
            if (_useMockAuth)
            {
                _authProvider = new MockAuthProvider();
                Debug.Log("[AuthController] Using MockAuthProvider");
            }
            else
            {
                // TODO: Initialize Firebase auth provider when available
                _authProvider = new MockAuthProvider();
                Debug.LogWarning("[AuthController] Firebase not configured, falling back to MockAuthProvider");
            }

            _authProvider.OnAuthStateChanged += HandleAuthStateChanged;
        }

        void OnEnable()
        {
            // Get root and cache all elements
            _root = _document.rootVisualElement;

            CacheUIElements();
            BindEvents();

            // Start on sign-in tab
            ShowSignInForm();

            // Hide loading initially
            SetLoading(false);
        }

        void OnDisable()
        {
            UnbindEvents();
        }

        void OnDestroy()
        {
            if (_authProvider != null)
                _authProvider.OnAuthStateChanged -= HandleAuthStateChanged;
        }

        void CacheUIElements()
        {
            // Tabs
            _tabSignIn = _root.Q<Button>("tab-signin");
            _tabSignUp = _root.Q<Button>("tab-signup");

            // Forms
            _signInForm = _root.Q<VisualElement>("signin-form");
            _signUpForm = _root.Q<VisualElement>("signup-form");

            // Sign In
            _signInEmail = _root.Q<TextField>("signin-email");
            _signInPassword = _root.Q<TextField>("signin-password");
            _btnSignIn = _root.Q<Button>("btn-signin");
            _btnForgotPassword = _root.Q<Button>("forgot-password");
            _signInError = _root.Q<Label>("signin-error");

            // Sign Up
            _signUpName = _root.Q<TextField>("signup-name");
            _signUpEmail = _root.Q<TextField>("signup-email");
            _signUpPassword = _root.Q<TextField>("signup-password");
            _signUpConfirm = _root.Q<TextField>("signup-confirm");
            _btnSignUp = _root.Q<Button>("btn-signup");
            _signUpError = _root.Q<Label>("signup-error");

            // Social
            _btnGoogle = _root.Q<Button>("btn-google");
            _btnApple = _root.Q<Button>("btn-apple");

            // Loading
            _loadingOverlay = _root.Q<VisualElement>("loading-overlay");
        }

        void BindEvents()
        {
            // Tabs
            _tabSignIn?.RegisterCallback<ClickEvent>(_ => ShowSignInForm());
            _tabSignUp?.RegisterCallback<ClickEvent>(_ => ShowSignUpForm());

            // Sign In
            _btnSignIn?.RegisterCallback<ClickEvent>(_ => OnSignInClicked());
            _btnForgotPassword?.RegisterCallback<ClickEvent>(_ => OnForgotPasswordClicked());

            // Sign Up
            _btnSignUp?.RegisterCallback<ClickEvent>(_ => OnSignUpClicked());

            // Social
            _btnGoogle?.RegisterCallback<ClickEvent>(_ => OnGoogleClicked());
            _btnApple?.RegisterCallback<ClickEvent>(_ => OnAppleClicked());

            // Enter key submits forms
            _signInPassword?.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    OnSignInClicked();
            });
            _signUpConfirm?.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                    OnSignUpClicked();
            });
        }

        void UnbindEvents()
        {
            // UI Toolkit handles cleanup on disable
        }

        #region Tab Switching

        void ShowSignInForm()
        {
            _signInForm.style.display = DisplayStyle.Flex;
            _signUpForm.style.display = DisplayStyle.None;

            // Update tab styling
            _tabSignIn?.AddToClassList("btn-primary");
            _tabSignIn?.RemoveFromClassList("btn-secondary");
            _tabSignUp?.AddToClassList("btn-secondary");
            _tabSignUp?.RemoveFromClassList("btn-primary");

            ClearErrors();
        }

        void ShowSignUpForm()
        {
            _signInForm.style.display = DisplayStyle.None;
            _signUpForm.style.display = DisplayStyle.Flex;

            // Update tab styling
            _tabSignUp?.AddToClassList("btn-primary");
            _tabSignUp?.RemoveFromClassList("btn-secondary");
            _tabSignIn?.AddToClassList("btn-secondary");
            _tabSignIn?.RemoveFromClassList("btn-primary");

            ClearErrors();
        }

        #endregion

        #region Sign In

        async void OnSignInClicked()
        {
            string email = _signInEmail?.value?.Trim();
            string password = _signInPassword?.value;

            // Validation
            if (string.IsNullOrEmpty(email))
            {
                ShowSignInError("Please enter your email");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowSignInError("Please enter your password");
                return;
            }

            SetLoading(true);
            ClearErrors();

            try
            {
                var result = await _authProvider.SignInWithEmailAsync(email, password);

                if (result.Success)
                {
                    Debug.Log($"[AuthController] Sign in successful: {result.User.Email}");
                    OnAuthSuccess?.Invoke(result.User);
                    NavigateToNextScene();
                }
                else
                {
                    ShowSignInError(GetUserFriendlyError(result.ErrorCode, result.ErrorMessage));
                    OnAuthError?.Invoke(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ShowSignInError("An unexpected error occurred");
                Debug.LogError($"[AuthController] Sign in error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        async void OnForgotPasswordClicked()
        {
            string email = _signInEmail?.value?.Trim();

            if (string.IsNullOrEmpty(email))
            {
                ShowSignInError("Please enter your email first");
                return;
            }

            SetLoading(true);
            ClearErrors();

            try
            {
                var result = await _authProvider.SendPasswordResetEmailAsync(email);

                if (result.Success)
                {
                    ShowSignInError("Password reset email sent! Check your inbox.");
                    // Change error label style to success
                    _signInError?.AddToClassList("text-success");
                }
                else
                {
                    ShowSignInError(GetUserFriendlyError(result.ErrorCode, result.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                ShowSignInError("Failed to send reset email");
                Debug.LogError($"[AuthController] Password reset error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        #endregion

        #region Sign Up

        async void OnSignUpClicked()
        {
            string name = _signUpName?.value?.Trim();
            string email = _signUpEmail?.value?.Trim();
            string password = _signUpPassword?.value;
            string confirm = _signUpConfirm?.value;

            // Validation
            if (string.IsNullOrEmpty(name))
            {
                ShowSignUpError("Please enter your name");
                return;
            }
            if (string.IsNullOrEmpty(email))
            {
                ShowSignUpError("Please enter your email");
                return;
            }
            if (!IsValidEmail(email))
            {
                ShowSignUpError("Please enter a valid email");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                ShowSignUpError("Please enter a password");
                return;
            }
            if (password.Length < 6)
            {
                ShowSignUpError("Password must be at least 6 characters");
                return;
            }
            if (password != confirm)
            {
                ShowSignUpError("Passwords do not match");
                return;
            }

            SetLoading(true);
            ClearErrors();

            try
            {
                var result = await _authProvider.CreateAccountWithEmailAsync(email, password, name);

                if (result.Success)
                {
                    Debug.Log($"[AuthController] Account created: {result.User.Email}");
                    OnAuthSuccess?.Invoke(result.User);
                    NavigateToNextScene();
                }
                else
                {
                    ShowSignUpError(GetUserFriendlyError(result.ErrorCode, result.ErrorMessage));
                    OnAuthError?.Invoke(result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                ShowSignUpError("An unexpected error occurred");
                Debug.LogError($"[AuthController] Sign up error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        #endregion

        #region Social Auth

        async void OnGoogleClicked()
        {
            SetLoading(true);
            ClearErrors();

            try
            {
                var result = await _authProvider.SignInWithProviderAsync("google");

                if (result.Success)
                {
                    Debug.Log($"[AuthController] Google sign in successful: {result.User.DisplayName}");
                    OnAuthSuccess?.Invoke(result.User);
                    NavigateToNextScene();
                }
                else
                {
                    ShowSignInError(GetUserFriendlyError(result.ErrorCode, result.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                ShowSignInError("Google sign in failed");
                Debug.LogError($"[AuthController] Google auth error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        async void OnAppleClicked()
        {
            SetLoading(true);
            ClearErrors();

            try
            {
                var result = await _authProvider.SignInWithProviderAsync("apple");

                if (result.Success)
                {
                    Debug.Log($"[AuthController] Apple sign in successful: {result.User.DisplayName}");
                    OnAuthSuccess?.Invoke(result.User);
                    NavigateToNextScene();
                }
                else
                {
                    ShowSignInError(GetUserFriendlyError(result.ErrorCode, result.ErrorMessage));
                }
            }
            catch (Exception ex)
            {
                ShowSignInError("Apple sign in failed");
                Debug.LogError($"[AuthController] Apple auth error: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        #endregion

        #region Helpers

        void ShowSignInError(string message)
        {
            if (_signInError == null) return;
            _signInError.text = message;
            _signInError.style.display = DisplayStyle.Flex;
            _signInError.RemoveFromClassList("text-success");
        }

        void ShowSignUpError(string message)
        {
            if (_signUpError == null) return;
            _signUpError.text = message;
            _signUpError.style.display = DisplayStyle.Flex;
        }

        void ClearErrors()
        {
            if (_signInError != null)
            {
                _signInError.style.display = DisplayStyle.None;
                _signInError.RemoveFromClassList("text-success");
            }
            if (_signUpError != null)
                _signUpError.style.display = DisplayStyle.None;
        }

        void SetLoading(bool loading)
        {
            if (_loadingOverlay != null)
                _loadingOverlay.style.display = loading ? DisplayStyle.Flex : DisplayStyle.None;

            // Disable buttons while loading
            SetButtonsEnabled(!loading);
        }

        void SetButtonsEnabled(bool enabled)
        {
            _btnSignIn?.SetEnabled(enabled);
            _btnSignUp?.SetEnabled(enabled);
            _btnGoogle?.SetEnabled(enabled);
            _btnApple?.SetEnabled(enabled);
        }

        bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            int atIndex = email.IndexOf('@');
            int dotIndex = email.LastIndexOf('.');
            return atIndex > 0 && dotIndex > atIndex + 1 && dotIndex < email.Length - 1;
        }

        string GetUserFriendlyError(AuthErrorCode code, string fallback)
        {
            return code switch
            {
                AuthErrorCode.InvalidEmail => "Please enter a valid email address",
                AuthErrorCode.InvalidPassword => "Incorrect password",
                AuthErrorCode.UserNotFound => "No account found with this email",
                AuthErrorCode.EmailAlreadyInUse => "An account already exists with this email",
                AuthErrorCode.WeakPassword => "Password is too weak. Use at least 6 characters",
                AuthErrorCode.NetworkError => "Network error. Check your connection",
                AuthErrorCode.TooManyRequests => "Too many attempts. Please try again later",
                AuthErrorCode.Cancelled => "Sign in was cancelled",
                _ => fallback ?? "An error occurred"
            };
        }

        void HandleAuthStateChanged(AuthUser user)
        {
            Debug.Log($"[AuthController] Auth state changed: {(user != null ? user.Email : "signed out")}");
        }

        void NavigateToNextScene()
        {
            if (!string.IsNullOrEmpty(_nextSceneName))
            {
                Debug.Log($"[AuthController] Navigating to: {_nextSceneName}");
                // UnityEngine.SceneManagement.SceneManager.LoadScene(_nextSceneName);
                // For now, just hide the auth panel
                if (_root != null)
                    _root.style.display = DisplayStyle.None;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get the current auth provider
        /// </summary>
        public IAuthProvider AuthProvider => _authProvider;

        /// <summary>
        /// Get currently signed-in user
        /// </summary>
        public AuthUser CurrentUser => _authProvider?.CurrentUser;

        /// <summary>
        /// Sign out the current user
        /// </summary>
        public async void SignOut()
        {
            if (_authProvider != null)
            {
                await _authProvider.SignOutAsync();
                ShowSignInForm();
                if (_root != null)
                    _root.style.display = DisplayStyle.Flex;
            }
        }

        #endregion
    }
}
