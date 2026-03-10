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
    public class ClientHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public ClientHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<PagedResponse<ClientListItem>> GetClientListAsync(ClientListRequestVM request, Guid companyId)
        {
            if (request == null) request = new ClientListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Client> query = _ctx.Client
                .AsNoTracking()
                .Include(c => c.locations)
                .Where(c => c.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(c =>
                    (c.contactName != null && c.contactName.ToLower().Contains(term)) ||
                    (c.email != null && c.email.ToLower().Contains(term)) ||
                    (c.phone != null && c.phone.ToLower().Contains(term)) ||
                    (c.companyName != null && c.companyName.ToLower().Contains(term))
                );
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(c => c.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(c => new ClientListItem
                {
                    id = c.id,
                    companyId = c.companyId,
                    contactName = c.contactName,
                    phone = c.phone,
                    email = c.email,
                    companyName = c.companyName,
                    primaryAddress = c.locations != null && c.locations.Any()
                        ? c.locations.Where(l => l.isPrimary).Select(l => l.city + ", " + l.state).FirstOrDefault()
                          ?? c.locations.OrderBy(l => l.createDatetime).Select(l => l.city + ", " + l.state).FirstOrDefault()
                        : null,
                    createDatetime = c.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<ClientListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<ClientRM?> GetClientByIdAsync(Guid id, Guid companyId)
        {
            var client = await _ctx.Client
                .AsNoTracking()
                .Include(c => c.locations)
                .FirstOrDefaultAsync(c => c.id == id && c.companyId == companyId);

            if (client == null)
                return null;

            return _mapper.Map<Client, ClientRM>(client);
        }

        public async Task<ClientRM> CreateClientAsync(CreateClientVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            var client = new Client
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                contactName = vm.contactName,
                phone = vm.phone,
                email = vm.email,
                companyName = vm.companyName,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.Client.Add(client);

            // Add location if provided (legacy single location)
            if (vm.location != null)
            {
                var location = new Location
                {
                    id = Guid.NewGuid(),
                    companyId = companyId,
                    clientId = client.id,
                    name = vm.location.name ?? "Primary",
                    addressLine1 = vm.location.addressLine1,
                    addressLine2 = vm.location.addressLine2,
                    city = vm.location.city,
                    state = vm.location.state,
                    zipcode = vm.location.zipcode,
                    country = vm.location.country,
                    latitude = vm.location.latitude,
                    longitude = vm.location.longitude,
                    createDatetime = now,
                    updateDatetime = now
                };
                _ctx.Location.Add(location);
            }

            await _ctx.SaveChangesAsync();

            // Reload with locations
            var created = await _ctx.Client
                .Include(c => c.locations)
                .FirstOrDefaultAsync(c => c.id == client.id);

            return _mapper.Map<Client, ClientRM>(created!);
        }

        public async Task<ClientRM?> UpdateClientAsync(UpdateClientVM vm, Guid companyId)
        {
            var client = await _ctx.Client
                .Include(c => c.locations)
                .FirstOrDefaultAsync(c => c.id == vm.id && c.companyId == companyId);

            if (client == null)
                return null;

            var now = DateTime.UtcNow;

            if (!string.IsNullOrEmpty(vm.contactName))
                client.contactName = vm.contactName;
            if (!string.IsNullOrEmpty(vm.phone))
                client.phone = vm.phone;
            if (!string.IsNullOrEmpty(vm.email))
                client.email = vm.email;
            if (!string.IsNullOrEmpty(vm.companyName))
                client.companyName = vm.companyName;

            // Handle location update
            if (vm.location != null)
            {
                var primaryLocation = client.locations?.FirstOrDefault();

                if (primaryLocation != null)
                {
                    // Update existing primary location
                    if (!string.IsNullOrEmpty(vm.location.name))
                        primaryLocation.name = vm.location.name;
                    if (!string.IsNullOrEmpty(vm.location.addressLine1))
                        primaryLocation.addressLine1 = vm.location.addressLine1;
                    if (vm.location.addressLine2 != null)
                        primaryLocation.addressLine2 = vm.location.addressLine2;
                    if (!string.IsNullOrEmpty(vm.location.city))
                        primaryLocation.city = vm.location.city;
                    if (!string.IsNullOrEmpty(vm.location.state))
                        primaryLocation.state = vm.location.state;
                    if (!string.IsNullOrEmpty(vm.location.zipcode))
                        primaryLocation.zipcode = vm.location.zipcode;
                    if (!string.IsNullOrEmpty(vm.location.country))
                        primaryLocation.country = vm.location.country;
                    if (vm.location.latitude.HasValue)
                        primaryLocation.latitude = vm.location.latitude.Value;
                    if (vm.location.longitude.HasValue)
                        primaryLocation.longitude = vm.location.longitude.Value;

                    primaryLocation.updateDatetime = now;
                    _ctx.Location.Update(primaryLocation);
                }
                else
                {
                    // Create new primary location
                    var newLocation = new Location
                    {
                        id = Guid.NewGuid(),
                        companyId = companyId,
                        clientId = client.id,
                        name = vm.location.name ?? "Primary",
                        addressLine1 = vm.location.addressLine1,
                        addressLine2 = vm.location.addressLine2,
                        city = vm.location.city,
                        state = vm.location.state,
                        zipcode = vm.location.zipcode,
                        country = vm.location.country,
                        latitude = vm.location.latitude ?? 0,
                        longitude = vm.location.longitude ?? 0,
                        createDatetime = now,
                        updateDatetime = now
                    };
                    _ctx.Location.Add(newLocation);
                }
            }

            client.updateDatetime = now;

            _ctx.Client.Update(client);
            await _ctx.SaveChangesAsync();

            // Reload to get updated locations
            var updated = await _ctx.Client
                .Include(c => c.locations)
                .FirstOrDefaultAsync(c => c.id == client.id);

            return _mapper.Map<Client, ClientRM>(updated!);
        }

        public async Task<bool> DeleteClientAsync(Guid id, Guid companyId)
        {
            var client = await _ctx.Client
                .Include(c => c.locations)
                .FirstOrDefaultAsync(c => c.id == id && c.companyId == companyId);

            if (client == null)
                return false;

            // Get location IDs to clear references
            var locationIds = client.locations?.Select(l => l.id).ToList() ?? new List<Guid>();

            if (locationIds.Any())
            {
                // Clear Job references to these locations
                var jobsWithLocations = await _ctx.Job
                    .Where(j => j.location != null && locationIds.Contains(j.location.id))
                    .ToListAsync();
                foreach (var job in jobsWithLocations)
                {
                    job.location = null;
                }

                // Clear Estimate references to these locations
                var estimatesWithLocations = await _ctx.Estimates
                    .Where(e => e.location != null && locationIds.Contains(e.location.id))
                    .ToListAsync();
                foreach (var estimate in estimatesWithLocations)
                {
                    estimate.location = null;
                }
            }

            // Clear Job references to this client
            var jobsWithClient = await _ctx.Job
                .Where(j => j.client != null && j.client.id == id)
                .ToListAsync();
            foreach (var job in jobsWithClient)
            {
                job.client = null;
            }

            // Clear Estimate references to this client
            var estimatesWithClient = await _ctx.Estimates
                .Where(e => e.client != null && e.client.id == id)
                .ToListAsync();
            foreach (var estimate in estimatesWithClient)
            {
                estimate.client = null;
            }

            // Now safe to delete client (locations will cascade delete)
            _ctx.Client.Remove(client);
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Bulk create clients with duplicate detection by email and phone
        /// </summary>
        public async Task<BulkCreateClientResultVM> BulkCreateClientsAsync(BulkCreateClientVM request, Guid companyId)
        {
            var result = new BulkCreateClientResultVM
            {
                totalRequested = request.clients.Count,
                results = new List<BulkClientResultItem>()
            };

            // Fetch existing clients for duplicate detection (email and phone)
            var existingClients = await _ctx.Client
                .AsNoTracking()
                .Where(c => c.companyId == companyId)
                .Select(c => new { c.id, c.email, c.phone, c.contactName })
                .ToListAsync();

            var existingEmails = existingClients
                .Where(c => !string.IsNullOrWhiteSpace(c.email))
                .GroupBy(c => c.email.ToLower().Trim())
                .ToDictionary(g => g.Key, g => g.First());

            var existingPhones = existingClients
                .Where(c => !string.IsNullOrWhiteSpace(c.phone))
                .GroupBy(c => NormalizePhone(c.phone))
                .ToDictionary(g => g.Key, g => g.First());

            // Track newly added in this batch to prevent intra-batch duplicates
            var batchEmails = new Dictionary<string, int>();
            var batchPhones = new Dictionary<string, int>();

            for (int i = 0; i < request.clients.Count; i++)
            {
                var vm = request.clients[i];
                var resultItem = new BulkClientResultItem
                {
                    index = i,
                    contactName = vm.contactName ?? string.Empty,
                    email = vm.email ?? string.Empty,
                    phone = vm.phone ?? string.Empty
                };

                try
                {
                    // Validate required fields
                    if (string.IsNullOrWhiteSpace(vm.contactName))
                    {
                        resultItem.status = "error";
                        resultItem.errorMessage = "Contact name is required";
                        result.totalErrors++;
                        result.results.Add(resultItem);
                        continue;
                    }

                    // Check for existing email duplicate
                    var normalizedEmail = (vm.email ?? "").ToLower().Trim();
                    if (!string.IsNullOrEmpty(normalizedEmail))
                    {
                        if (existingEmails.TryGetValue(normalizedEmail, out var existingByEmail))
                        {
                            resultItem.status = "duplicate";
                            resultItem.duplicateField = "email";
                            resultItem.existingClientId = existingByEmail.id;
                            resultItem.errorMessage = $"Client with this email already exists: {existingByEmail.contactName}";
                            result.totalDuplicates++;
                            result.results.Add(resultItem);
                            continue;
                        }
                        if (batchEmails.TryGetValue(normalizedEmail, out var batchIndex))
                        {
                            resultItem.status = "duplicate";
                            resultItem.duplicateField = "email";
                            resultItem.errorMessage = $"Duplicate of item {batchIndex} in this batch";
                            result.totalDuplicates++;
                            result.results.Add(resultItem);
                            continue;
                        }
                    }

                    // Check for existing phone duplicate
                    var normalizedPhone = NormalizePhone(vm.phone ?? "");
                    if (!string.IsNullOrEmpty(normalizedPhone))
                    {
                        if (existingPhones.TryGetValue(normalizedPhone, out var existingByPhone))
                        {
                            resultItem.status = "duplicate";
                            resultItem.duplicateField = "phone";
                            resultItem.existingClientId = existingByPhone.id;
                            resultItem.errorMessage = $"Client with this phone already exists: {existingByPhone.contactName}";
                            result.totalDuplicates++;
                            result.results.Add(resultItem);
                            continue;
                        }
                        if (batchPhones.TryGetValue(normalizedPhone, out var batchIndex))
                        {
                            resultItem.status = "duplicate";
                            resultItem.duplicateField = "phone";
                            resultItem.errorMessage = $"Duplicate of item {batchIndex} in this batch";
                            result.totalDuplicates++;
                            result.results.Add(resultItem);
                            continue;
                        }
                    }

                    // Create the client
                    var now = DateTime.UtcNow;
                    var client = new Client
                    {
                        id = Guid.NewGuid(),
                        companyId = companyId,
                        contactName = TruncateString(vm.contactName, 100),
                        phone = TruncateString(vm.phone ?? "", 25),
                        email = TruncateString(vm.email ?? "", 50),
                        companyName = TruncateString(vm.companyName ?? "", 25),
                        createDatetime = now,
                        updateDatetime = now
                    };

                    _ctx.Client.Add(client);
                    await _ctx.SaveChangesAsync();

                    // Track for intra-batch duplicate detection
                    if (!string.IsNullOrEmpty(normalizedEmail))
                        batchEmails[normalizedEmail] = i;
                    if (!string.IsNullOrEmpty(normalizedPhone))
                        batchPhones[normalizedPhone] = i;

                    resultItem.id = client.id;
                    resultItem.status = "created";
                    result.totalCreated++;
                }
                catch (Exception ex)
                {
                    resultItem.status = "error";
                    resultItem.errorMessage = ex.Message;
                    result.totalErrors++;
                }

                result.results.Add(resultItem);
            }

            return result;
        }

        /// <summary>
        /// Normalize phone number by removing all non-digit characters for comparison
        /// </summary>
        private static string NormalizePhone(string phone)
        {
            if (string.IsNullOrEmpty(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        /// <summary>
        /// Truncate string to max length
        /// </summary>
        private static string TruncateString(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }
    }
}
