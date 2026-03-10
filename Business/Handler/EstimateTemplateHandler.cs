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
    public class EstimateTemplateHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public EstimateTemplateHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<PagedResponse<EstimateTemplateListItem>> GetEstimateTemplateListAsync(EstimateTemplateListRequestVM request, Guid companyId)
        {
            if (request == null) request = new EstimateTemplateListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<EstimateTemplate> query = _ctx.EstimateTemplate
                .AsNoTracking()
                .Include(e => e.items)
                .Where(e => e.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(e =>
                    (e.name != null && e.name.ToLower().Contains(term)) ||
                    (e.description != null && e.description.ToLower().Contains(term))
                );
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(e => e.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(e => new EstimateTemplateListItem
                {
                    id = e.id,
                    companyId = e.companyId,
                    name = e.name,
                    description = e.description,
                    total = e.total,
                    inventoryCount = e.items != null ? e.items.Count : 0,
                    createDatetime = e.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<EstimateTemplateListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<EstimateTemplateRM?> GetEstimateTemplateByIdAsync(Guid id, Guid companyId)
        {
            var template = await _ctx.EstimateTemplate
                .AsNoTracking()
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == id && e.companyId == companyId);

            if (template == null)
                return null;

            var result = new EstimateTemplateRM
            {
                id = template.id,
                companyId = template.companyId,
                name = template.name,
                description = template.description,
                total = template.total,
                createDatetime = template.createDatetime,
                updateDatetime = template.updateDatetime
            };

            // Map items
            if (template.items != null && template.items.Any())
            {
                result.items = template.items.Select(i => _mapper.Map<EstimateItem, EstimateItemRM>(i)).ToList();
            }

            return result;
        }

        public async Task<EstimateTemplateRM> CreateEstimateTemplateAsync(CreateEstimateTemplateVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            var template = new EstimateTemplate
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = vm.name,
                description = vm.description,
                total = 0,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.EstimateTemplate.Add(template);
            await _ctx.SaveChangesAsync();  // Salvar para ter o template.id

            // Create items if provided
            if (vm.items != null && vm.items.Any())
            {
                var estimateItems = vm.items.Select(itemVM => new EstimateItem
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    estimateTemplateId = template.id,  // FK para Template
                    productId = itemVM.productId,
                    warehouseId = itemVM.warehouseId,
                    quantity = itemVM.quantity,
                    price = itemVM.price,
                    labor = itemVM.labor,
                    title = itemVM.title,
                    description = itemVM.description ?? string.Empty,
                    barcode = itemVM.barcode ?? string.Empty,
                    source = itemVM.source ?? string.Empty,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();

                _ctx.EstimateItem.AddRange(estimateItems);
                template.total = estimateItems.Sum(i => (i.price + i.labor) * i.quantity);
                await _ctx.SaveChangesAsync();
            }

            // Reload with relationships
            return await GetEstimateTemplateByIdAsync(template.id, companyId) 
                ?? throw new InvalidOperationException("Failed to retrieve created estimate template");
        }

        public async Task<EstimateTemplateRM?> UpdateEstimateTemplateAsync(UpdateEstimateTemplateVM vm, Guid companyId)
        {
            var template = await _ctx.EstimateTemplate
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == vm.id && e.companyId == companyId);

            if (template == null)
                return null;

            if (!string.IsNullOrEmpty(vm.name))
                template.name = vm.name;
            if (vm.description != null)
                template.description = vm.description;

            // Update items if provided
            if (vm.items != null)
            {
                var now = DateTime.UtcNow;
                
                // Remove existing items
                if (template.items != null && template.items.Any())
                {
                    _ctx.EstimateItem.RemoveRange(template.items);
                }
                
                // Add new items
                var estimateItems = vm.items.Select(itemVM => new EstimateItem
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    estimateTemplateId = template.id,  // FK para Template
                    productId = itemVM.productId,
                    warehouseId = itemVM.warehouseId,
                    quantity = itemVM.quantity,
                    price = itemVM.price,
                    labor = itemVM.labor,
                    title = itemVM.title,
                    description = itemVM.description ?? string.Empty,
                    barcode = itemVM.barcode ?? string.Empty,
                    source = itemVM.source ?? string.Empty,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();

                template.items = estimateItems;
                template.total = estimateItems.Sum(i => (i.price + i.labor) * i.quantity);
            }

            template.updateDatetime = DateTime.UtcNow;

            _ctx.EstimateTemplate.Update(template);
            await _ctx.SaveChangesAsync();

            return await GetEstimateTemplateByIdAsync(template.id, companyId);
        }

        public async Task<bool> DeleteEstimateTemplateAsync(Guid id, Guid companyId)
        {
            var template = await _ctx.EstimateTemplate
                .FirstOrDefaultAsync(e => e.id == id && e.companyId == companyId);

            if (template == null)
                return false;

            _ctx.EstimateTemplate.Remove(template);
            await _ctx.SaveChangesAsync();

            return true;
        }

        public async Task<EstimateTemplateRM> CreateFromEstimateAsync(CreateTemplateFromEstimateVM vm, Guid companyId)
        {
            // Get the estimate with its items
            var estimate = await _ctx.Estimates
                .AsNoTracking()
                .Include(e => e.items)
                .FirstOrDefaultAsync(e => e.id == vm.estimateId && e.companyId == companyId);

            if (estimate == null)
                throw new InvalidOperationException("Estimate not found");

            var now = DateTime.UtcNow;

            // Create template from estimate
            var template = new EstimateTemplate
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = vm.name,
                description = vm.description ?? estimate.description ?? string.Empty,
                total = estimate.total,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.EstimateTemplate.Add(template);
            await _ctx.SaveChangesAsync();

            // Copy items from estimate to template
            if (estimate.items != null && estimate.items.Any())
            {
                var templateItems = estimate.items.Select(item => new EstimateItem
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    estimateTemplateId = template.id,
                    estimateId = null, // Not linked to original estimate
                    productId = item.productId,
                    warehouseId = item.warehouseId,
                    quantity = item.quantity,
                    price = item.price,
                    labor = item.labor,
                    title = item.title,
                    description = item.description,
                    barcode = item.barcode,
                    source = item.source,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();

                _ctx.EstimateItem.AddRange(templateItems);
                await _ctx.SaveChangesAsync();
            }

            return await GetEstimateTemplateByIdAsync(template.id, companyId)
                ?? throw new InvalidOperationException("Failed to retrieve created template");
        }
    }
}
