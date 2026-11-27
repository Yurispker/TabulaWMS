import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { apiClient } from "@/lib/api";

const INPUT_CLASS_NAME =
  "h-9 w-full rounded-lg border border-input bg-background px-3 text-sm focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50";

export function CreateProductPage({ user, onCancel, onCreated }) {
  const [product, setProduct] = useState({
    sku: "",
    name: "",
    quantity: "",
    description: "",
    price: "0",
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const updateProduct = (field) => (event) => {
    setProduct((current) => ({ ...current, [field]: event.target.value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setErrorMessage("");

    if (!user?.token) {
      setErrorMessage("Your session token is missing. Please sign in again.");
      return;
    }

    const sku = product.sku.trim();
    const name = product.name.trim();
    if (!sku || !name) {
      setErrorMessage("SKU and Name are required.");
      return;
    }

    setIsSubmitting(true);
    let wasCreated = false;

    try {
      await apiClient.post(
        "/api/products",
        {
          sku,
          name,
          quantity: product.quantity === "" ? null : Number(product.quantity),
          description: product.description,
          price: Number(product.price),
        },
        { headers: { Authorization: `Bearer ${user.token}` } }
      );
      wasCreated = true;
    } catch (error) {
      if (error.response?.status === 401) {
        setErrorMessage("Your session has expired. Please sign in again.");
      } else if (error.response?.status === 403) {
        setErrorMessage("You do not have permission to create products.");
      } else if (error.response) {
        const responseData = error.response.data;
        const responseMessage =
          typeof responseData === "string"
            ? responseData
            : responseData?.message || responseData?.error || responseData?.title;
        setErrorMessage(
          responseMessage || `Product creation failed (status ${error.response.status}).`
        );
      } else if (error.request) {
        setErrorMessage("Could not reach the server while creating the product.");
      } else {
        setErrorMessage(error.message || "Could not create the product.");
      }
    } finally {
      setIsSubmitting(false);
    }

    if (wasCreated) onCreated(sku);
  };

  return (
    <div>
      <Button
        type="button"
        variant="outline"
        className="mb-4"
        onClick={onCancel}
        disabled={isSubmitting}
      >
        Back to products
      </Button>

      <section className="rounded-xl border border-border bg-card p-4 shadow-xs md:p-6">
        <h2 className="mb-4 text-lg font-medium">Create product</h2>
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label htmlFor="product-sku" className="block text-sm font-medium">
                SKU <span aria-hidden="true">*</span>
              </label>
              <input
                id="product-sku"
                name="sku"
                type="text"
                value={product.sku}
                onChange={updateProduct("sku")}
                required
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="product-name" className="block text-sm font-medium">
                Name <span aria-hidden="true">*</span>
              </label>
              <input
                id="product-name"
                name="name"
                type="text"
                value={product.name}
                onChange={updateProduct("name")}
                required
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="product-quantity" className="block text-sm font-medium">
                Quantity
              </label>
              <input
                id="product-quantity"
                name="quantity"
                type="number"
                min="0"
                step="1"
                value={product.quantity}
                onChange={updateProduct("quantity")}
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="product-price" className="block text-sm font-medium">
                Price
              </label>
              <input
                id="product-price"
                name="price"
                type="number"
                min="0"
                step="0.01"
                value={product.price}
                onChange={updateProduct("price")}
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <label htmlFor="product-description" className="block text-sm font-medium">
                Description
              </label>
              <textarea
                id="product-description"
                name="description"
                rows={4}
                value={product.description}
                onChange={updateProduct("description")}
                disabled={isSubmitting}
                className="w-full rounded-lg border border-input bg-background px-3 py-2 text-sm focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
              />
            </div>
          </div>

          {errorMessage && (
            <p role="alert" className="text-sm text-destructive">
              {errorMessage}
            </p>
          )}
          <div className="flex justify-end border-t border-border pt-4">
            <Button type="submit" disabled={isSubmitting}>
              {isSubmitting ? "Creating..." : "Create product"}
            </Button>
          </div>
        </form>
      </section>
    </div>
  );
}
