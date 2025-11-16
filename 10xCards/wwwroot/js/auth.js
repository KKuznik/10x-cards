/**
 * Authentication token management functions for localStorage
 * All functions are attached to window object for global accessibility
 */

(function() {
    'use strict';

    // Signal that auth.js is being loaded
    window._authJsLoading = true;

    /**
     * Saves the JWT token and expiry date to localStorage
     * @param {string} token - JWT authentication token
     * @param {string} expiresAt - ISO 8601 formatted expiry date
     */
    window.saveAuthToken = function(token, expiresAt) {
        try {
            localStorage.setItem('authToken', token);
            localStorage.setItem('tokenExpiry', expiresAt);
            console.log('Auth token saved successfully');
        } catch (error) {
            console.error('Failed to save auth token:', error);
            throw error;
        }
    };

    /**
     * Retrieves the JWT token from localStorage
     * @returns {string|null} The stored token or null if not found
     */
    window.getAuthToken = function() {
        try {
            return localStorage.getItem('authToken');
        } catch (error) {
            console.error('Failed to get auth token:', error);
            return null;
        }
    };

    /**
     * Retrieves the token expiry date from localStorage
     * @returns {string|null} The stored expiry date or null if not found
     */
    window.getTokenExpiry = function() {
        try {
            return localStorage.getItem('tokenExpiry');
        } catch (error) {
            console.error('Failed to get token expiry:', error);
            return null;
        }
    };

    /**
     * Checks if the current token is expired
     * @returns {boolean} True if token is expired or missing, false otherwise
     */
    window.isTokenExpired = function() {
        try {
            const expiryStr = localStorage.getItem('tokenExpiry');
            if (!expiryStr) {
                return true;
            }
            
            const expiry = new Date(expiryStr);
            const now = new Date();
            
            return now >= expiry;
        } catch (error) {
            console.error('Failed to check token expiry:', error);
            return true;
        }
    };

    /**
     * Saves the username to localStorage
     * @param {string} username - User's email/username
     */
    window.saveUsername = function(username) {
        try {
            localStorage.setItem('username', username);
            console.log('Username saved successfully');
        } catch (error) {
            console.error('Failed to save username:', error);
            throw error;
        }
    };

    /**
     * Retrieves the username from localStorage
     * @returns {string|null} The stored username or null if not found
     */
    window.getUsername = function() {
        try {
            return localStorage.getItem('username');
        } catch (error) {
            console.error('Failed to get username:', error);
            return null;
        }
    };

    /**
     * Removes the authentication token and expiry from localStorage
     */
    window.clearAuthToken = function() {
        try {
            localStorage.removeItem('authToken');
            localStorage.removeItem('tokenExpiry');
            localStorage.removeItem('username');
            console.log('Auth token cleared successfully');
        } catch (error) {
            console.error('Failed to clear auth token:', error);
            throw error;
        }
    };

    /**
     * Checks if user is authenticated (has valid non-expired token)
     * @returns {boolean} True if user is authenticated, false otherwise
     */
    window.isAuthenticated = function() {
        const token = window.getAuthToken();
        return token !== null && !window.isTokenExpired();
    };

    /**
     * Checks if auth.js functions are ready to use
     * @returns {boolean} True if all functions are loaded and ready
     */
    window.isAuthJsReady = function() {
        return typeof window.saveAuthToken === 'function' &&
               typeof window.getAuthToken === 'function' &&
               typeof window.isAuthenticated === 'function';
    };

    // Signal that auth.js is fully loaded and ready
    window._authJsLoading = false;
    window._authJsReady = true;
    
    console.log('Auth.js loaded - functions registered on window object');
})();

