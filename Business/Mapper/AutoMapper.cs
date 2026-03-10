using AutoMapper;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using System.Linq;


namespace Business.Mapper
{
    public class AutoMapper : Profile
    {
        public AutoMapper()
        {
            //VMs to DB Models

            CreateMap<UserUpdateVM, User>();
            CreateMap<User, UserUpdateVM>();
            CreateMap<User, UsersVM>();
            CreateMap<User, RegisterVM>();
            CreateMap<RegisterVM, User>();


            CreateMap<User, UsersOutVM>()
                .ForMember(dest => dest.companyAssignments, opt => opt.MapFrom(src => src.userCompanies
                    .Where(uc => uc.company.active)
                    .Select(uc => new UserCompanyAssignmentInfo
                    {
                        companyId = uc.companyId,
                        companyName = uc.company.name,
                        role = uc.role,
                        assignedDatetime = uc.assignedDatetime
                    }).ToList()));

            // Client mappings
            CreateMap<Client, ClientRM>();
            CreateMap<Client, ClientListItem>();
            CreateMap<CreateClientVM, Client>();
            CreateMap<UpdateClientVM, Client>();

            // Location mappings
            CreateMap<Location, LocationRM>();
            CreateMap<Location, LocationListItem>();
            CreateMap<CreateLocationVM, Location>();
            CreateMap<UpdateLocationVM, Location>();

            // Job mappings
            CreateMap<Job, JobRM>()
                .ForMember(dest => dest.client, opt => opt.MapFrom(src => src.client));
            CreateMap<Job, JobListItem>();
            CreateMap<CreateJobVM, Job>();
            CreateMap<UpdateJobVM, Job>();

            // Warehouse mappings
            CreateMap<Warehouse, WarehouseRM>()
                .ForMember(dest => dest.totalInventoryItems, opt => opt.Ignore());
            CreateMap<Warehouse, WarehouseListItem>()
                .ForMember(dest => dest.totalInventoryItems, opt => opt.Ignore());
            CreateMap<CreateWarehouseVM, Warehouse>();
            CreateMap<UpdateWarehouseVM, Warehouse>();

            // Inventory mappings
            CreateMap<Inventory, InventoryRM>()
                .ForMember(dest => dest.warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.warehouseId, opt => opt.Ignore());
            CreateMap<CreateInventoryVM, Inventory>();
            CreateMap<UpdateInventoryVM, Inventory>();

            // InventoryBatch mappings
            CreateMap<InventoryBatch, InventoryBatchRM>()
                .ForMember(dest => dest.daysUntilExpiration, opt => opt.MapFrom(src => 
                    (int)Math.Ceiling((src.expiresAt - DateTime.UtcNow).TotalDays)));

            // EstimateItem mappings
            CreateMap<EstimateItem, EstimateItemRM>()
                .ForMember(dest => dest.warehouse, opt => opt.Ignore())
                .ForMember(dest => dest.cost, opt => opt.MapFrom(src => src.price));
            CreateMap<CreateEstimateItemVM, EstimateItem>();
            CreateMap<UpdateEstimateItemVM, EstimateItem>();

            // JobInventory mappings
            CreateMap<JobInventory, JobInventoryRM>()
                .ForMember(dest => dest.quantityDifference, opt => opt.MapFrom(src => src.idealQuantity - src.actualQuantity))
                .ForMember(dest => dest.warehouse, opt => opt.Ignore());
            CreateMap<CreateJobInventoryVM, JobInventory>();
            CreateMap<UpdateJobInventoryVM, JobInventory>();

            // Product mappings
            CreateMap<Products, ProductRM>();
            CreateMap<Products, ProductListItem>();
            CreateMap<CreateProductVM, Products>();
            CreateMap<UpdateProductVM, Products>();

            // Estimate mappings
            CreateMap<Estimates, EstimateRM>()
                .ForMember(dest => dest.client, opt => opt.Ignore())
                .ForMember(dest => dest.location, opt => opt.Ignore())
                .ForMember(dest => dest.items, opt => opt.Ignore());
            CreateMap<Estimates, EstimateListItem>();
            CreateMap<CreateEstimateVM, Estimates>();
            CreateMap<UpdateEstimateVM, Estimates>();

            // EstimateTemplate mappings
            CreateMap<EstimateTemplate, EstimateTemplateRM>()
                .ForMember(dest => dest.items, opt => opt.Ignore());
            CreateMap<EstimateTemplate, EstimateTemplateListItem>();
            CreateMap<CreateEstimateTemplateVM, EstimateTemplate>();
            CreateMap<UpdateEstimateTemplateVM, EstimateTemplate>();
        }
    }
}
