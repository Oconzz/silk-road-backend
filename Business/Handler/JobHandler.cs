using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.Services;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class JobHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;
        private readonly IS3PresignedService? _s3PresignedService;

        public JobHandler(DBContext ctx, IMapper mapper, IS3PresignedService? s3PresignedService = null)
        {
            _ctx = ctx;
            _mapper = mapper;
            _s3PresignedService = s3PresignedService;
        }

        public async Task<PagedResponse<JobListItem>> GetJobListAsync(JobListRequestVM request, Guid companyId)
        {
            if (request == null) request = new JobListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Job> query = _ctx.Job
                .AsNoTracking()
                .Include(j => j.client)
                .Include(j => j.location)
                .Include(j => j.estimate)
                .Where(j => j.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(j =>
                    (j.title != null && j.title.ToLower().Contains(term)) ||
                    (j.description != null && j.description.ToLower().Contains(term)) ||
                    (j.status != null && j.status.ToLower().Contains(term))
                );
            }

            // Filter by status
            if (!string.IsNullOrWhiteSpace(request.status))
            {
                query = query.Where(j => j.status == request.status);
            }

            // Filter by client
            if (request.clientId.HasValue)
            {
                query = query.Where(j => j.client != null && j.client.id == request.clientId.Value);
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(j => j.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(j => new JobListItem
                {
                    id = j.id,
                    companyId = j.companyId,
                    title = j.title,
                    jobNumber = j.jobNumber,
                    status = j.status,
                    clientId = j.client != null ? j.client.id : (Guid?)null,
                    clientName = j.client != null 
                        ? (!string.IsNullOrEmpty(j.client.companyName) ? j.client.companyName : j.client.contactName)
                        : string.Empty,
                    locationCity = j.location != null ? j.location.city : string.Empty,
                    scheduledStartDate = j.scheduledStartDate,
                    estimatedTotal = j.estimatedTotal,
                    estimateId = j.estimateId,
                    estimateTitle = j.estimate != null ? j.estimate.name : null,
                    isRecurring = j.isRecurring,
                    parentJobId = j.parentJobId,
                    occurrenceDate = j.occurrenceDate,
                    childJobsCount = _ctx.Job.Count(child => child.parentJobId == j.id),
                    createDatetime = j.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<JobListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<JobRM?> GetJobByIdAsync(Guid id, Guid companyId)
        {
            var job = await _ctx.Job
                .AsNoTracking()
                .Include(j => j.client)
                .Include(j => j.location)
                .Include(j => j.estimate)
                .Include(j => j.parentJob)
                .Include(j => j.jobInventory)
                    .ThenInclude(ji => ji.warehouse)
                .FirstOrDefaultAsync(j => j.id == id && j.companyId == companyId);

            if (job == null)
                return null;

            var result = new JobRM
            {
                id = job.id,
                companyId = job.companyId,
                jobNumber = job.jobNumber,
                title = job.title,
                description = job.description,
                status = job.status,
                estimateId = job.estimateId,
                estimateTitle = job.estimate?.name,
                scheduledStartDate = job.scheduledStartDate,
                scheduledEndDate = job.scheduledEndDate,
                estimatedTotal = job.estimatedTotal,
                laborCost = job.laborCost,
                materialsCost = job.materialsCost,
                isRecurring = job.isRecurring,
                recurrenceRule = job.recurrenceRule,
                recurrenceEndDate = job.recurrenceEndDate,
                timeZone = job.timeZone,
                lastOccurrenceCreated = job.lastOccurrenceCreated,
                billingFrequency = job.billingFrequency,
                parentJobId = job.parentJobId,
                parentJobTitle = job.parentJob?.title,
                occurrenceDate = job.occurrenceDate,
                createDatetime = job.createDatetime,
                updateDatetime = job.updateDatetime
            };

            // Count child jobs for recurring parent
            result.childJobsCount = await _ctx.Job
                .CountAsync(c => c.parentJobId == id && c.companyId == companyId);

            // Map client
            if (job.client != null)
            {
                result.client = new ClientListItem
                {
                    id = job.client.id,
                    companyId = job.client.companyId,
                    contactName = job.client.contactName,
                    phone = job.client.phone,
                    email = job.client.email,
                    companyName = job.client.companyName,
                    createDatetime = job.client.createDatetime
                };
            }

            // Map location
            if (job.location != null)
            {
                result.location = _mapper.Map<Location, LocationRM>(job.location);
            }

            // Map job inventory - always return a list (empty or with items)
            result.jobInventory = (job.jobInventory ?? new List<JobInventory>()).Select(ji =>
            {
                var itemRM = _mapper.Map<JobInventory, JobInventoryRM>(ji);
                if (ji.warehouse != null)
                {
                    itemRM.warehouse = new WarehouseBasicInfo
                    {
                        id = ji.warehouse.id,
                        name = ji.warehouse.name,
                        location = ji.warehouse.location
                    };
                }
                return itemRM;
            }).ToList();

            // Load media for this job
            var media = await _ctx.JobMedia
                .AsNoTracking()
                .Where(m => m.jobId == id && m.companyId == companyId)
                .OrderBy(m => m.orderIndex)
                .ToListAsync();

            // Load albums for this job
            var albums = await _ctx.JobAlbums
                .AsNoTracking()
                .Where(a => a.jobId == id && a.companyId == companyId)
                .OrderBy(a => a.orderIndex)
                .ToListAsync();

            // Map media - generate presigned URLs for private bucket access
            if (media != null && media.Any())
            {
                var mediaTasks = media.Select(async m =>
                {
                    // Find the inventory item title for this media
                    string? inventoryItemTitle = null;
                    if (m.jobInventoryId.HasValue && job.jobInventory != null)
                    {
                        inventoryItemTitle = job.jobInventory.FirstOrDefault(ji => ji.id == m.jobInventoryId)?.title;
                    }

                    var mediaRM = new JobMediaRM
                    {
                        id = m.id,
                        jobId = m.jobId,
                        companyId = m.companyId,
                        albumId = m.albumId,
                        jobInventoryId = m.jobInventoryId,
                        jobInventoryTitle = inventoryItemTitle,
                        webUrl = m.webUrl,
                        thumbUrl = m.thumbUrl,
                        caption = m.caption,
                        type = m.type,
                        category = m.category,
                        orderIndex = m.orderIndex,
                        createDatetime = m.createDatetime,
                        updateDatetime = m.updateDatetime
                    };

                    // Generate presigned URLs if S3 service is available
                    if (_s3PresignedService != null && !string.IsNullOrEmpty(m.webUrl))
                    {
                        var s3Key = ExtractS3KeyFromUrl(m.webUrl);
                        if (!string.IsNullOrEmpty(s3Key))
                        {
                            try
                            {
                                var (webPresigned, thumbPresigned) = await _s3PresignedService.GeneratePresignedDownloadUrls(s3Key);
                                mediaRM.webUrl = webPresigned;
                                mediaRM.thumbUrl = thumbPresigned;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[JobHandler] Error generating presigned URL for {s3Key}: {ex.Message}");
                            }
                        }
                    }

                    return mediaRM;
                });

                result.media = (await Task.WhenAll(mediaTasks)).ToList();
            }

            // Map albums
            if (albums != null && albums.Any())
            {
                result.albums = albums.Select(a => new JobAlbumRM
                {
                    id = a.id,
                    jobId = a.jobId,
                    companyId = a.companyId,
                    name = a.name,
                    description = a.description,
                    orderIndex = a.orderIndex,
                    mediaCount = media?.Count(m => m.albumId == a.id) ?? 0,
                    media = result.media?.Where(m => m.albumId == a.id).ToList() ?? new List<JobMediaRM>(),
                    createDatetime = a.createDatetime,
                    updateDatetime = a.updateDatetime
                }).ToList();
            }

            // Load assignments for this job
            var assignments = await _ctx.JobAssignments
                .AsNoTracking()
                .Include(ja => ja.user)
                .Where(ja => ja.jobId == id && ja.companyId == companyId && ja.active)
                .OrderBy(ja => ja.assignedDatetime)
                .ToListAsync();

            if (assignments != null && assignments.Any())
            {
                result.assignments = assignments.Select(ja => new AssignedTeamMemberRM
                {
                    userId = ja.userId,
                    fullName = ja.user?.fullName ?? string.Empty,
                    email = ja.user?.email ?? string.Empty,
                    photoThumbnail = ja.user?.personalPhotoThumbnail,
                    role = ja.role,
                    assignedDatetime = ja.assignedDatetime
                }).ToList();
            }

            // Load notes for this job
            var notes = await _ctx.JobNotes
                .AsNoTracking()
                .Include(n => n.creator)
                .Where(n => n.jobId == id && n.companyId == companyId)
                .OrderByDescending(n => n.createDatetime)
                .ToListAsync();

            if (notes != null && notes.Any())
            {
                result.notes = notes.Select(n => new JobNoteListItem
                {
                    id = n.id,
                    jobId = n.jobId,
                    createdByName = n.creator?.fullName ?? "Unknown",
                    content = n.content,
                    type = n.type,
                    createDatetime = n.createDatetime
                }).ToList();
            }

            return result;
        }

        public async Task<JobRM> CreateJobAsync(CreateJobVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            // Calculate financials from jobInventory if not provided
            decimal materialsCost = vm.materialsCost ?? 0;
            decimal laborCost = vm.laborCost ?? 0;
            
            if (vm.jobInventory != null && vm.jobInventory.Any())
            {
                // Only calculate if not explicitly provided
                if (!vm.materialsCost.HasValue)
                {
                    materialsCost = vm.jobInventory.Sum(i => i.price * i.idealQuantity);
                }
                if (!vm.laborCost.HasValue)
                {
                    laborCost = vm.jobInventory.Sum(i => i.labor * i.idealQuantity);
                }
            }
            
            var estimatedTotal = vm.estimatedTotal ?? (materialsCost + laborCost);

            // Generate job number (JOB-XXXXXX format)
            var lastJobNumber = await _ctx.Job
                .Where(j => j.companyId == companyId && j.jobNumber != null)
                .OrderByDescending(j => j.createDatetime)
                .Select(j => j.jobNumber)
                .FirstOrDefaultAsync();
            
            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastJobNumber) && lastJobNumber.StartsWith("JOB-"))
            {
                if (int.TryParse(lastJobNumber.Substring(4), out int lastNum))
                {
                    nextNumber = lastNum + 1;
                }
            }
            var jobNumber = $"JOB-{nextNumber:D6}";

            var job = new Job
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                jobNumber = jobNumber,
                title = vm.title,
                description = vm.description,
                status = vm.status,
                estimateId = vm.estimateId,
                scheduledStartDate = vm.scheduledStartDate,
                scheduledEndDate = vm.scheduledEndDate,
                isRecurring = vm.isRecurring,
                recurrenceRule = vm.recurrenceRule,
                recurrenceEndDate = vm.recurrenceEndDate,
                timeZone = vm.timeZone,
                billingFrequency = vm.billingFrequency,
                estimatedTotal = estimatedTotal,
                laborCost = laborCost,
                materialsCost = materialsCost,
                createDatetime = now,
                updateDatetime = now
            };

            // Link client if provided
            if (vm.clientId.HasValue)
            {
                var client = await _ctx.Client.FindAsync(vm.clientId.Value);
                if (client != null && client.companyId == companyId)
                {
                    job.client = client;
                }
            }

            // Link location if provided
            if (vm.locationId.HasValue)
            {
                var location = await _ctx.Location.FindAsync(vm.locationId.Value);
                if (location != null && location.companyId == companyId)
                {
                    job.location = location;
                }
            }

            // Create job inventory items if provided
            if (vm.jobInventory != null && vm.jobInventory.Any())
            {
                job.jobInventory = vm.jobInventory.Select(jiVM => new JobInventory
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    jobId = job.id,
                    productId = jiVM.productId,
                    warehouseId = jiVM.warehouseId,
                    inventoryId = jiVM.inventoryId,
                    idealQuantity = jiVM.idealQuantity,
                    actualQuantity = jiVM.actualQuantity,
                    price = jiVM.price,
                    labor = jiVM.labor,
                    title = jiVM.title,
                    description = jiVM.description ?? string.Empty,
                    barcode = jiVM.barcode ?? string.Empty,
                    source = jiVM.source ?? string.Empty,
                    createDatetime = now,
                    updateDatetime = now
                }).ToList();
            }

            _ctx.Job.Add(job);
            await _ctx.SaveChangesAsync();

            // Copy estimate media to job if created from an estimate
            if (vm.estimateId.HasValue)
            {
                var estimateMedia = await _ctx.EstimateMedia
                    .AsNoTracking()
                    .Where(m => m.estimateId == vm.estimateId.Value && m.companyId == companyId)
                    .ToListAsync();

                // Build mapping from estimate item ID → job inventory ID
                // Uses the estimate items to find which job inventory item corresponds
                Dictionary<Guid, Guid>? estimateItemToJobInventoryMap = null;
                if (job.jobInventory != null && job.jobInventory.Any())
                {
                    var estimateItems = await _ctx.EstimateItem
                        .AsNoTracking()
                        .Where(ei => ei.estimateId == vm.estimateId.Value)
                        .OrderBy(ei => ei.createDatetime)
                        .ToListAsync();

                    if (estimateItems.Any())
                    {
                        estimateItemToJobInventoryMap = new Dictionary<Guid, Guid>();
                        // Match by title+description (same items in same order)
                        foreach (var ei in estimateItems)
                        {
                            var matchingJi = job.jobInventory.FirstOrDefault(ji =>
                                ji.title == ei.title && ji.description == ei.description);
                            if (matchingJi != null)
                            {
                                estimateItemToJobInventoryMap[ei.id] = matchingJi.id;
                            }
                        }
                    }
                }

                if (estimateMedia.Any())
                {
                    var jobMediaList = estimateMedia.Select(em =>
                    {
                        // Map estimateItemId to jobInventoryId
                        Guid? jobInventoryId = null;
                        if (em.estimateItemId.HasValue && estimateItemToJobInventoryMap != null)
                        {
                            estimateItemToJobInventoryMap.TryGetValue(em.estimateItemId.Value, out var mappedId);
                            if (mappedId != Guid.Empty) jobInventoryId = mappedId;
                        }

                        return new JobMedia
                        {
                            id = Guid.NewGuid(),
                            jobId = job.id,
                            companyId = companyId,
                            jobInventoryId = jobInventoryId,
                            webUrl = em.webUrl,
                            thumbUrl = em.thumbUrl,
                            caption = em.caption,
                            type = em.type,
                            category = "general",
                            orderIndex = em.orderIndex,
                            createDatetime = now,
                            updateDatetime = now
                        };
                    }).ToList();

                    _ctx.JobMedia.AddRange(jobMediaList);
                    await _ctx.SaveChangesAsync();
                }
            }

            // Reload with relationships
            return await GetJobByIdAsync(job.id, companyId) 
                ?? throw new InvalidOperationException("Failed to retrieve created job");
        }

        public async Task<JobRM?> UpdateJobAsync(UpdateJobVM vm, Guid companyId)
        {
            var job = await _ctx.Job
                .Include(j => j.client)
                .Include(j => j.location)
                .Include(j => j.jobInventory)
                .FirstOrDefaultAsync(j => j.id == vm.id && j.companyId == companyId);

            if (job == null)
                return null;

            if (!string.IsNullOrEmpty(vm.title))
                job.title = vm.title;
            if (vm.description != null)
                job.description = vm.description;
            if (!string.IsNullOrEmpty(vm.status))
                job.status = vm.status;
            
            if (vm.scheduledStartDate.HasValue)
                job.scheduledStartDate = vm.scheduledStartDate;
            
            if (vm.scheduledEndDate.HasValue)
                job.scheduledEndDate = vm.scheduledEndDate;

            // Update estimate link and financial fields
            if (vm.estimateId.HasValue)
                job.estimateId = vm.estimateId;
            if (vm.estimatedTotal.HasValue)
                job.estimatedTotal = vm.estimatedTotal.Value;
            if (vm.laborCost.HasValue)
                job.laborCost = vm.laborCost.Value;
            if (vm.materialsCost.HasValue)
                job.materialsCost = vm.materialsCost.Value;

            // Update recurring fields
            if (vm.isRecurring.HasValue)
                job.isRecurring = vm.isRecurring.Value;
            if (vm.recurrenceRule != null)
                job.recurrenceRule = vm.recurrenceRule;
            if (vm.recurrenceEndDate.HasValue)
                job.recurrenceEndDate = vm.recurrenceEndDate;
            if (vm.timeZone != null)
                job.timeZone = vm.timeZone;
            if (vm.billingFrequency != null)
                job.billingFrequency = vm.billingFrequency;

            // Update client relationship
            if (vm.clientId.HasValue)
            {
                var client = await _ctx.Client.FindAsync(vm.clientId.Value);
                if (client != null && client.companyId == companyId)
                {
                    job.client = client;
                }
            }

            // Update location relationship
            if (vm.locationId.HasValue)
            {
                var location = await _ctx.Location.FindAsync(vm.locationId.Value);
                if (location != null && location.companyId == companyId)
                {
                    job.location = location;
                }
            }

            // Update job inventory if provided
            if (vm.jobInventory != null)
            {
                var now = DateTime.UtcNow;
                
                // 1. Remove existing items and Save
                if (job.jobInventory != null && job.jobInventory.Any())
                {
                    _ctx.JobInventory.RemoveRange(job.jobInventory);
                    await _ctx.SaveChangesAsync(); // Commit delete
                    
                    // Clear the collection in memory to avoid tracking confusion
                    job.jobInventory.Clear();
                }
                
                // 2. Add new items - use only AddRange, don't assign to navigation property
                if (vm.jobInventory.Any())
                {
                    var newItems = vm.jobInventory.Select(jiVM => new JobInventory
                    {
                        id = Guid.NewGuid(),
                        companyId = companyId,
                        jobId = job.id,
                        productId = jiVM.productId,
                        warehouseId = jiVM.warehouseId,
                        inventoryId = jiVM.inventoryId,
                        idealQuantity = jiVM.idealQuantity,
                        actualQuantity = jiVM.actualQuantity,
                        price = jiVM.price,
                        labor = jiVM.labor,
                        title = jiVM.title,
                        description = jiVM.description ?? string.Empty,
                        barcode = jiVM.barcode ?? string.Empty,
                        source = jiVM.source ?? string.Empty,
                        createDatetime = now,
                        updateDatetime = now
                    }).ToList();

                    // Add new items directly to the DbSet - EF Core will track as Added
                    await _ctx.JobInventory.AddRangeAsync(newItems);

                    // 3. Recalculate financials from updated line items
                    job.materialsCost = newItems.Sum(i => i.price * i.idealQuantity);
                    job.laborCost = newItems.Sum(i => i.labor * i.idealQuantity);
                    job.estimatedTotal = job.materialsCost + job.laborCost;
                }
                else
                {
                    // All items removed - zero out financials
                    job.materialsCost = 0;
                    job.laborCost = 0;
                    job.estimatedTotal = 0;
                }
            }

            job.updateDatetime = DateTime.UtcNow;

            // _ctx.Job.Update(job); // Removed to avoid tracking conflict
            try
            {
                await _ctx.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Reload the entity and try again
                await _ctx.Entry(job).ReloadAsync();
                
                // Re-apply the changes
                if (!string.IsNullOrEmpty(vm.title))
                    job.title = vm.title;
                if (vm.description != null)
                    job.description = vm.description;
                if (!string.IsNullOrEmpty(vm.status))
                    job.status = vm.status;
                if (vm.scheduledStartDate.HasValue)
                    job.scheduledStartDate = vm.scheduledStartDate;
                if (vm.scheduledEndDate.HasValue)
                    job.scheduledEndDate = vm.scheduledEndDate;
                    
                job.updateDatetime = DateTime.UtcNow;
                await _ctx.SaveChangesAsync();
            }

            return await GetJobByIdAsync(job.id, companyId);
        }

        public async Task<List<JobListItem>> GetChildJobsAsync(Guid parentJobId, Guid companyId)
        {
            return await _ctx.Job
                .AsNoTracking()
                .Include(j => j.client)
                .Where(j => j.parentJobId == parentJobId && j.companyId == companyId)
                .OrderByDescending(j => j.occurrenceDate ?? j.scheduledStartDate)
                .Select(j => new JobListItem
                {
                    id = j.id,
                    companyId = j.companyId,
                    title = j.title,
                    jobNumber = j.jobNumber,
                    status = j.status,
                    clientId = j.client != null ? j.client.id : (Guid?)null,
                    clientName = j.client != null
                        ? (!string.IsNullOrEmpty(j.client.companyName) ? j.client.companyName : j.client.contactName)
                        : string.Empty,
                    scheduledStartDate = j.scheduledStartDate,
                    estimatedTotal = j.estimatedTotal,
                    isRecurring = false,
                    parentJobId = j.parentJobId,
                    occurrenceDate = j.occurrenceDate,
                    createDatetime = j.createDatetime
                })
                .ToListAsync();
        }

        /// <summary>
        /// Delete all related entities for a job (goals, assignments, notes, etc.)
        /// </summary>
        private async Task DeleteJobRelatedEntitiesAsync(Guid jobId)
        {
            // 1. Delete JobGoalMedia (depends on JobGoals)
            var goalIds = await _ctx.JobGoals.Where(g => g.jobId == jobId).Select(g => g.id).ToListAsync();
            if (goalIds.Any())
            {
                var goalMedia = await _ctx.JobGoalMedia.Where(m => goalIds.Contains(m.goalId)).ToListAsync();
                _ctx.JobGoalMedia.RemoveRange(goalMedia);

                // 2. Delete JobGoalVotes (depends on JobGoals)
                var goalVotes = await _ctx.JobGoalVotes.Where(v => goalIds.Contains(v.goalId)).ToListAsync();
                _ctx.JobGoalVotes.RemoveRange(goalVotes);
            }

            // 3. Delete JobGoals
            var goals = await _ctx.JobGoals.Where(g => g.jobId == jobId).ToListAsync();
            _ctx.JobGoals.RemoveRange(goals);

            // 4. Delete JobAssignments
            var assignments = await _ctx.JobAssignments.Where(a => a.jobId == jobId).ToListAsync();
            _ctx.JobAssignments.RemoveRange(assignments);

            // 5. Delete JobNotes
            var notes = await _ctx.JobNotes.Where(n => n.jobId == jobId).ToListAsync();
            _ctx.JobNotes.RemoveRange(notes);

            // 6. Delete JobMilestones
            var milestones = await _ctx.JobMilestones.Where(m => m.jobId == jobId).ToListAsync();
            _ctx.JobMilestones.RemoveRange(milestones);

            // 7. Delete JobMedia
            var media = await _ctx.JobMedia.Where(m => m.jobId == jobId).ToListAsync();
            _ctx.JobMedia.RemoveRange(media);

            // 8. Delete JobAlbums
            var albums = await _ctx.JobAlbums.Where(a => a.jobId == jobId).ToListAsync();
            _ctx.JobAlbums.RemoveRange(albums);

            // 9. Delete TimeCards
            var timeCards = await _ctx.TimeCards.Where(t => t.jobId == jobId).ToListAsync();
            _ctx.TimeCards.RemoveRange(timeCards);

            // 10. Delete JobInventory
            var jobInventory = await _ctx.JobInventory.Where(ji => ji.jobId == jobId).ToListAsync();
            _ctx.JobInventory.RemoveRange(jobInventory);

            // 11. Delete Payments
            var payments = await _ctx.Payments.Where(p => p.jobId == jobId).ToListAsync();
            _ctx.Payments.RemoveRange(payments);
        }

        public async Task<bool> DeleteJobAsync(Guid id, Guid companyId)
        {
            var job = await _ctx.Job
                .FirstOrDefaultAsync(j => j.id == id && j.companyId == companyId);

            if (job == null)
                return false;

            // First, delete any child jobs (occurrences) if this is a recurring parent job
            var childJobs = await _ctx.Job
                .Where(j => j.parentJobId == id && j.companyId == companyId)
                .ToListAsync();

            if (childJobs.Any())
            {
                // Delete all related entities for child jobs
                foreach (var childJob in childJobs)
                {
                    await DeleteJobRelatedEntitiesAsync(childJob.id);
                    _ctx.Job.Remove(childJob);
                }
            }

            // Delete related entities for the main job
            await DeleteJobRelatedEntitiesAsync(id);

            // Finally delete the job
            _ctx.Job.Remove(job);
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Extract S3 key from a stored S3 URL
        /// </summary>
        private static string? ExtractS3KeyFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                var path = uri.AbsolutePath.TrimStart('/');
                if (!string.IsNullOrEmpty(path))
                {
                    return path;
                }
            }

            if (!url.StartsWith("http"))
            {
                return url;
            }

            return null;
        }
    }
}
