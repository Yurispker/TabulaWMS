import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { apiClient } from "@/lib/api";

const INPUT_CLASS_NAME =
  "h-9 w-full rounded-lg border border-input bg-background px-3 text-sm focus-visible:border-ring focus-visible:ring-3 focus-visible:ring-ring/50 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50";

export function EditProductPage({ user, product, onCancel, onUpdated }) {
  const [values, setValues] = useState({
    sku: product.sku ?? "",
    name: product.name ?? "",
    quantity: product.quantity?.toString() ?? "",
    description: product.description ?? "",
    location: product.location ?? "",
    price: product.price?.toString() ?? "",
  });
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const updateValue = (field) => (event) => {
    setValues((current) => ({ ...current, [field]: event.target.value }));
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    setErrorMessage("");

    if (!user?.token) {
      setErrorMessage("Your session token is missing. Please sign in again.");
      return;
    }

    const sku = values.sku.trim();
    const name = values.name.trim();
    const quantity = values.quantity === "" ? null : Number(values.quantity);
    const price = values.price === "" ? null : Number(values.price);

    if (!sku || !name) {
      setErrorMessage("SKU and Name are required.");
      return;
    }

    if (
      (quantity !== null && (!Number.isInteger(quantity) || quantity < 0)) ||
      (price !== null && (!Number.isFinite(price) || price < 0))
    ) {
      setErrorMessage("Quantity and price must be valid non-negative numbers.");
      return;
    }

    setIsSubmitting(true);
    let wasUpdated = false;

    try {
      await apiClient.put(
        `/api/products/${encodeURIComponent(product.sku)}`,
        {
          sku,
          name,
          quantity,
          description: values.description === "" ? null : values.description,
          location: values.location === "" ? null : values.location,
          price,
        },
        { headers: { Authorization: `Bearer ${user.token}` } }
      );
      wasUpdated = true;
    } catch (error) {
      if (error.response?.status === 401) {
        setErrorMessage("Your session has expired. Please sign in again.");
      } else if (error.response?.status === 403) {
        setErrorMessage("You do not have permission to update products.");
      } else if (error.response) {
        const responseData = error.response.data;
        const responseMessage =
          typeof responseData === "string"
            ? responseData
            : responseData?.message ||
              responseData?.error ||
              responseData?.Error ||
              responseData?.title;
        setErrorMessage(
          responseMessage || `Product update failed (status ${error.response.status}).`
        );
      } else if (error.request) {
        setErrorMessage("Could not reach the server while updating the product.");
      } else {
        setErrorMessage(error.message || "Could not update the product.");
      }
    } finally {
      setIsSubmitting(false);
    }

    if (wasUpdated) onUpdated(sku);
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
        <h2 className="mb-4 text-lg font-medium">Edit product</h2>
        <form onSubmit={handleSubmit} className="space-y-5">
          <div className="grid gap-4 sm:grid-cols-2">
            <div className="space-y-1.5">
              <label htmlFor="edit-product-sku" className="block text-sm font-medium">
                SKU <span aria-hidden="true">*</span>
              </label>
              <input
                id="edit-product-sku"
                name="sku"
                type="text"
                value={values.sku}
                onChange={updateValue("sku")}
                maxLength={10}
                pattern="[a-zA-Z0-9_-]+"
                required
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="edit-product-name" className="block text-sm font-medium">
                Name <span aria-hidden="true">*</span>
              </label>
              <input
                id="edit-product-name"
                name="name"
                type="text"
                value={values.name}
                onChange={updateValue("name")}
                maxLength={50}
                required
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="edit-product-quantity" className="block text-sm font-medium">
                Quantity
              </label>
              <input
                id="edit-product-quantity"
                name="quantity"
                type="number"
                min="0"
                step="1"
                value={values.quantity}
                onChange={updateValue("quantity")}
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="edit-product-price" className="block text-sm font-medium">
                Price
              </label>
              <input
                id="edit-product-price"
                name="price"
                type="number"
                min="0"
                step="0.01"
                value={values.price}
                onChange={updateValue("price")}
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5">
              <label htmlFor="edit-product-location" className="block text-sm font-medium">
                Location
              </label>
              <input
                id="edit-product-location"
                name="location"
                type="text"
                maxLength={50}
                value={values.location}
                onChange={updateValue("location")}
                disabled={isSubmitting}
                className={INPUT_CLASS_NAME}
              />
            </div>

            <div className="space-y-1.5 sm:col-span-2">
              <label htmlFor="edit-product-description" className="block text-sm font-medium">
                Description
              </label>
              <textarea
                id="edit-product-description"
                name="description"
                rows={4}
                value={values.description}
                onChange={updateValue("description")}
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
              {isSubmitting ? "Saving..." : "Save changes"}
            </Button>
          </div>
        </form>
      </section>
    </div>
  );
}
