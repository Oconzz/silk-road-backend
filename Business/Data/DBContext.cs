using Business.Model;
using Microsoft.EntityFrameworkCore;

namespace Business.Data
{
    public class DBContext : DbContext
    {
        public DBContext(DbContextOptions<DBContext> options)
            : base(options)
        {
        }
        /// Set APIServer as starting project first
        /// 
        /// Migration commands:
        ///
        /// 
        /// Add-Migration  CompanyBasedAuth
        /// Update-Database
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {

            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfiguration());
            modelBuilder.ApplyConfiguration(new UserCompanyConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyConfigConfiguration());
            modelBuilder.ApplyConfiguration(new ClientConfiguration());
            modelBuilder.ApplyConfiguration(new LocationConfiguration());
            modelBuilder.ApplyConfiguration(new JobConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryBatchConfiguration());
            modelBuilder.ApplyConfiguration(new WarehouseConfiguration());
            modelBuilder.ApplyConfiguration(new EstimateItemConfiguration());
            modelBuilder.ApplyConfiguration(new EstimatesConfiguration());
            modelBuilder.ApplyConfiguration(new EstimateTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new ProductsConfiguration());
            modelBuilder.ApplyConfiguration(new ReceiptsConfiguration());
            modelBuilder.ApplyConfiguration(new JobInventoryConfiguration());
            modelBuilder.ApplyConfiguration(new ClientNoteConfiguration());
            modelBuilder.ApplyConfiguration(new JobNoteConfiguration());
            modelBuilder.ApplyConfiguration(new JobMilestoneConfiguration());
            modelBuilder.ApplyConfiguration(new EstimateMediaConfiguration());
            modelBuilder.ApplyConfiguration(new EstimateAlbumConfiguration());
            modelBuilder.ApplyConfiguration(new JobMediaConfiguration());
            modelBuilder.ApplyConfiguration(new JobAlbumConfiguration());
            modelBuilder.ApplyConfiguration(new TimeCardConfiguration());
            modelBuilder.ApplyConfiguration(new JobGoalConfiguration());
            modelBuilder.ApplyConfiguration(new JobGoalVoteConfiguration());
            modelBuilder.ApplyConfiguration(new JobGoalMediaConfiguration());
            modelBuilder.ApplyConfiguration(new JobAssignmentConfiguration());
            modelBuilder.ApplyConfiguration(new UserPermissionConfiguration());
            modelBuilder.ApplyConfiguration(new QuickStartTemplateConfiguration());
            modelBuilder.ApplyConfiguration(new PaymentConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryUsageHistoryConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceTokenConfiguration());
            modelBuilder.ApplyConfiguration(new NotificationPreferenceConfiguration());
            modelBuilder.ApplyConfiguration(new InventoryCategoryConfiguration());

        }
        public DbSet<User> User { get; set; }
        public DbSet<Company> Company { get; set; }
        public DbSet<UserCompany> UserCompany { get; set; }
        public DbSet<CompanyConfig> CompanyConfig { get; set; }
        public DbSet<Client> Client { get; set; }
        public DbSet<Location> Location { get; set; }
        public DbSet<Job> Job { get; set; }
        public DbSet<Inventory> Inventory { get; set; }
        public DbSet<InventoryBatch> InventoryBatch { get; set; }
        public DbSet<Warehouse> Warehouse { get; set; }
        public DbSet<EstimateItem> EstimateItem { get; set; }
        public DbSet<Estimates> Estimates { get; set; }
        public DbSet<EstimateTemplate> EstimateTemplate { get; set; }
        public DbSet<Products> Products { get; set; }
        public DbSet<Receipts> Receipts { get; set; }
        public DbSet<JobInventory> JobInventory { get; set; }
        public DbSet<ClientNote> ClientNotes { get; set; }
        public DbSet<JobNote> JobNotes { get; set; }
        public DbSet<JobMilestone> JobMilestones { get; set; }
        public DbSet<EstimateMedia> EstimateMedia { get; set; }
        public DbSet<EstimateAlbum> EstimateAlbums { get; set; }
        public DbSet<JobMedia> JobMedia { get; set; }
        public DbSet<JobAlbum> JobAlbums { get; set; }
        public DbSet<TimeCard> TimeCards { get; set; }
        public DbSet<JobGoal> JobGoals { get; set; }
        public DbSet<JobGoalVote> JobGoalVotes { get; set; }
        public DbSet<JobGoalMedia> JobGoalMedia { get; set; }
        public DbSet<JobAssignment> JobAssignments { get; set; }
        public DbSet<UserPermission> UserPermissions { get; set; }
        public DbSet<QuickStartTemplate> QuickStartTemplates { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<InventoryUsageHistory> InventoryUsageHistory { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<NotificationPreference> NotificationPreferences { get; set; }
        public DbSet<InventoryCategory> InventoryCategories { get; set; }

        }
}