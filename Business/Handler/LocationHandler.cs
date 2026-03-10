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
    public class LocationHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public LocationHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<PagedResponse<LocationListItem>> GetLocationListAsync(LocationListRequestVM request, Guid companyId)
        {
            if (request == null) request = new LocationListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Location> query = _ctx.Location
                .AsNoTracking()
                .Where(l => l.companyId == companyId);

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(l =>
                    (l.addressLine1 != null && l.addressLine1.ToLower().Contains(term)) ||
                    (l.addressLine2 != null && l.addressLine2.ToLower().Contains(term)) ||
                    (l.city != null && l.city.ToLower().Contains(term)) ||
                    (l.state != null && l.state.ToLower().Contains(term)) ||
                    (l.zipcode != null && l.zipcode.ToLower().Contains(term)) ||
                    (l.country != null && l.country.ToLower().Contains(term))
                );
            }

            // Default sorting by createDatetime descending
            query = query.OrderByDescending(l => l.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(l => new LocationListItem
                {
                    id = l.id,
                    companyId = l.companyId,
                    addressLine1 = l.addressLine1,
                    city = l.city,
                    state = l.state,
                    zipcode = l.zipcode,
                    createDatetime = l.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<LocationListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<LocationRM?> GetLocationByIdAsync(Guid id, Guid companyId)
        {
            var location = await _ctx.Location
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.id == id && l.companyId == companyId);

            if (location == null)
                return null;

            return _mapper.Map<Location, LocationRM>(location);
        }

        public async Task<List<LocationRM>> GetLocationsByClientIdAsync(Guid clientId, Guid companyId)
        {
            var locations = await _ctx.Location
                .AsNoTracking()
                .Where(l => l.clientId == clientId && l.companyId == companyId)
                .OrderByDescending(l => l.createDatetime)
                .ToListAsync();

            return _mapper.Map<List<Location>, List<LocationRM>>(locations);
        }

        public async Task<LocationRM> CreateLocationAsync(CreateLocationVM vm, Guid companyId)
        {
            var now = DateTime.UtcNow;

            var location = new Location
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                clientId = vm.clientId,
                name = vm.name ?? "Location",
                addressLine1 = vm.addressLine1,
                addressLine2 = vm.addressLine2,
                city = vm.city,
                state = vm.state,
                zipcode = vm.zipcode,
                country = vm.country,
                latitude = vm.latitude,
                longitude = vm.longitude,
                isPrimary = vm.isPrimary,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.Location.Add(location);
            await _ctx.SaveChangesAsync();

            return _mapper.Map<Location, LocationRM>(location);
        }

        public async Task<LocationRM?> UpdateLocationAsync(UpdateLocationVM vm, Guid companyId)
        {
            var location = await _ctx.Location
                .FirstOrDefaultAsync(l => l.id == vm.id && l.companyId == companyId);

            if (location == null)
                return null;

            if (!string.IsNullOrEmpty(vm.addressLine1))
                location.addressLine1 = vm.addressLine1;
            if (vm.addressLine2 != null)
                location.addressLine2 = vm.addressLine2;
            if (!string.IsNullOrEmpty(vm.city))
                location.city = vm.city;
            if (!string.IsNullOrEmpty(vm.state))
                location.state = vm.state;
            if (!string.IsNullOrEmpty(vm.zipcode))
                location.zipcode = vm.zipcode;
            if (!string.IsNullOrEmpty(vm.country))
                location.country = vm.country;
            if (vm.latitude.HasValue)
                location.latitude = vm.latitude.Value;
            if (vm.longitude.HasValue)
                location.longitude = vm.longitude.Value;
            if (vm.isPrimary.HasValue)
                location.isPrimary = vm.isPrimary.Value;

            location.updateDatetime = DateTime.UtcNow;

            _ctx.Location.Update(location);
            await _ctx.SaveChangesAsync();

            return _mapper.Map<Location, LocationRM>(location);
        }

        public async Task<bool> DeleteLocationAsync(Guid id, Guid companyId)
        {
            var location = await _ctx.Location
                .FirstOrDefaultAsync(l => l.id == id && l.companyId == companyId);

            if (location == null)
                return false;

            _ctx.Location.Remove(location);
            await _ctx.SaveChangesAsync();

            return true;
        }
    }
}
