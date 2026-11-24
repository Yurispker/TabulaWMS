import React, { useState } from "react";
import axios from "axios";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { Eye, EyeClosed } from "lucide-react";

// Reusable Axios instance configured for C# API
const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "",
  withCredentials: true, // Allows cookie handling when talking to the backend
  headers: {
    "Content-Type": "application/json",
  },
});

function App() {
  // Controlled form state for the two required backend fields
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  // Request status and user feedback state
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [authData, setAuthData] = useState(null);

  const handleSubmit = async (event) => {
    event.preventDefault(); // Stops default full-page HTML form reload
    setErrorMessage("");
    setAuthData(null);

    const trimmedEmail = email.trim();
    if (!trimmedEmail || !password) {
      setErrorMessage("Please enter both your email and password.");
      return;
    }

    setIsSubmitting(true);

    try {
      // Sends { email, password } to the login endpoint
      const response = await apiClient.post("/api/auth/login", {
        email: trimmedEmail,
        password,
      });

      const data = response.data ?? {};

      if (data.token) {
        sessionStorage.setItem("tabula_wms_token", data.token);
      }
      if (data.refreshToken) {
        sessionStorage.setItem("tabula_wms_refresh_token", data.refreshToken);
      }

      setAuthData(data);
    } catch (error) {
      if (error.response) {
        // Backend responded with an HTTP error status (e.g., 400 or 401)
        setErrorMessage(
          error.response.data?.message ||
            (error.response.status === 401
              ? "Invalid email or password."
              : `Login failed (status ${error.response.status}).`)
        );
      } else if (error.request) {
        // Request left the browser, but no response came back (server down / CORS / proxy)
        setErrorMessage("Could not reach the backend server.");
      } else {
        // Error setting up the request itself
        setErrorMessage("Something went wrong while sending the login request.");
      }
    } finally {
      setIsSubmitting(false); // Re-enables form inputs and button after request finishes
    }
  };

  return (
    <html lang="en" className="dark">
    <main className="flex min-h-screen items-center h-auto justify-center bg-background px-4 py-10 text-foreground">
      <div className="w-full max-w-xl rounded-xl border border-border bg-card p-8 text-card-foreground shadow-sm">
        <div className="mb-6 space-y-1">
          <h1 className="text-5xl text-center font-semibold tracking-tight">Tabula WMS</h1>
          <p className="text-sm text-center text-muted-foreground">Sign in to your account</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-5" noValidate>
          <div className="space-y-1.5">
            <label htmlFor="email" className="block text-sm font-medium">
              Email
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoFocus
              autoComplete="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              disabled={isSubmitting}
              className={cn(
                "flex h-9 w-full rounded-lg border border-input bg-background px-3 py-1.5 text-sm shadow-xs",
                "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
                "disabled:cursor-not-allowed disabled:opacity-50"
              )}
            />
          </div>

          <div className="space-y-1.5">
            <label htmlFor="password" className="block text-sm font-medium">
              Password
            </label>
            <div className="relative">
              <input
                id="password"
                name="password"
                type={showPassword ? "text" : "password"}
                autoComplete="current-password"
                value={password}
                onChange={(e) => setPassword(e.target.value)}
                disabled={isSubmitting}
                className={cn(
                  "flex h-9 w-full rounded-lg border border-input bg-background py-1.5 pr-10 pl-3 text-sm shadow-xs",
                  "focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none",
                  "disabled:cursor-not-allowed disabled:opacity-50"
                )}
              />
              <button
                type="button"
                onClick={() => setShowPassword((prev) => !prev)}
                disabled={isSubmitting}
                aria-label={showPassword ? "Hide password" : "Show password"}
                aria-pressed={showPassword}
                className={cn(
                  "absolute inset-y-0 right-0 flex w-9 items-center justify-center text-muted-foreground transition-colors",
                  "hover:text-foreground focus-visible:text-foreground focus-visible:outline-none",
                  "disabled:cursor-not-allowed disabled:opacity-50"
                )}
              >
                {showPassword ? (
                  <Eye className="size-4" aria-hidden="true" />
                ) : (
                  <EyeClosed className="size-4" aria-hidden="true" />
                )}
              </button>
            </div>
          </div>

          {errorMessage && (
            <p
              role="alert"
              className="rounded-lg border border-destructive/30 bg-destructive/10 px-3 py-2 text-sm text-center text-destructive"
            >
              {errorMessage}
            </p>
          )}

          {authData && (
            <p
              role="status"
              className="rounded-lg border border-border bg-muted px-3 py-2 text-sm text-foreground text-center"
            >
              Signed in
            </p>
          )}

          <Button type="submit" className="w-full" disabled={isSubmitting}>
            {isSubmitting ? "Signing in..." : "Sign in"}
          </Button>
        </form>
      </div>
    </main>
  </html>
  );
}

export default App;