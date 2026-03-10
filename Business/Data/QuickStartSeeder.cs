using Business.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Business.Data
{
    public static class QuickStartSeeder
    {
        /// <summary>
        /// Generate a deterministic GUID from a string key so template IDs remain stable across restarts
        /// </summary>
        private static Guid DeterministicGuid(string key)
        {
            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
            return new Guid(hash);
        }

        public static void Seed(DBContext context)
        {
            try
            {
                // Only seed if the table is empty — don't delete and re-create every startup
                // This prevents template IDs from changing, which would break cached references
                if (context.QuickStartTemplates.Any())
                {
                    Console.WriteLine("[Seed] QuickStart Templates already exist, skipping seed.");
                    return;
                }

                Console.WriteLine("[Seed] Seeding 110 QuickStart Templates...");
                var templates = GetTemplates();
                context.QuickStartTemplates.AddRange(templates);
                context.SaveChanges();
                Console.WriteLine("[Seed] QuickStart Templates seeded successfully!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Seed] Error seeding QuickStart Templates: {ex.Message}");
            }
        }

        private static List<QuickStartTemplate> GetTemplates()
        {
            var templates = new List<QuickStartTemplate>();
            int order = 1;

            // Helper to add a category
            void AddCategory(string category, Dictionary<string, string> items)
            {
                int localOrder = 1;
                foreach (var item in items)
                {
                    templates.Add(new QuickStartTemplate
                    {
                        id = DeterministicGuid($"QST:{category}:{item.Key}"),
                        category = category,
                        name = item.Key,
                        description = item.Value,
                        displayOrder = localOrder++,
                        active = true,
                        createDatetime = DateTime.UtcNow,
                        updateDatetime = DateTime.UtcNow
                    });
                }
            }

            // 1. General Contracting
            AddCategory("General Contracting", new Dictionary<string, string>
            {
                { "New Custom Home", "Complete estimate for a new custom single-family home build, including foundation, framing, and finishes." },
                { "Whole House Renovation", "Comprehensive renovation estimate covering demolition, structural changes, and full interior updates." },
                { "Commercial Tenant Fit-Out", "Estimate for adapting a commercial space for a new tenant, including partitions, HVAC, and lighting." },
                { "Room Addition", "Construction of a new room addition, including foundation, framing, roofing, and interior intergration." },
                { "Garage Construction", "Detached or attached garage build estimate, including concrete slab, framing, and electrical." },
                { "Basement Finishing", "Converting an unfinished basement into living space with insulation, drywall, flooring, and lighting." },
                { "Kitchen & Bath Remodel Combo", "Combined estimate for simultaneous kitchen and bathroom remodeling projects." },
                { "Deck & Patio Construction", "Outdoor living space creation including wood/composite decking or paver patio installation." },
                { "Window & Door Replacement", "Full house replacement of windows and exterior doors for energy efficiency." },
                { "ADU (Accessory Dwelling Unit)", "Construction of a secondary housing unit, turnkey estimate." }
            });

            // 2. Home Building & Renovations
            AddCategory("Home Building & Renovations", new Dictionary<string, string>
            {
                { "Kitchen Remodel", "Full kitchen upgrade including cabinets, countertops, appliances, and flooring." },
                { "Bathroom Remodel", "Master or guest bath renovation including tub/shower conversion, vanity, and tiling." },
                { "Attic Conversion", "Transforming attic space into a bedroom or office, including insulation and skylights." },
                { "Siding Replacement", "Removal of old siding and installation of new vinyl, fiber cement, or wood siding." },
                { "Roof Replacement", "Tear-off and replacement of asphalt shingle or metal roofing system." },
                { "Hardwood Flooring Installation", "Installation of new hardwood floors or refinishing existing ones throughout the home." },
                { "Interior Paint", "Full interior painting estimate including wall prep, priming, and two coats of finish." },
                { "Exterior Facelift", "Curb appeal improvements including painting, shutters, and porch updates." },
                { "Home Office Setup", "Renovation to create a dedicated home office space with upgraded electrical and lighting." },
                { "Mudroom Addition", "Creation of a mudroom entry with storage lockers, bench, and durable flooring." }
            });

            // 3. Painting
            AddCategory("Painting", new Dictionary<string, string>
            {
                { "Interior Full House", "Painting walls, ceilings, and trim for an entire average-sized home." },
                { "Exterior Repaint", "Pressure washing, scraping, repairs, and painting of exterior siding and trim." },
                { "Cabinet Refinishing", "Sanding, priming, and painting kitchen or bathroom cabinets for a modern look." },
                { "Deck Staining & Sealing", "Cleaning and applying stain or sealer to protect and beautify a wood deck." },
                { "Trim & Door Painting", "Detailed painting of baseboards, crown molding, door casings, and doors." },
                { "Accent Wall", "Application of decorative paint or specialty finish to a feature wall." },
                { "Wallpaper Removal & Paint", "Stripping old wallpaper, skimming walls, and painting." },
                { "New Construction Paint", "Spray and back-roll painting for a newly constructed home (primer + finish)." },
                { "Fence Staining", "Staining and sealing wood fencing to prevent weathering." },
                { "Garage Floor Coating", "Epoxy or polyaspartic coating application for garage floors." }
            });

            // 4. Electrical
            AddCategory("Electrical", new Dictionary<string, string>
            {
                { "Whole House Rewire", "Replacing outdated knob-and-tube or aluminum wiring with modern copper romex." },
                { "Panel Upgrade (200A)", "Upgrading main service panel to 200 Amps to support modern appliances." },
                { "Lighting Fixture Install", "Installation of recessed lights, chandeliers, and ceiling fans throughout the home." },
                { "EV Charger Station", "Installation of a Level 2 electric vehicle charging outlet or hardwired station." },
                { "Generator Installation", "Transfer switch and hookup for a standby or portable generator system." },
                { "Smart Home Integration", "Installation of smart switches, thermostats, and doorbell cameras." },
                { "Hot Tub Circuit", "Dedicated GFCI circuit wiring for a new hot tub or spa." },
                { "Kitchen Remodel Wiring", "Moving outlets, adding appliance circuits, and under-cabinet lighting." },
                { "Smoke & CO Detector", "Hardwiring unified smoke and carbon monoxide detector system." },
                { "Landscape Lighting", "Low-voltage exterior lighting installation for pathways and gardens." }
            });

            // 5. Concrete
            AddCategory("Concrete", new Dictionary<string, string>
            {
                { "Driveway Replacement", "Demolition of old driveway and pouring of new reinforced concrete driveway." },
                { "Stamped Concrete Patio", "Pouring a decorative stamped and colored concrete patio." },
                { "Sidewalk & Walkway", "Forming and pouring concrete walkways with broom finish." },
                { "Garage Slab", "Pouring a floating or monolithic concrete slab for a garage." },
                { "Retaining Wall", "Poured concrete retaining wall with proper drainage and footing." },
                { "Foundation Walls", "Pouring concrete foundation walls for a new house or addition." },
                { "Pool Deck", "Concrete pool surround with slip-resistant finish." },
                { "Concrete Steps", "Forming and pouring durable concrete entry steps." },
                { "Basement Floor", "Pouring a smooth finish basement floor slab." },
                { "Shed Pad", "Small concrete pad installation for a storage shed." }
            });

            // 6. Carpentry
            AddCategory("Carpentry", new Dictionary<string, string>
            {
                { "Custom Cabinets", "Fabrication and installation of bespoke cabinetry for kitchen or build-ins." },
                { "Trim & Molding", "Installation of baseboards, crown molding, and wainscoting." },
                { "Deck Framing", "Structural framing for a new wood or composite deck." },
                { "Framing (New Addition)", "Wall, floor, and roof framing for a residential addition." },
                { "Staircase Install", "Building or replacing interior staircase and railing system." },
                { "Door Installation", "Pre-hung or slab door installation including hardware." },
                { "Window Installation", "Installing new construction or retrofit windows." },
                { "Built-in Shelving", "Custom bookshelves and entertainment center construction." },
                { "Pergola / Gazebo", "Outdoor timber structure construction." },
                { "Subfloor Repair", "Replacing damaged subflooring and strengthening joists." }
            });

            // 7. Landscaping
            AddCategory("Landscaping", new Dictionary<string, string>
            {
                { "Full Landscape Design", "Complete yard makeover including plants, turf, and hardscaping." },
                { "Sod Installation", "Grading soil and laying fresh sod for an instant lawn." },
                { "Irrigation System", "Design and installation of an automated sprinkler system." },
                { "Paver Patio", "Installation of interlocking pavers for a patio or walkway." },
                { "Retaining Wall (Block)", "Building a segmented block retaining wall." },
                { "Tree Planting", "Planting mature trees with proper soil amendment and staking." },
                { "Lawn Maintenance (Annual)", "Yearly contract for mowing, edging, and seasonal cleanups." },
                { "Mulch & Bed Prep", "Weeding, edging, and mulching flower beds." },
                { "Outdoor Lighting", "Installation of low-voltage accent lighting for trees and paths." },
                { "Water Feature", "Installation of a pond, waterfall, or fountain." }
            });

            // 8. Flooring & Tile
            AddCategory("Flooring & Tile", new Dictionary<string, string>
            {
                { "Hardwood Install", "Nail-down or glue-down installation of solid or engineered hardwood." },
                { "Laminate/LVP Install", "Floating installation of laminate or luxury vinyl plank flooring." },
                { "Tile Floor Install", "Ceramic or porcelain tile installation with thinset and grout." },
                { "Shower Tile Job", "Waterproofing and tiling walls and floor of a custom shower." },
                { "Backsplash Tile", "Kitchen backsplash installation typically with subway or mosaic tile." },
                { "Carpet Installation", "Removal of old carpet and installation of pad and new carpet." },
                { "Hardwood Refinishing", "Sanding, staining, and finishing existing hardwood floors." },
                { "Tile Repair", "Replacing cracked tiles and re-grouting specific areas." },
                { "Epoxy Flooring", "Application of decorative epoxy coating for residential or commercial floors." },
                { "Stair Runner", "Installation of a carpet runner on hardwood stairs." }
            });

            // 9. Excavation
            AddCategory("Excavation", new Dictionary<string, string>
            {
                { "Foundation Dig", "Excavation for a new home foundation including hauling spoils." },
                { "Trenching for Utilities", "Digging trenches for water, sewer, power, or gas lines." },
                { "Site Grading", "Rough and final grading to ensure proper drainage around a building." },
                { "Land Clearing", "Removal of trees, brush, and stumps to prepare a lot for construction." },
                { "Septic System Install", "Excavation and installation of septic tank and leach field." },
                { "Driveway Prep", "Excavating and compacting base for a new driveway." },
                { "Pool Excavation", "Digging for an inground swimming pool." },
                { "Backfilling", "Filling in around foundations after concrete pour." },
                { "Demolition (Structure)", "Knocking down an existing structure and clearing debris." },
                { "Drainage correction", "Installing french drains or swales to manage storm water." }
            });

            // 10. Plumbing
            AddCategory("Plumbing", new Dictionary<string, string>
            {
                { "New Bath Rough-In", "Installing drain, waste, and vent lines and water supply for a new bathroom." },
                { "Water Heater Replace", "Removal of old unit and installation of new tank or tankless water heater." },
                { "Re-pipe Whole House", "Replacing old galvanized pipes with new PEX or copper supply lines." },
                { "Fixture Installation", "Setting toilets, sinks, faucets, and tubs after finish phase." },
                { "Sewer Line Repair", "Excavation and repair or replacement of main sewer lateral." },
                { "Gas Line Install", "Running gas piping for stove, dryer, or fireplace." },
                { "Sump Pump Install", "Installation of sump basin and pump with discharge line." },
                { "Water Treatment System", "Installing softener or filtration system for whole-house water quality." },
                { "Kitchen Sink & Disposal", "Hooking up kitchen sink, faucet, and garbage disposal." },
                { "Leak Detection & Repair", "Diagnostic service to find and fix hidden water leaks." }
            });

             // 11. Roofing
            AddCategory("Roofing", new Dictionary<string, string>
            {
                { "Asphalt Shingle Reroof", "Tear-off existing roof and install new architectural shingles." },
                { "Metal Roof Install", "Installation of standing seam or corrugated metal roofing." },
                { "Flat Roof Replacement", "EPDM, TPO, or torch-down roofing for flat or low-slope areas." },
                { "Roof Repair", "Patching leaks, replacing missing shingles, and flashing repair." },
                { "Gutter Installation", "Installing seamless aluminum gutters and downspouts." },
                { "Skylight Installation", "Cutting in and installing a new skylight with flashing." },
                { "Roof Inspection", "Comprehensive inspection report for real estate or insurance." },
                { "Chimney Flashing", "Replacing lead or copper flashing around chimney." },
                { "Ventilation Upgrade", "Adding ridge vents or soffit vents to improve airflow." },
                { "Cedar Shake Roof", "Installation of premium cedar shake roofing materials." }
            });

            return templates;
        }
    }
}
