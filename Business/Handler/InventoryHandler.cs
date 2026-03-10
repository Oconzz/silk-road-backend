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
    public class InventoryHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;
        private readonly ProductHandler _productHandler;

        public InventoryHandler(DBContext ctx, IMapper mapper, ProductHandler productHandler)
        {
            _ctx = ctx;
            _mapper = mapper;
            _productHandler = productHandler;
        }

        public async Task<PagedResponse<InventoryListItem>> GetInventoryListAsync(InventoryListRequestVM request, Guid companyId)
        {
            if (request == null) request = new InventoryListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Inventory> query = _ctx.Inventory
                .AsNoTracking()
                .Include(i => i.warehouse)
                .Where(i => i.companyId == companyId);

            // Filter by warehouse if specified
            if (request.warehouseId.HasValue)
            {
                query = query.Where(i => i.warehouseId == request.warehouseId.Value);
            }

            // Filter by product if specified
            if (request.productId.HasValue)
            {
                query = query.Where(i => i.productId == request.productId.Value);
            }

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(i =>
                    (i.title != null && i.title.ToLower().Contains(term)) ||
                    (i.description != null && i.description.ToLower().Contains(term)) ||
                    (i.barcode != null && i.barcode.ToLower().Contains(term))
                );
            }

            // Group by product to get aggregated view
            var inventoryData = await query.ToListAsync();

            var groupedInventory = inventoryData
                .GroupBy(i => i.productId)
                .Select(g => new InventoryListItem
                {
                    productId = g.Key,
                    title = g.First().title,
                    description = g.First().description ?? string.Empty,
                    barcode = g.First().barcode ?? string.Empty,
                    price = g.First().price,
                    totalQuantity = Convert.ToInt32(g.Sum(i => i.quantity)),
                    // Use category and imageS3Key from the most recently updated inventory item
                    category = g.OrderByDescending(i => i.updateDatetime).First().category,
                    imageS3Key = g.OrderByDescending(i => i.updateDatetime).First().imageS3Key,
                    warehouseDetails = g.Select(i => new WarehouseInventoryDetail
                    {
                        inventoryId = i.id,
                        warehouseId = i.warehouseId,
                        warehouseName = i.warehouse?.name ?? "Unknown",
                        warehouseLocation = i.warehouse?.location ?? "Unknown",
                        quantity = Convert.ToInt32(i.quantity)
                    }).ToList(),
                    lastUpdateDatetime = g.Max(i => i.updateDatetime)
                })
                .OrderByDescending(i => i.lastUpdateDatetime)
                .ToList();

            var totalCount = groupedInventory.Count;
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = groupedInventory.Skip(skip).Take(request.pageSize).ToList();

            return new PagedResponse<InventoryListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<InventoryRM?> GetInventoryByIdAsync(Guid id, Guid companyId)
        {
            Console.WriteLine($"[InventoryHandler] GetInventoryByIdAsync called with id={id}, companyId={companyId}");
            
            var inventory = await _ctx.Inventory
                .AsNoTracking()
                .Include(i => i.warehouse)
                .Include(i => i.batches)
                .FirstOrDefaultAsync(i => i.id == id && i.companyId == companyId);

            if (inventory == null)
            {
                Console.WriteLine($"[InventoryHandler] Inventory not found for id={id}");
                return null;
            }

            Console.WriteLine($"[InventoryHandler] Found inventory: {inventory.title}, batches count: {inventory.batches?.Count ?? 0}");

            // Auto-create batch for legacy inventory items that don't have batches
            if ((inventory.batches == null || !inventory.batches.Any()) && inventory.quantity > 0)
            {
                Console.WriteLine($"[InventoryHandler] Creating retroactive batch for legacy inventory item");
                var now = DateTime.UtcNow;
                var batch = new InventoryBatch
                {
                    id = Guid.NewGuid(),
                    inventoryId = inventory.id,
                    companyId = inventory.companyId,
                    quantityAdded = inventory.quantity,
                    quantityRemaining = inventory.quantity,
                    addedAt = inventory.createDatetime,
                    expiresAt = inventory.createDatetime.AddDays(30),
                    isExpired = inventory.createDatetime.AddDays(30) <= now,
                    createDatetime = now,
                    updateDatetime = now
                };
                _ctx.InventoryBatch.Add(batch);
                await _ctx.SaveChangesAsync();
                
                // Reload inventory with the new batch
                inventory = await _ctx.Inventory
                    .AsNoTracking()
                    .Include(i => i.warehouse)
                    .Include(i => i.batches)
                    .FirstOrDefaultAsync(i => i.id == id && i.companyId == companyId);
                    
                Console.WriteLine($"[InventoryHandler] Batch created, new count: {inventory?.batches?.Count ?? 0}");
            }

            // CRITICAL FIX: Without this check, warehouse staff cannot create/move inventory.
            // If inventory reload returns null (e.g., deleted by another process), app crashes.
            if (inventory == null)
                throw new InvalidOperationException($"Inventory item {id} not found after batch creation");

            var result = _mapper.Map<Inventory, InventoryRM>(inventory!);
            result.warehouseId = inventory!.warehouseId;
            if (inventory.warehouse != null)
            {
                result.warehouse = new WarehouseBasicInfo
                {
                    id = inventory.warehouse.id,
                    name = inventory.warehouse.name,
                    location = inventory.warehouse.location
                };
            }

            // Map batches with expiration info (Task 1.2, 1.3)
            if (inventory.batches != null && inventory.batches.Any())
            {
                Console.WriteLine($"[InventoryHandler] Mapping {inventory.batches.Count} batches");
                var now = DateTime.UtcNow;
                result.batches = inventory.batches
                    .Where(b => b.quantityRemaining > 0)
                    .OrderBy(b => b.expiresAt)
                    .Select(b => new InventoryBatchRM
                    {
                        id = b.id,
                        inventoryId = b.inventoryId,
                        quantityAdded = b.quantityAdded,
                        quantityRemaining = b.quantityRemaining,
                        addedAt = b.addedAt,
                        expiresAt = b.expiresAt,
                        isExpired = b.expiresAt <= now,
                        daysUntilExpiration = (int)(b.expiresAt - now).TotalDays
                    })
                    .ToList();
                Console.WriteLine($"[InventoryHandler] Mapped batches count: {result.batches.Count}");
            }
            else
            {
                Console.WriteLine($"[InventoryHandler] No batches to map (batches is null or empty)");
                result.batches = new List<InventoryBatchRM>();
            }

            return result;
        }

        public async Task<InventoryRM> CreateInventoryAsync(CreateInventoryVM vm, Guid companyId)
        {
            // Validate warehouse exists and belongs to company
            var warehouse = await _ctx.Warehouse
                .FirstOrDefaultAsync(w => w.id == vm.warehouseId && w.companyId == companyId);

            if (warehouse == null)
                throw new InvalidOperationException("Warehouse not found or does not belong to this company");

            // If barcode is provided, ensure product exists (create if necessary)
            Guid productId = vm.productId;
            
            if (!string.IsNullOrWhiteSpace(vm.barcode))
            {
                var product = await _productHandler.GetOrCreateProductByBarcodeAsync(
                    vm.barcode,
                    vm.title,
                    vm.description ?? string.Empty,
                    vm.price
                );
                productId = product.productId;
            }

            var inventoryNow = DateTime.UtcNow;

            var inventory = new Inventory
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                productId = productId,
                warehouseId = vm.warehouseId,
                quantity = vm.quantity,
                price = vm.price,
                title = vm.title,
                description = vm.description ?? string.Empty,
                barcode = vm.barcode ?? string.Empty,
                source = vm.source ?? string.Empty,
                category = vm.category,
                imageS3Key = vm.imageS3Key,
                createDatetime = inventoryNow,
                updateDatetime = inventoryNow
            };

            _ctx.Inventory.Add(inventory);

            // Create initial batch with 30-day expiration (Task 1.1)
            var batch = new InventoryBatch
            {
                id = Guid.NewGuid(),
                inventoryId = inventory.id,
                companyId = companyId,
                quantityAdded = vm.quantity,
                quantityRemaining = vm.quantity,
                addedAt = inventoryNow,
                expiresAt = inventoryNow.AddDays(30),
                isExpired = false,
                createDatetime = inventoryNow,
                updateDatetime = inventoryNow
            };

            _ctx.InventoryBatch.Add(batch);
            await _ctx.SaveChangesAsync();

            // Reload with warehouse and batches
            var created = await _ctx.Inventory
                .Include(i => i.warehouse)
                .Include(i => i.batches)
                .FirstOrDefaultAsync(i => i.id == inventory.id);

            // CRITICAL FIX: Without this check, warehouse staff cannot create/move inventory.
            // If inventory reload returns null, the app crashes and work stops.
            if (created == null)
                throw new InvalidOperationException($"Inventory item {inventory.id} not found after save");

            var result = _mapper.Map<Inventory, InventoryRM>(created!);
            result.warehouseId = created!.warehouseId;
            if (created.warehouse != null)
            {
                result.warehouse = new WarehouseBasicInfo
                {
                    id = created.warehouse.id,
                    name = created.warehouse.name,
                    location = created.warehouse.location
                };
            }

            // Map batches with expiration info
            if (created.batches != null && created.batches.Any())
            {
                var now = DateTime.UtcNow;
                result.batches = created.batches
                    .Where(b => b.quantityRemaining > 0)
                    .OrderBy(b => b.expiresAt)
                    .Select(b => new InventoryBatchRM
                    {
                        id = b.id,
                        inventoryId = b.inventoryId,
                        quantityAdded = b.quantityAdded,
                        quantityRemaining = b.quantityRemaining,
                        addedAt = b.addedAt,
                        expiresAt = b.expiresAt,
                        isExpired = b.expiresAt <= now,
                        daysUntilExpiration = (int)(b.expiresAt - now).TotalDays
                    })
                    .ToList();
            }

            return result;
        }

        public async Task<InventoryRM?> UpdateInventoryAsync(UpdateInventoryVM vm, Guid companyId)
        {
            var inventory = await _ctx.Inventory
                .Include(i => i.warehouse)
                .FirstOrDefaultAsync(i => i.id == vm.id && i.companyId == companyId);

            if (inventory == null)
                return null;

            // If barcode is being updated and is not empty, ensure product exists (create if necessary)
            if (vm.barcode != null && !string.IsNullOrWhiteSpace(vm.barcode))
            {
                // Get the updated values, using existing values as fallback
                var title = !string.IsNullOrEmpty(vm.title) ? vm.title : inventory.title;
                var description = vm.description != null ? vm.description : inventory.description;
                var price = vm.price ?? inventory.price;

                var product = await _productHandler.GetOrCreateProductByBarcodeAsync(
                    vm.barcode,
                    title,
                    description,
                    price
                );
                
                inventory.productId = product.productId;
                inventory.barcode = vm.barcode;
            }

            if (vm.quantity.HasValue)
            {
                var oldQuantity = inventory.quantity;
                var newQuantity = vm.quantity.Value;
                inventory.quantity = newQuantity;

                // If quantity increased, create a new batch (Task 1.1, 1.2)
                if (newQuantity > oldQuantity)
                {
                    var quantityAdded = newQuantity - oldQuantity;
                    var now = DateTime.UtcNow;
                    var newBatch = new InventoryBatch
                    {
                        id = Guid.NewGuid(),
                        inventoryId = inventory.id,
                        companyId = companyId,
                        quantityAdded = quantityAdded,
                        quantityRemaining = quantityAdded,
                        addedAt = now,
                        expiresAt = now.AddDays(30),
                        isExpired = false,
                        createDatetime = now,
                        updateDatetime = now
                    };
                    _ctx.InventoryBatch.Add(newBatch);
                }
            }
            if (vm.price.HasValue)
                inventory.price = vm.price.Value;
            if (!string.IsNullOrEmpty(vm.title))
                inventory.title = vm.title;
            if (vm.description != null)
                inventory.description = vm.description;
            if (vm.category != null)
                inventory.category = vm.category;
            if (vm.imageS3Key != null)
                inventory.imageS3Key = vm.imageS3Key;

            inventory.updateDatetime = DateTime.UtcNow;

            _ctx.Inventory.Update(inventory);
            await _ctx.SaveChangesAsync();

            var result = _mapper.Map<Inventory, InventoryRM>(inventory);
            result.warehouseId = inventory.warehouseId;
            if (inventory.warehouse != null)
            {
                result.warehouse = new WarehouseBasicInfo
                {
                    id = inventory.warehouse.id,
                    name = inventory.warehouse.name,
                    location = inventory.warehouse.location
                };
            }

            return result;
        }

        /// <summary>
        /// Deducts quantity from inventory using FIFO (First In, First Out) batch consumption (Task 1.4)
        /// </summary>
        public async Task<bool> DeductInventoryQuantityAsync(Guid inventoryId, decimal quantityToDeduct, Guid companyId)
        {
            var inventory = await _ctx.Inventory
                .Include(i => i.batches)
                .FirstOrDefaultAsync(i => i.id == inventoryId && i.companyId == companyId);

            if (inventory == null)
                return false;

            if (inventory.quantity < quantityToDeduct)
                throw new InvalidOperationException("Insufficient inventory quantity");

            var remainingToDeduct = quantityToDeduct;
            var now = DateTime.UtcNow;

            // Get batches ordered by expiration date (FIFO - oldest first)
            var batches = inventory.batches?
                .Where(b => b.quantityRemaining > 0)
                .OrderBy(b => b.expiresAt)
                .ThenBy(b => b.addedAt)
                .ToList();

            if (batches == null || !batches.Any())
                throw new InvalidOperationException("No batches available for deduction");

            // Deduct from oldest batches first
            foreach (var batch in batches)
            {
                if (remainingToDeduct <= 0)
                    break;

                var deductFromBatch = Math.Min(batch.quantityRemaining, remainingToDeduct);
                batch.quantityRemaining -= deductFromBatch;
                batch.updateDatetime = now;

                remainingToDeduct -= deductFromBatch;

                _ctx.InventoryBatch.Update(batch);
            }

            // Update total inventory quantity
            inventory.quantity -= quantityToDeduct;
            inventory.updateDatetime = now;
            _ctx.Inventory.Update(inventory);

            await _ctx.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteInventoryAsync(Guid id, Guid companyId)
        {
            var inventory = await _ctx.Inventory
                .FirstOrDefaultAsync(i => i.id == id && i.companyId == companyId);

            if (inventory == null)
                return false;

            _ctx.Inventory.Remove(inventory);
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Add quantity to an existing inventory item by creating a new batch (supports FIFO tracking)
        /// </summary>
        public async Task<InventoryRM> AddQuantityAsync(AddQuantityVM vm, Guid companyId, Guid userId)
        {
            var inventory = await _ctx.Inventory
                .Include(i => i.warehouse)
                .Include(i => i.batches)
                .FirstOrDefaultAsync(i => i.id == vm.inventoryId && i.companyId == companyId);

            if (inventory == null)
                throw new InvalidOperationException("Inventory item not found");

            var now = DateTime.UtcNow;
            var returnDays = vm.returnDays ?? 30;

            // Create new batch for the added quantity
            var newBatch = new InventoryBatch
            {
                id = Guid.NewGuid(),
                inventoryId = inventory.id,
                companyId = companyId,
                quantityAdded = vm.quantity,
                quantityRemaining = vm.quantity,
                addedAt = now,
                expiresAt = now.AddDays(returnDays),
                isExpired = false,
                createDatetime = now,
                updateDatetime = now
            };
            _ctx.InventoryBatch.Add(newBatch);

            // Update total inventory quantity
            var previousQuantity = inventory.quantity;
            inventory.quantity += vm.quantity;
            inventory.updateDatetime = now;
            _ctx.Inventory.Update(inventory);

            // Record usage history for traceability
            var history = new InventoryUsageHistory
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                inventoryId = inventory.id,
                jobId = null,
                actionType = "added",
                quantity = vm.quantity,
                previousQuantity = previousQuantity,
                newQuantity = inventory.quantity,
                notes = vm.notes ?? "Quantity added via manual entry",
                performedByUserId = userId,
                createDatetime = now
            };
            _ctx.InventoryUsageHistory.Add(history);

            await _ctx.SaveChangesAsync();

            // Return updated inventory with batches
            var result = _mapper.Map<Inventory, InventoryRM>(inventory);
            result.warehouseId = inventory.warehouseId;
            if (inventory.warehouse != null)
            {
                result.warehouse = new WarehouseBasicInfo
                {
                    id = inventory.warehouse.id,
                    name = inventory.warehouse.name,
                    location = inventory.warehouse.location
                };
            }

            // Map batches
            if (inventory.batches != null && inventory.batches.Any())
            {
                result.batches = inventory.batches
                    .Where(b => b.quantityRemaining > 0)
                    .OrderBy(b => b.expiresAt)
                    .Select(b => new InventoryBatchRM
                    {
                        id = b.id,
                        inventoryId = b.inventoryId,
                        quantityAdded = b.quantityAdded,
                        quantityRemaining = b.quantityRemaining,
                        addedAt = b.addedAt,
                        expiresAt = b.expiresAt,
                        isExpired = b.expiresAt <= now,
                        daysUntilExpiration = (int)(b.expiresAt - now).TotalDays
                    })
                    .ToList();
            }

            return result;
        }

        public async Task<InventoryRM?> MoveInventoryAsync(MoveInventoryVM vm, Guid companyId)
        {
            // Get source inventory
            var sourceInventory = await _ctx.Inventory
                .Include(i => i.warehouse)
                .FirstOrDefaultAsync(i => i.id == vm.inventoryId && i.companyId == companyId);

            if (sourceInventory == null)
                throw new InvalidOperationException("Source inventory not found");

            if (sourceInventory.warehouseId != vm.sourceWarehouseId)
                throw new InvalidOperationException("Inventory is not in the specified source warehouse");

            if (sourceInventory.quantity < vm.quantityToMove)
                throw new InvalidOperationException("Insufficient quantity in source warehouse");

            // Validate target warehouse exists and belongs to company
            var targetWarehouse = await _ctx.Warehouse
                .FirstOrDefaultAsync(w => w.id == vm.targetWarehouseId && w.companyId == companyId);

            if (targetWarehouse == null)
                throw new InvalidOperationException("Target warehouse not found or does not belong to this company");

            var now = DateTime.UtcNow;

            // Check if there's already inventory for this product in target warehouse
            var targetInventory = await _ctx.Inventory
                .Include(i => i.warehouse)
                .FirstOrDefaultAsync(i => i.productId == sourceInventory.productId 
                    && i.warehouseId == vm.targetWarehouseId 
                    && i.companyId == companyId);

            if (targetInventory != null)
            {
                // Add to existing inventory
                targetInventory.quantity += vm.quantityToMove;
                targetInventory.updateDatetime = now;
                _ctx.Inventory.Update(targetInventory);
            }
            else
            {
                // Create new inventory entry in target warehouse
                targetInventory = new Inventory
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    productId = sourceInventory.productId,
                    warehouseId = vm.targetWarehouseId,
                    quantity = vm.quantityToMove,
                    price = sourceInventory.price,
                    title = sourceInventory.title,
                    description = sourceInventory.description,
                    barcode = sourceInventory.barcode,
                    createDatetime = now,
                    updateDatetime = now
                };
                _ctx.Inventory.Add(targetInventory);
            }

            // Reduce quantity from source or delete if zero
            if (sourceInventory.quantity == vm.quantityToMove)
            {
                _ctx.Inventory.Remove(sourceInventory);
            }
            else
            {
                sourceInventory.quantity -= vm.quantityToMove;
                sourceInventory.updateDatetime = now;
                _ctx.Inventory.Update(sourceInventory);
            }

            await _ctx.SaveChangesAsync();

            // Reload target inventory with warehouse details
            var result = await _ctx.Inventory
                .Include(i => i.warehouse)
                .FirstOrDefaultAsync(i => i.id == targetInventory.id);

            var response = _mapper.Map<Inventory, InventoryRM>(result!);
            response.warehouseId = result!.warehouseId;
            if (result.warehouse != null)
            {
                response.warehouse = new WarehouseBasicInfo
                {
                    id = result.warehouse.id,
                    name = result.warehouse.name,
                    location = result.warehouse.location
                };
            }

            return response;
        }

        /// <summary>
        /// Record inventory usage (reduce quantity) when completing a job using FIFO batch consumption
        /// </summary>
        public async Task<InventoryUsageHistoryRM> RecordInventoryUsageAsync(RecordInventoryUsageVM vm, Guid companyId, Guid userId)
        {
            var inventory = await _ctx.Inventory
                .Include(i => i.batches)
                .FirstOrDefaultAsync(i => i.id == vm.inventoryId && i.companyId == companyId);

            if (inventory == null)
                throw new InvalidOperationException("Inventory item not found");

            var previousQuantity = inventory.quantity;
            var quantityChange = vm.quantity;
            var now = DateTime.UtcNow;
            
            // For "used" action, subtract from inventory using FIFO
            if (vm.actionType == "used")
            {
                // Deduct from batches using FIFO (oldest first by expiresAt)
                var batches = inventory.batches?
                    .Where(b => b.quantityRemaining > 0)
                    .OrderBy(b => b.expiresAt)
                    .ThenBy(b => b.addedAt)
                    .ToList();

                if (batches != null && batches.Any())
                {
                    var remainingToDeduct = quantityChange;
                    foreach (var batch in batches)
                    {
                        if (remainingToDeduct <= 0)
                            break;

                        var deductFromBatch = Math.Min(batch.quantityRemaining, remainingToDeduct);
                        batch.quantityRemaining -= deductFromBatch;
                        batch.updateDatetime = now;
                        remainingToDeduct -= deductFromBatch;
                        _ctx.InventoryBatch.Update(batch);
                    }
                }

                inventory.quantity = Math.Max(0, inventory.quantity - quantityChange);
            }
            // For "returned" action, create a new batch with the returned quantity
            else if (vm.actionType == "returned")
            {
                inventory.quantity += quantityChange;
                
                // Create a new batch for returned items with 30-day expiration
                var newBatch = new InventoryBatch
                {
                    id = Guid.NewGuid(),
                    inventoryId = inventory.id,
                    companyId = companyId,
                    quantityAdded = quantityChange,
                    quantityRemaining = quantityChange,
                    addedAt = now,
                    expiresAt = now.AddDays(30),
                    isExpired = false,
                    createDatetime = now,
                    updateDatetime = now
                };
                _ctx.InventoryBatch.Add(newBatch);
            }

            inventory.updateDatetime = now;

            // Create history record
            var history = new InventoryUsageHistory
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                inventoryId = vm.inventoryId,
                jobId = vm.jobId,
                actionType = vm.actionType,
                quantity = quantityChange,
                previousQuantity = previousQuantity,
                newQuantity = inventory.quantity,
                notes = vm.notes,
                performedByUserId = userId,
                createDatetime = DateTime.UtcNow
            };

            _ctx.InventoryUsageHistory.Add(history);
            await _ctx.SaveChangesAsync();

            return new InventoryUsageHistoryRM
            {
                id = history.id,
                inventoryId = history.inventoryId,
                jobId = history.jobId,
                actionType = history.actionType,
                quantity = history.quantity,
                previousQuantity = history.previousQuantity,
                newQuantity = history.newQuantity,
                notes = history.notes,
                createDatetime = history.createDatetime
            };
        }

        /// <summary>
        /// Get usage history for an inventory item
        /// </summary>
        public async Task<List<InventoryUsageHistoryRM>> GetInventoryUsageHistoryAsync(Guid inventoryId, Guid companyId)
        {
            var history = await _ctx.InventoryUsageHistory
                .AsNoTracking()
                .Include(h => h.job)
                .Include(h => h.performedByUser)
                .Where(h => h.inventoryId == inventoryId && h.companyId == companyId)
                .OrderByDescending(h => h.createDatetime)
                .ToListAsync();

            return history.Select(h => new InventoryUsageHistoryRM
            {
                id = h.id,
                inventoryId = h.inventoryId,
                jobId = h.jobId,
                jobTitle = h.job?.title,
                actionType = h.actionType,
                quantity = h.quantity,
                previousQuantity = h.previousQuantity,
                newQuantity = h.newQuantity,
                notes = h.notes,
                performedByUserName = h.performedByUser?.fullName,
                createDatetime = h.createDatetime
            }).ToList();
        }

        /// <summary>
        /// Get inventory items that are allocated to active jobs.
        /// Returns each inventory item with the jobs it's allocated to and quantities.
        /// </summary>
        public async Task<object> GetAllocatedInventoryAsync(Guid companyId)
        {
            var allocations = await _ctx.JobInventory
                .AsNoTracking()
                .Include(ji => ji.job)
                .Where(ji => ji.companyId == companyId
                    && ji.job != null
                    && ji.job.status != "cancelled"
                    && ji.job.status != "completed"
                    && (ji.inventoryId.HasValue || ji.productId.HasValue))
                .Select(ji => new
                {
                    itemTitle = ji.title,
                    inventoryId = ji.inventoryId,
                    productId = ji.productId,
                    allocatedQuantity = ji.idealQuantity,
                    jobId = ji.jobId,
                    jobTitle = ji.job!.title,
                    jobNumber = ji.job.jobNumber,
                    jobStatus = ji.job.status
                })
                .ToListAsync();

            // Group by inventory item
            var grouped = allocations
                .GroupBy(a => a.itemTitle)
                .Select(g => new
                {
                    itemTitle = g.Key,
                    totalAllocated = g.Sum(a => a.allocatedQuantity),
                    jobs = g.Select(a => new
                    {
                        a.jobId,
                        a.jobTitle,
                        a.jobNumber,
                        a.jobStatus,
                        a.allocatedQuantity
                    }).ToList()
                })
                .OrderByDescending(g => g.totalAllocated)
                .ToList();

            return new
            {
                totalAllocatedItems = grouped.Count,
                items = grouped
            };
        }

        /// Get inventory statistics including Amount Saved metric.
        /// Amount Saved = value of inventory items used before expiration in completed jobs.
        /// </summary>
        public async Task<InventoryStatsRM> GetInventoryStatsAsync(Guid companyId)
        {
            // Get total inventory value
            var totalValue = await _ctx.Inventory
                .AsNoTracking()
                .Where(i => i.companyId == companyId)
                .SumAsync(i => i.quantity * i.price);

            var totalItems = await _ctx.Inventory
                .AsNoTracking()
                .Where(i => i.companyId == companyId)
                .CountAsync();

            // Calculate Amount Saved:
            // Value of inventory used before expiration date in completed jobs
            // Join InventoryUsageHistory with Inventory and InventoryBatch
            // where actionType = "used", job is completed, and usage date < batch expiration
            var amountSaved = await (
                from usage in _ctx.InventoryUsageHistory.AsNoTracking()
                join inventory in _ctx.Inventory.AsNoTracking() on usage.inventoryId equals inventory.id
                join job in _ctx.Job.AsNoTracking() on usage.jobId equals job.id
                join batch in _ctx.InventoryBatch.AsNoTracking() on usage.inventoryId equals batch.inventoryId
                where usage.companyId == companyId
                    && usage.actionType == "used"
                    && job.status == "completed"
                    && usage.createDatetime < batch.expiresAt
                select new { usage.quantity, inventory.price }
            ).Distinct().SumAsync(x => x.quantity * x.price);

            return new InventoryStatsRM
            {
                TotalValue = totalValue,
                TotalItems = totalItems,
                AmountSaved = amountSaved
            };
        }
    }
}
