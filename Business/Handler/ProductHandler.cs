using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Business.ViewModel;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers
{
    public class ProductHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public ProductHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        public async Task<PagedResponse<ProductListItem>> GetProductListAsync(ProductListRequestVM request)
        {
            if (request == null) request = new ProductListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Products> query = _ctx.Products.AsNoTracking();

            // Search
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(p =>
                    (p.name != null && p.name.ToLower().Contains(term)) ||
                    (p.description != null && p.description.ToLower().Contains(term)) ||
                    (p.barcode != null && p.barcode.ToLower().Contains(term))
                );
            }

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var products = await query
                .OrderByDescending(p => p.createDatetime)
                .Skip(skip)
                .Take(request.pageSize)
                .ToListAsync();

            var items = products.Select(p => new ProductListItem
            {
                id = p.id,
                productId = p.productId,
                name = p.name,
                description = p.description,
                price = p.price,
                barcode = p.barcode,
                createDatetime = p.createDatetime
            }).ToList();

            return new PagedResponse<ProductListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<ProductRM?> GetProductByIdAsync(Guid id)
        {
            var product = await _ctx.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.id == id);

            if (product == null)
                return null;

            return _mapper.Map<Products, ProductRM>(product);
        }

        public async Task<ProductRM?> GetProductByBarcodeAsync(string barcode)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                return null;

            var product = await _ctx.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.barcode == barcode);

            if (product == null)
                return null;

            return _mapper.Map<Products, ProductRM>(product);
        }

        public async Task<ProductRM> CreateProductAsync(CreateProductVM vm)
        {
            // Check if barcode already exists
            var existingProduct = await _ctx.Products
                .FirstOrDefaultAsync(p => p.barcode == vm.barcode);

            if (existingProduct != null)
                throw new InvalidOperationException("A product with this barcode already exists");

            var now = DateTime.UtcNow;
            var productId = Guid.NewGuid();

            var product = new Products
            {
                id = Guid.NewGuid(),
                productId = productId,
                name = vm.name,
                description = vm.description,
                price = vm.price,
                barcode = vm.barcode,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.Products.Add(product);
            await _ctx.SaveChangesAsync();

            return _mapper.Map<Products, ProductRM>(product);
        }

        public async Task<ProductRM?> UpdateProductAsync(UpdateProductVM vm)
        {
            var product = await _ctx.Products
                .FirstOrDefaultAsync(p => p.id == vm.id);

            if (product == null)
                return null;

            // Check if barcode is being changed and if new barcode already exists
            if (!string.IsNullOrEmpty(vm.barcode) && vm.barcode != product.barcode)
            {
                var existingProduct = await _ctx.Products
                    .FirstOrDefaultAsync(p => p.barcode == vm.barcode && p.id != vm.id);

                if (existingProduct != null)
                    throw new InvalidOperationException("A product with this barcode already exists");

                product.barcode = vm.barcode;
            }

            if (!string.IsNullOrEmpty(vm.name))
                product.name = vm.name;
            if (vm.description != null)
                product.description = vm.description;
            if (vm.price.HasValue)
                product.price = vm.price.Value;

            product.updateDatetime = DateTime.UtcNow;

            _ctx.Products.Update(product);
            await _ctx.SaveChangesAsync();

            return _mapper.Map<Products, ProductRM>(product);
        }

        public async Task<bool> DeleteProductAsync(Guid id)
        {
            var product = await _ctx.Products
                .FirstOrDefaultAsync(p => p.id == id);

            if (product == null)
                return false;

            // Check if product is used in any inventory
            var inventoryExists = await _ctx.Inventory
                .AnyAsync(i => i.productId == product.productId);

            if (inventoryExists)
                throw new InvalidOperationException("Cannot delete product that is being used in inventory");

            _ctx.Products.Remove(product);
            await _ctx.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Helper method to get or create a product by barcode
        /// Used by InventoryHandler when creating inventory
        /// </summary>
        public async Task<Products> GetOrCreateProductByBarcodeAsync(string barcode, string name, string description, decimal price)
        {
            if (string.IsNullOrWhiteSpace(barcode))
                throw new InvalidOperationException("Barcode is required");

            var product = await _ctx.Products
                .FirstOrDefaultAsync(p => p.barcode == barcode);

            if (product == null)
            {
                var now = DateTime.UtcNow;
                var productId = Guid.NewGuid();

                product = new Products
                {
                    id = Guid.NewGuid(),
                    productId = productId,
                    name = name,
                    description = description,
                    price = price,
                    barcode = barcode,
                    createDatetime = now,
                    updateDatetime = now
                };

                _ctx.Products.Add(product);
                await _ctx.SaveChangesAsync();
            }

            return product;
        }
    }
}
