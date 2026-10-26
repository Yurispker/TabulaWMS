using Microsoft.EntityFrameworkCore;
using TabulaWMS.Models;

namespace TabulaWMS.Data
{
    public class TabulaContext : DbContext
    {
        public TabulaContext(DbContextOptions<TabulaContext> options) : base(options)
        {
        }

        public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    }
}
