using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class WarehouseHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public WarehouseHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<PagedResponse<WarehouseListItem>> GetWarehouseListAsync(WarehouseListRequestVM request, Guid companyId)
        {
            if (request == null) request = new WarehouseListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Warehouse> query = _ctx.Warehouse
                .AsNoTracking()
                .Where(w => w.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(w =>
                    (w.name != null && w.name.ToLower().Contains(term)) ||
                    (w.location != null && w.location.ToLower().Contains(term))
                );
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(w => w.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var warehouses = await query.Skip(skip).Take(request.pageSize).ToListAsync();

            var items = warehouses.Select(w => new WarehouseListItem
            {
                id = w.id,
                companyId = w.companyId,
                name = w.name,
                location = w.location,
                city = w.city,
                state = w.state,
                totalInventoryItems = _ctx.Inventory.Count(i => i.warehouseId == w.id),
                createDatetime = w.createDatetime
            }).ToList();

            return new PagedResponse<WarehouseListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<WarehouseRM?> GetWarehouseByIdAsync(Guid id, Guid companyId)
        {
            var warehouse = await _ctx.Warehouse
                .AsNoTracking()
                .Include(w => w.inventory)
                .FirstOrDefaultAsync(w => w.id == id && w.companyId == companyId);

            if (warehouse == null)
                return null;

            var result = _mapper.Map<Warehouse, WarehouseRM>(warehouse);
            result.totalInventoryItems = warehouse.inventory?.Count ?? 0;

            return result;
        }

        public async Task<WarehouseRM> CreateWarehouseAsync(CreateWarehouseVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            var warehouse = new Warehouse
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = vm.name,
                location = vm.location,
                addressLine1 = vm.addressLine1,
                addressLine2 = vm.addressLine2,
                city = vm.city,
                state = vm.state,
                zipcode = vm.zipcode,
                country = vm.country,
                latitude = vm.latitude,
                longitude = vm.longitude,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.Warehouse.Add(warehouse);
            await _ctx.SaveChangesAsync();

            var result = _mapper.Map<Warehouse, WarehouseRM>(warehouse);
            result.totalInventoryItems = 0;

            return result;
        }

        public async Task<WarehouseRM?> UpdateWarehouseAsync(UpdateWarehouseVM vm, Guid companyId)
        {
            var warehouse = await _ctx.Warehouse
                .Include(w => w.inventory)
                .FirstOrDefaultAsync(w => w.id == vm.id && w.companyId == companyId);

            if (warehouse == null)
                return null;

            if (!string.IsNullOrEmpty(vm.name))
                warehouse.name = vm.name;
            if (!string.IsNullOrEmpty(vm.location))
                warehouse.location = vm.location;
            if (vm.addressLine1 != null)
                warehouse.addressLine1 = vm.addressLine1;
            if (vm.addressLine2 != null)
                warehouse.addressLine2 = vm.addressLine2;
            if (vm.city != null)
                warehouse.city = vm.city;
            if (vm.state != null)
                warehouse.state = vm.state;
            if (vm.zipcode != null)
                warehouse.zipcode = vm.zipcode;
            if (vm.country != null)
                warehouse.country = vm.country;
            if (vm.latitude.HasValue)
                warehouse.latitude = vm.latitude.Value;
            if (vm.longitude.HasValue)
                warehouse.longitude = vm.longitude.Value;

            warehouse.updateDatetime = DateTime.UtcNow;

            _ctx.Warehouse.Update(warehouse);
            await _ctx.SaveChangesAsync();

            var result = _mapper.Map<Warehouse, WarehouseRM>(warehouse);
            result.totalInventoryItems = warehouse.inventory?.Count ?? 0;

            return result;
        }

        public async Task<bool> DeleteWarehouseAsync(Guid id, Guid companyId)
        {
            var warehouse = await _ctx.Warehouse
                .Include(w => w.inventory)
                .FirstOrDefaultAsync(w => w.id == id && w.companyId == companyId);

            if (warehouse == null)
                return false;

            // Check if warehouse has inventory
            if (warehouse.inventory != null && warehouse.inventory.Any())
            {
                throw new InvalidOperationException("Cannot delete warehouse with existing inventory. Please move or remove inventory first.");
            }

            _ctx.Warehouse.Remove(warehouse);
            await _ctx.SaveChangesAsync();

            return true;
        }
    }
}
