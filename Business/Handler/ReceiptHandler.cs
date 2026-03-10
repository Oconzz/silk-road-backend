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
    public class ReceiptHandler
    {
        private readonly DBContext _ctx;
        private readonly IReceiptService _receiptService;

        public ReceiptHandler(DBContext ctx, IReceiptService receiptService)
        {
            _ctx = ctx;
            _receiptService = receiptService;
        }

        public async Task<PagedResponse<ReceiptListItem>> GetReceiptListAsync(ReceiptListRequestVM request, Guid companyId)
        {
            if (request == null) request = new ReceiptListRequestVM();
            if (request.pageNumber <= 0) request.pageNumber = 1;
            if (request.pageSize <= 0 || request.pageSize > 200) request.pageSize = 20;

            IQueryable<Receipts> query = _ctx.Receipts
                .AsNoTracking()
                .Where(r => r.companyId == companyId);

            // Search by filename
            if (!string.IsNullOrWhiteSpace(request.searchTerm))
            {
                var term = request.searchTerm.Trim().ToLower();
                query = query.Where(r => r.fileName != null && r.fileName.ToLower().Contains(term));
            }

            // Filter by date range
            if (request.fromDate.HasValue)
            {
                query = query.Where(r => r.createDatetime >= request.fromDate.Value);
            }

            if (request.toDate.HasValue)
            {
                var toDateEndOfDay = request.toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(r => r.createDatetime <= toDateEndOfDay);
            }

            // Default sorting by createDatetime descending (newest first)
            query = query.OrderByDescending(r => r.createDatetime);

            var totalCount = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCount / (double)request.pageSize);
            var skip = (request.pageNumber - 1) * request.pageSize;

            var items = await query.Skip(skip).Take(request.pageSize)
                .Select(r => new ReceiptListItem
                {
                    id = r.id,
                    companyId = r.companyId,
                    fileName = r.fileName,
                    url = GetPublicUrl(r.s3Name),
                    size = r.size,
                    contentType = r.contentType,
                    createDatetime = r.createDatetime
                })
                .ToListAsync();

            return new PagedResponse<ReceiptListItem>
            {
                Items = items,
                PageNumber = request.pageNumber,
                PageSize = request.pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                SearchTerm = request.searchTerm
            };
        }

        public async Task<ReceiptRM?> GetReceiptByIdAsync(Guid id, Guid companyId)
        {
            var receipt = await _ctx.Receipts
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.id == id && r.companyId == companyId);

            if (receipt == null)
                return null;

            return new ReceiptRM
            {
                id = receipt.id,
                companyId = receipt.companyId,
                s3Name = receipt.s3Name,
                fileName = receipt.fileName,
                url = GetPublicUrl(receipt.s3Name),
                size = receipt.size,
                contentType = receipt.contentType,
                createDatetime = receipt.createDatetime,
                updateDatetime = receipt.updateDatetime
            };
        }

        public async Task<ReceiptRM> CreateReceiptAsync(CreateReceiptVM vm, Guid companyId)
        {
            if (string.IsNullOrWhiteSpace(vm.receiptBase64))
                throw new ArgumentException("Receipt image data is required");

            if (string.IsNullOrWhiteSpace(vm.fileName))
                throw new ArgumentException("File name is required");

            var now = DateTime.UtcNow;

            // Upload to S3
            var (s3Name, url, size, contentType) = await _receiptService.UploadReceiptAsync(vm.receiptBase64, companyId.ToString());

            if (string.IsNullOrEmpty(s3Name))
                throw new InvalidOperationException("Failed to upload receipt to storage");

            var receipt = new Receipts
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                s3Name = s3Name,
                fileName = vm.fileName.Trim(),
                size = size,
                contentType = contentType,
                createDatetime = now,
                updateDatetime = now
            };

            _ctx.Receipts.Add(receipt);
            await _ctx.SaveChangesAsync();

            return new ReceiptRM
            {
                id = receipt.id,
                companyId = receipt.companyId,
                s3Name = receipt.s3Name,
                fileName = receipt.fileName,
                url = url ?? string.Empty,
                size = receipt.size,
                contentType = receipt.contentType,
                createDatetime = receipt.createDatetime,
                updateDatetime = receipt.updateDatetime
            };
        }

        public async Task<ReceiptRM?> UpdateReceiptAsync(UpdateReceiptVM vm, Guid companyId)
        {
            var receipt = await _ctx.Receipts
                .FirstOrDefaultAsync(r => r.id == vm.id && r.companyId == companyId);

            if (receipt == null)
                return null;

            // Update filename if provided
            if (!string.IsNullOrWhiteSpace(vm.fileName))
            {
                receipt.fileName = vm.fileName.Trim();
            }

            // Replace receipt image if new one provided
            if (!string.IsNullOrWhiteSpace(vm.receiptBase64))
            {
                // Delete old receipt from S3
                await _receiptService.DeleteReceiptAsync(receipt.s3Name);

                // Upload new receipt
                var (s3Name, url, size, contentType) = await _receiptService.UploadReceiptAsync(vm.receiptBase64, companyId.ToString());

                if (string.IsNullOrEmpty(s3Name))
                    throw new InvalidOperationException("Failed to upload new receipt to storage");

                receipt.s3Name = s3Name;
                receipt.size = size;
                receipt.contentType = contentType;
            }

            receipt.updateDatetime = DateTime.UtcNow;

            _ctx.Receipts.Update(receipt);
            await _ctx.SaveChangesAsync();

            return await GetReceiptByIdAsync(receipt.id, companyId);
        }

        public async Task<bool> DeleteReceiptAsync(Guid id, Guid companyId)
        {
            var receipt = await _ctx.Receipts
                .FirstOrDefaultAsync(r => r.id == id && r.companyId == companyId);

            if (receipt == null)
                return false;

            // Delete from S3
            await _receiptService.DeleteReceiptAsync(receipt.s3Name);

            // Delete from database
            _ctx.Receipts.Remove(receipt);
            await _ctx.SaveChangesAsync();

            return true;
        }

        private string GetPublicUrl(string s3Name)
        {
            if (string.IsNullOrEmpty(s3Name))
                return string.Empty;

            var bucket = Environment.GetEnvironmentVariable("PUBLIC_S3_BUCKET") ?? "public-cnk";
            // Simplified URL construction - adjust region if needed
            return $"https://{bucket}.s3.amazonaws.com/{s3Name}";
        }
    }
}
