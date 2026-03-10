using System;
using System.Linq;
using System.Threading.Tasks;
using Business.Data;
using Business.Model;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;

namespace Business.Handler
{
    public class CompanyHandler
    {
        private readonly DBContext _ctx;

        public CompanyHandler(DBContext ctx)
        {
            _ctx = ctx;
        }

        public async Task<Company?> UpdateCompanyAsync(UpdateCompanyVM vm, Guid companyId)
        {
            var company = await _ctx.Company.FirstOrDefaultAsync(c => c.id == companyId);
            if (company == null) return null;

            // Only update fields that are present/allowed
            if (!string.IsNullOrEmpty(vm.name)) company.name = vm.name;
            if (!string.IsNullOrEmpty(vm.category)) company.category = vm.category;
            if (vm.city != null) company.city = vm.city;
            if (vm.state != null) company.state = vm.state;
            if (vm.zipcode != null) company.zipcode = vm.zipcode;
            if (vm.country != null) company.country = vm.country;
            if (vm.logoUrl != null) company.logo = vm.logoUrl;

            company.updateDatetime = DateTime.UtcNow;

            await _ctx.SaveChangesAsync();

            return company;
        }

        public async Task<Company?> GetCompanyByIdAsync(Guid id)
        {
            return await _ctx.Company.FirstOrDefaultAsync(c => c.id == id);
        }
    }
}
