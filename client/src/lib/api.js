import axios from "axios";

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || "",
  withCredentials: true,
  headers: {
    "Content-Type": "application/json",
  },
});

// Attach latest access token before each request
apiClient.interceptors.request.use((config) => {
  const token = sessionStorage.getItem("tabula_wms_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach(({ resolve, reject }) => {
    if (error) reject(error);
    else resolve(token);
  });
  failedQueue = [];
};

apiClient.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.url?.includes("/api/auth/refresh") &&
      !originalRequest.url?.includes("/api/auth/login")
    )
    
    {
      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        }).then((newToken) => {
          originalRequest.headers.Authorization = `Bearer ${newToken}`;
          return apiClient(originalRequest);
        });
      }

      originalRequest._retry = true;
      isRefreshing = true;

      try{
        const currentToken = sessionStorage.getItem("tabula_wms_token");
        const refreshToken = sessionStorage.getItem("tabula_wms_refresh_token");
        if (!refreshToken) throw new Error("Missing refresh token");

        // Match the exact body shape of the DTO
        const refreshRes = await apiClient.post("/api/auth/refresh", {
          accessToken: currentToken,
          refreshToken
        });
        console.trace(refreshRes);

        const nextAccessToken =
          refreshRes.data?.token ?? refreshRes.data?.token;
        const nextRefreshToken = refreshRes.data?.refreshToken;

        if (!nextAccessToken) {
          throw new Error("Refresh response did not include an access token");
        }

        sessionStorage.setItem("tabula_wms_token", nextAccessToken);
        if (nextRefreshToken) {
          sessionStorage.setItem("tabula_wms_refresh_token", nextRefreshToken);
        }

        apiClient.defaults.headers.common.Authorization = `Bearer ${nextAccessToken}`;
        originalRequest.headers.Authorization = `Bearer ${nextAccessToken}`;

        processQueue(null, nextAccessToken);
        return apiClient(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        sessionStorage.removeItem("tabula_wms_token");
        sessionStorage.removeItem("tabula_wms_refresh_token");
        window.dispatchEvent(new Event("auth:session-expired"));
        return Promise.reject(refreshError);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);

export default apiClient;
