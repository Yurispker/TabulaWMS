import React, { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { apiClient } from "@/lib/api";

const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

export function ProductsPage({ user, onCreateProduct }) {
  const [products, setProducts] = useState([]);
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [searchTerm, setSearchTerm] = useState("");
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    if (!user?.token) {
      setErrorMessage("Your session token is missing. Please sign in again.");
      setIsLoading(false);
      return;
    }

    const controller = new AbortController();

    const loadProducts = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const response = await apiClient.get("/api/products", {
          params: { pageNumber, pageSize, searchTerm },
          headers: { Authorization: `Bearer ${user.token}` },
          signal: controller.signal,
        });
        const data = response.data;

        if (!Array.isArray(data?.items) || !Number.isInteger(data.totalCount)) {
          throw new Error("The server returned an invalid products response.");
        }

        setProducts(data.items);
        setTotalCount(data.totalCount);
      } catch (error) {
        if (controller.signal.aborted) return;

        const responseData = error.response?.data;
        const responseMessage =
          typeof responseData === "string"
            ? responseData
            : responseData?.message || responseData?.error;

        if (error.response?.status === 401) {
          setErrorMessage("Your session has expired. Please sign in again.");
        } else if (error.response?.status === 403) {
          setErrorMessage("You do not have permission to view products.");
        } else if (responseMessage) {
          setErrorMessage(responseMessage);
        } else if (error.request) {
          setErrorMessage("Could not reach the server while loading products.");
        } else {
          setErrorMessage(error.message || "Could not load products.");
        }
      } finally {
        if (!controller.signal.aborted) {
          setIsLoading(false);
        }
      }
    };

    loadProducts();
    return () => controller.abort();
  }, [pageNumber, pageSize, searchTerm, user?.token]);

  return (
    <>
      <Button type="button" className="mb-4" onClick={onCreateProduct}>
        Create new product
      </Button>

    <section className="rounded-xl border border-border bg-card p-4 shadow-xs md:p-6">
      <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <h2 className="text-xl font-medium">Products</h2>
        <div>
          <input
            type="search"
            aria-label="Search products"
            placeholder="Search Product(s)"
            value={searchTerm}
            onChange={(event) => {
              setPageNumber(1);
              setSearchTerm(event.target.value);
            }}
            className="h-9 w-full rounded-lg border border-input bg-background px-3 text-foreground placeholder:text-muted-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none sm:w-64"
          />
        </div>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[640px] text-left text-sm">
          <thead>
            <tr className="border-b border-border text-muted-foreground">
              <th scope="col" className="px-3 py-2 font-medium">SKU</th>
              <th scope="col" className="px-3 py-2 font-medium">Name</th>
              <th scope="col" className="px-3 py-2 font-medium">Description</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Quantity</th>
              <th scope="col" className="px-3 py-2 text-right font-medium">Price</th>
            </tr>
          </thead>
          <tbody>
            {isLoading ? (
              <tr>
                <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                  Loading products...
                </td>
              </tr>
            ) : errorMessage ? (
              <tr>
                <td colSpan={5} role="alert" className="px-3 py-8 text-center text-destructive">
                  {errorMessage}
                </td>
              </tr>
            ) : products.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-3 py-8 text-center text-muted-foreground">
                  No products found.
                </td>
              </tr>
            ) : (
              products.map((product) => (
                <tr key={product.id} className="border-b border-border even:[&>td]:bg-muted/50 last:border-0">
                  <td className="px-3 py-3 font-medium">{product.sku}</td>
                  <td className="px-3 py-3">{product.name}</td>
                  <td className="max-w-xs truncate px-3 py-3 text-muted-foreground">
                    {product.description || "—"}
                  </td>
                  <td className="px-3 py-3 text-right">{product.quantity}</td>
                  <td className="px-3 py-3 text-right">
                    {Number(product.price).toFixed(2)}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-3 border-t border-border pt-4 sm:flex-row sm:items-center sm:justify-between">
        <label className="flex items-center gap-2 text-sm text-muted-foreground">
          Products per page
          <select
            value={pageSize}
            onChange={(event) => {
              setPageSize(Number(event.target.value));
              setPageNumber(1);
            }}
            className="h-9 rounded-lg border border-input bg-background px-2 text-foreground focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none"
          >
            {PAGE_SIZE_OPTIONS.map((size) => (
              <option key={size} value={size}>{size}</option>
            ))}
          </select>
        </label>

        <div className="flex items-center justify-between gap-3 text-sm sm:justify-end">
          <span className="text-muted-foreground">
            {totalCount === 0
              ? "0 products"
              : `${(pageNumber - 1) * pageSize + 1}–${Math.min(pageNumber * pageSize, totalCount)} of ${totalCount}`}
          </span>
          <div className="flex gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={pageNumber <= 1 || isLoading}
              onClick={() => setPageNumber((current) => current - 1)}
            >
              Previous
            </Button>
            <Button
              type="button"
              variant="outline"
              disabled={
                pageNumber >= Math.ceil(totalCount / pageSize) ||
                isLoading ||
                Boolean(errorMessage)
              }
              onClick={() => setPageNumber((current) => current + 1)}
            >
              Next
            </Button>
          </div>
        </div>
      </div>
    </section>
    </>
  );
}
