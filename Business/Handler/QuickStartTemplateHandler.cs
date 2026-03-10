using AutoMapper;
using Business.Data;
using Business.Model;
using Business.Response;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Business.Handlers
{
    /// <summary>
    /// Handler for Quick Start Template operations.
    /// These are global read-only templates that users can copy to create draft estimates.
    /// </summary>
    public class QuickStartTemplateHandler
    {
        private readonly DBContext _ctx;
        private readonly IMapper _mapper;

        public QuickStartTemplateHandler(DBContext ctx, IMapper mapper)
        {
            _ctx = ctx;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all active templates for a specific category
        /// </summary>
        public async Task<List<QuickStartTemplateRM>> GetTemplatesByCategoryAsync(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return new List<QuickStartTemplateRM>();

            var templates = await _ctx.QuickStartTemplates
                .AsNoTracking()
                .Where(t => t.category == category && t.active)
                .OrderBy(t => t.displayOrder)
                .Select(t => new QuickStartTemplateRM
                {
                    id = t.id,
                    category = t.category,
                    name = t.name,
                    description = t.description,
                    displayOrder = t.displayOrder
                })
                .ToListAsync();

            return templates;
        }

        /// <summary>
        /// Copy a Quick Start Template to create a new draft Estimate for the company
        /// </summary>
        public async Task<EstimateRM?> CopyTemplateToDraftAsync(Guid templateId, Guid companyId)
        {
            var template = await _ctx.QuickStartTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.id == templateId && t.active);

            if (template == null)
                return null;

            var now = DateTime.UtcNow;

            // Create new draft estimate based on the template
            var estimate = new Estimates
            {
                id = Guid.NewGuid(),
                companyId = companyId,
                name = template.name,
                description = template.description,
                total = 0,
                sent = false,
                approved = false,
                sentDateTime = DateTime.MinValue,
                createDatetime = now,
                updateDatetime = now,
                items = new List<EstimateItem>()
            };

            _ctx.Estimates.Add(estimate);
            await _ctx.SaveChangesAsync();

            // Return the created estimate
            return new EstimateRM
            {
                id = estimate.id,
                companyId = estimate.companyId,
                name = estimate.name,
                description = estimate.description,
                total = estimate.total,
                sent = estimate.sent,
                approved = estimate.approved,
                createDatetime = estimate.createDatetime,
                updateDatetime = estimate.updateDatetime
            };
        }

        /// <summary>
        /// Get a template by ID (for verification purposes)
        /// </summary>
        public async Task<QuickStartTemplateRM?> GetTemplateByIdAsync(Guid id)
        {
            var template = await _ctx.QuickStartTemplates
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.id == id && t.active);

            if (template == null)
                return null;

            return new QuickStartTemplateRM
            {
                id = template.id,
                category = template.category,
                name = template.name,
                description = template.description,
                displayOrder = template.displayOrder
            };
        }
    }
}
