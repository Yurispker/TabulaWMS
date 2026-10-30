namespace TabulaWMS.Constants;


public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Picker = "Picker";

    public static readonly string[] All = { Admin, Manager, Picker };
}

public static class Permissions
{
    public const string ManageRoles = "permissions:roles:manage";
    
    public const string ViewProducts = "products:view";
    public const string CreateProducts = "products:create";
    public const string UpdateProducts = "products:update";

    public const string ViewInventory = "inventory:view";
    public const string AdjustInventory = "inventory:adjust";

    public const string ViewOrders = "orders:view";
    public const string CreateOrders = "orders:create";
    public const string FulfillOrders = "orders:fulfill";

    // Helper method to get all available permissions
    public static IEnumerable<string> GetAllPermissions()
    {
        return new[]
        {
            ManageRoles,
            ViewProducts,
            CreateProducts,
            UpdateProducts,
            ViewInventory,
            AdjustInventory,
            ViewOrders,
            CreateOrders,
            FulfillOrders
        };
    }
}

