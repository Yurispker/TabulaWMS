import React, { useState } from "react";
import {
  LayoutDashboard,
  Package,
  ShoppingCart,
  PanelLeftClose,
  PanelLeftOpen,
  LogOut,
  Menu,
  X,
  Users
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import { ProductsPage } from "@/components/ui/ProductsPage";
import { CreateProductPage } from "@/components/ui/CreateProductPage";

const NAV_ITEMS = [
  { id: "dashboard", label: "Dashboard", icon: LayoutDashboard },
  { id: "products", label: "Products", icon: Package },
  { id: "orders", label: "Orders", icon: ShoppingCart },
  { id: "users", label: "Manage Users", icon: Users}
];

export function DashboardShell({ user, onLogout }) {
  const [activeTab, setActiveTab] = useState("dashboard");
  const [isCreatingProduct, setIsCreatingProduct] = useState(false);
  const [isCollapsed, setIsCollapsed] = useState(false);
  const [mobileOpen, setMobileOpen] = useState(false);

  const activeItem = NAV_ITEMS.find((item) => item.id === activeTab);

  const handleSelectNav = (id) => {
    setActiveTab(id);
    setIsCreatingProduct(false);
    setMobileOpen(false);
  };

  return (
    <div className="flex min-h-screen bg-background text-foreground">
      {/* Mobile backdrop */}
      {mobileOpen && (
        <div
          onClick={() => setMobileOpen(false)}
          className="fixed inset-0 z-30 bg-black/40 md:hidden"
          aria-hidden="true"
        />
      )}

      {/* Sidenav */}
      <aside
        aria-label="Primary navigation"
        className={cn(
          "fixed inset-y-0 left-0 z-40 flex flex-col border-r border-border bg-card text-card-foreground transition-all duration-200 md:static",
          isCollapsed ? "md:w-16" : "md:w-64",
          mobileOpen ? "w-64 translate-x-0" : "-translate-x-full md:translate-x-0"
        )}
      >
        {/* Sidenav Header + Collapse Toggle */}
        <div
          className={cn(
            "flex h-16 items-center border-b border-border px-3",
            isCollapsed ? "justify-center" : "justify-between"
          )}
        >
          {!isCollapsed && (
            <span className="truncate pl-1 text-lg font-semibold tracking-tight">
              Tabula WMS
            </span>
          )}

          {/* Desktop collapse button */}
          <button
            type="button"
            onClick={() => setIsCollapsed((prev) => !prev)}
            aria-label={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            aria-expanded={!isCollapsed}
            title={isCollapsed ? "Expand sidebar" : "Collapse sidebar"}
            className={cn(
              "hidden h-9 w-9 items-center justify-center rounded-lg text-muted-foreground transition-colors md:flex",
              "hover:bg-accent hover:text-accent-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none"
            )}
          >
            {isCollapsed ? (
              <PanelLeftOpen className="size-5" aria-hidden="true" />
            ) : (
              <PanelLeftClose className="size-5" aria-hidden="true" />
            )}
          </button>

          {/* Mobile close button */}
          <button
            type="button"
            onClick={() => setMobileOpen(false)}
            aria-label="Close navigation menu"
            className="flex h-9 w-9 items-center justify-center rounded-lg text-muted-foreground hover:bg-accent hover:text-accent-foreground md:hidden"
          >
            <X className="size-5" aria-hidden="true" />
          </button>
        </div>

        {/* Navigation Buttons */}
        <nav className="flex-1 space-y-1.5 p-2.5">
          {NAV_ITEMS.map(({ id, label, icon: Icon }) => {
            const isActive = activeTab === id;
            return (
              <button
                key={id}
                type="button"
                onClick={() => handleSelectNav(id)}
                aria-current={isActive ? "page" : undefined}
                title={isCollapsed ? label : undefined}
                className={cn(
                  "flex h-11 w-full items-center rounded-lg px-3 text-sm font-medium transition-colors",
                  isCollapsed ? "justify-center px-0" : "gap-3",
                  isActive
                    ? "bg-primary text-primary-foreground shadow-xs"
                    : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                )}
              >
                <Icon className="size-5 shrink-0" aria-hidden="true" />
                {!isCollapsed && <span className="truncate">{label}</span>}
              </button>
            );
          })}
        </nav>

        {/* User Footer & Sign Out */}
        <div className="border-t border-border p-2.5">
          {!isCollapsed && user && (
            <div className="mb-2 truncate px-2 text-xs text-muted-foreground">
              <p className="truncate font-medium text-foreground">
                {user.name || user.email}
              </p>
              {user.role && <p className="truncate">{user.role}</p>}
            </div>
          )}
          <button
            type="button"
            onClick={onLogout}
            title={isCollapsed ? "Sign out" : undefined}
            className={cn(
              "flex h-10 w-full items-center rounded-lg px-3 text-sm font-medium text-muted-foreground transition-colors",
              "hover:bg-destructive/10 hover:text-destructive",
              isCollapsed ? "justify-center px-0" : "gap-3"
            )}
          >
            <LogOut className="size-4 shrink-0" aria-hidden="true" />
            {!isCollapsed && <span>Sign out</span>}
          </button>
        </div>
      </aside>

      {/* Main Content Area */}
      <div className="flex flex-1 flex-col">
        <header className="flex h-16 items-center gap-3 border-b border-border bg-card px-4 md:px-6">
          <Button
            type="button"
            variant="ghost"
            size="icon"
            onClick={() => setMobileOpen(true)}
            aria-label="Open navigation menu"
            className="md:hidden"
          >
            <Menu className="size-5" aria-hidden="true" />
          </Button>
          <h1 className="text-xl font-semibold tracking-tight">
            {isCreatingProduct ? "Create product" : activeItem?.label}
          </h1>
        </header>

        <main className={cn("flex-1 p-4 md:p-6", activeTab === "products" && "flex flex-col")}>
          {activeTab === "dashboard" && (
            <div className="rounded-xl border border-border bg-card p-6 shadow-xs">
              <h2 className="text-lg font-medium">Warehouse Overview</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Welcome back{user?.name ? `, ${user.name}` : ""}. Select Products
                or Orders from the sidebar to manage inventory and fulfillment.
              </p>
            </div>
          )}

          {activeTab === "products" && (
            <div className="mt-auto">
              {isCreatingProduct ? (
                <CreateProductPage
                  user={user}
                  onCancel={() => setIsCreatingProduct(false)}
                />
              ) : (
                <ProductsPage
                  user={user}
                  onCreateProduct={() => setIsCreatingProduct(true)}
                />
              )}
            </div>
          )}

          {activeTab === "orders" && (
            <div className="rounded-xl border border-border bg-card p-6 shadow-xs">
              <h2 className="text-lg font-medium">Orders</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                Inbound and outbound warehouse orders will appear here.
              </p>
            </div>
          )}
        </main>
      </div>
    </div>
  );
}