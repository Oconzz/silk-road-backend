//using Business.Data;
//using Google.Analytics.Data.V1Beta;
//using Google.Apis.Auth.OAuth2;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.Extensions.Configuration;
//using Microsoft.Extensions.Logging;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace Business.Services
//{
//    public class GoogleAnalyticsService
//    {
//        /// <summary>
//        /// Service for interacting with Google Analytics Data API (GA4).
//        /// Handles authentication, report retrieval, pagination, and error handling.
//        /// </summary>
//        private readonly string _propertyId;
//        private readonly string _credentialsPath;
//        private readonly ILogger<GoogleAnalyticsService> _logger;
//        private readonly BetaAnalyticsDataClient _analyticsDataClient;
//        private readonly DBContext? _ctx; // optional, used when loading creds by clientId

//        /// <summary>
//        /// Lightweight result holding headers and all paged rows for a GA4 report.
//        /// </summary>
//        public record GaReportResult(
//            IReadOnlyList<DimensionHeader> DimensionHeaders,
//            IReadOnlyList<MetricHeader> MetricHeaders,
//            IReadOnlyList<Row> Rows);

//        /// <summary>
//        /// Initializes a new instance with configuration-based credentials (fallback mode).
//        /// </summary>
//        public GoogleAnalyticsService(IConfiguration configuration, ILogger<GoogleAnalyticsService> logger)
//        {
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//            var gaSection = configuration.GetSection("GoogleAnalytics");
//            _propertyId = gaSection.GetValue<string>("PropertyId") ?? throw new ArgumentException("PropertyId is required in configuration.");
//            _credentialsPath = gaSection.GetValue<string>("CredentialsPath") ?? throw new ArgumentException("CredentialsPath is required in configuration.");

//            _analyticsDataClient = CreateAnalyticsDataClientFromFile(_credentialsPath);
//        }

//        /// <summary>
//        /// Initializes a new instance that can also read credentials from DB per client.
//        /// </summary>
//        public GoogleAnalyticsService(DBContext ctx, IConfiguration configuration, ILogger<GoogleAnalyticsService> logger)
//        : this(configuration, logger)
//        {
//            _ctx = ctx;
//        }

//        /// <summary>
//        /// Authenticates and creates a BetaAnalyticsDataClient using service account credentials file.
//        /// </summary>
//        private BetaAnalyticsDataClient CreateAnalyticsDataClientFromFile(string credentialsPath)
//        {
//            try
//            {
//                using var stream = new FileStream(credentialsPath, FileMode.Open, FileAccess.Read);
//                var credential = GoogleCredential.FromStream(stream)
//                    .CreateScoped(BetaAnalyticsDataClient.DefaultScopes);

//                return new BetaAnalyticsDataClientBuilder
//                {
//                    Credential = credential
//                }.Build();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Failed to authenticate with Google Analytics Data API (file).");
//                throw;
//            }
//        }

//        /// <summary>
//        /// Builds a Google Analytics client and propertyId from DB using the provided clientId.
//        /// No JSON file or JSON serialization is created; it uses ServiceAccountCredential directly.
//        /// </summary>
//        //private async Task<(BetaAnalyticsDataClient Client, string PropertyId)> BuildClientFromDbAsync(Guid clientId, CancellationToken cancellationToken)
//        //{
//        //    if (_ctx is null)
//        //    {
//        //        throw new InvalidOperationException("DBContext not available. Use the constructor that injects DBContext to load credentials from DB.");
//        //    }

//        //    var cred = await _ctx.GoogleAnalyticsCredential
//        //        .AsNoTracking()
//        //        .FirstOrDefaultAsync(c => c.clientId == clientId && c.active, cancellationToken);
//        //    if (cred is null)
//        //    {
//        //        throw new InvalidOperationException("No active Google Analytics credentials found for the specified client.");
//        //    }

//        //    try
//        //    {
//        //        // Build ServiceAccountCredential directly from DB fields
//        //        var initializer = new ServiceAccountCredential.Initializer(cred.client_email)
//        //        {
//        //            // KeyId is optional; TokenServerUrl defaults to GoogleAuthConsts.TokenUrl
//        //            KeyId = cred.private_key_id,
//        //            Scopes = BetaAnalyticsDataClient.DefaultScopes
//        //        };

//        //        var saCredential = new ServiceAccountCredential(initializer.FromPrivateKey(cred.private_key));
//        //        var googleCredential = GoogleCredential.FromServiceAccountCredential(saCredential);

//        //        var client = new BetaAnalyticsDataClientBuilder
//        //        {
//        //            Credential = googleCredential
//        //        }.Build();

//        //        return (client, cred.propertyId);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Failed building GA client from DB credentials for client {ClientId}.", clientId);
//        //        throw;
//        //    }
//        //}

//        /// <summary>
//        /// Retrieves a basic report from Google Analytics Data API with pagination support.
//        /// </summary>
//        /// <param name="startDate">Start date (yyyy-MM-dd).</param>
//        /// <param name="endDate">End date (yyyy-MM-dd).</param>
//        /// <param name="dimensions">Array of dimension names.</param>
//        /// <param name="metrics">Array of metric names.</param>
//        /// <param name="pageSize">Number of rows per page (default:1000).</param>
//        /// <param name="cancellationToken">Cancellation token.</param>
//        /// <returns>List of all rows from the report.</returns>
//        public async Task<IList<Row>> GetReportAsync(
//            string startDate,
//            string endDate,
//            string[] dimensions,
//            string[] metrics,
//            int pageSize = 1000,
//            CancellationToken cancellationToken = default)
//        {
//            var allRows = new List<Row>();
//            var offset = 0;
//            var moreRows = true;

//            try
//            {
//                while (moreRows)
//                {
//                    var request = new RunReportRequest
//                    {
//                        Property = $"properties/{_propertyId}",
//                        DateRanges = { new DateRange { StartDate = startDate, EndDate = endDate } },
//                        Dimensions = { Array.ConvertAll(dimensions, d => new Dimension { Name = d }) },
//                        Metrics = { Array.ConvertAll(metrics, m => new Metric { Name = m }) },
//                        Limit = pageSize,
//                        Offset = offset
//                    };

//                    var response = await _analyticsDataClient.RunReportAsync(request, cancellationToken);
//                    if (response.Rows.Count == 0)
//                        break;

//                    allRows.AddRange(response.Rows);
//                    offset += response.Rows.Count;
//                    moreRows = response.Rows.Count == pageSize;
//                }
//            }
//            catch (Google.GoogleApiException gae)
//            {
//                _logger.LogError(gae, "Google API error while fetching report.");
//                throw;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Unexpected error while fetching report.");
//                throw;
//            }

//            return allRows;
//        }

//        /// <summary>
//        /// Retrieves an advanced report with filters, multiple dimensions/metrics, and pagination.
//        /// </summary>
//        /// <param name="startDate">Start date (yyyy-MM-dd).</param>
//        /// <param name="endDate">End date (yyyy-MM-dd).</param>
//        /// <param name="dimensions">Array of dimension names.</param>
//        /// <param name="metrics">Array of metric names.</param>
//        /// <param name="dimensionFilter">Optional dimension filter expression.</param>
//        /// <param name="metricFilter">Optional metric filter expression.</param>
//        /// <param name="orderBys">Optional list of OrderBy objects.</param>
//        /// <param name="pageSize">Number of rows per page (default:1000).</param>
//        /// <param name="cancellationToken">Cancellation token.</param>
//        /// <returns>List of all rows from the advanced report.</returns>
//        public async Task<IList<Row>> GetAdvancedReportAsync(
//            string startDate,
//            string endDate,
//            string[] dimensions,
//            string[] metrics,
//            FilterExpression? dimensionFilter = null,
//            FilterExpression? metricFilter = null,
//            IList<OrderBy>? orderBys = null,
//            int pageSize = 1000,
//            CancellationToken cancellationToken = default)
//        {
//            var allRows = new List<Row>();
//            var offset = 0;
//            var moreRows = true;

//            try
//            {
//                while (moreRows)
//                {
//                    var request = new RunReportRequest
//                    {
//                        Property = $"properties/{_propertyId}",
//                        DateRanges = { new DateRange { StartDate = startDate, EndDate = endDate } },
//                        Dimensions = { Array.ConvertAll(dimensions, d => new Dimension { Name = d }) },
//                        Metrics = { Array.ConvertAll(metrics, m => new Metric { Name = m }) },
//                        Limit = pageSize,
//                        Offset = offset
//                    };

//                    if (dimensionFilter is not null)
//                        request.DimensionFilter = dimensionFilter;
//                    if (metricFilter is not null)
//                        request.MetricFilter = metricFilter;
//                    if (orderBys is not null && orderBys.Count > 0)
//                        request.OrderBys.Add(orderBys);

//                    var response = await _analyticsDataClient.RunReportAsync(request, cancellationToken);
//                    if (response.Rows.Count == 0)
//                        break;

//                    allRows.AddRange(response.Rows);
//                    offset += response.Rows.Count;
//                    moreRows = response.Rows.Count == pageSize;
//                }
//            }
//            catch (Google.GoogleApiException gae)
//            {
//                _logger.LogError(gae, "Google API error while fetching advanced report.");
//                throw;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Unexpected error while fetching advanced report.");
//                throw;
//            }

//            return allRows;
//        }

//        // ===================== Helper builders & paginator =====================

//        private static Dimension BuildDimension(string name) => new() { Name = name };
//        private static Metric BuildMetric(string name) => new() { Name = name };

//        private static FilterExpression? BuildCountryEqualsFilter(string? country)
//        {
//            if (string.IsNullOrWhiteSpace(country)) return null;
//            return new FilterExpression
//            {
//                Filter = new Filter
//                {
//                    FieldName = "country",
//                    StringFilter = new Filter.Types.StringFilter
//                    {
//                        MatchType = Filter.Types.StringFilter.Types.MatchType.Exact,
//                        Value = country
//                    }
//                }
//            };
//        }

//        private static FilterExpression BuildEcommerceEventRegexFilter()
//        {
//            return new FilterExpression
//            {
//                Filter = new Filter
//                {
//                    FieldName = "eventName",
//                    StringFilter = new Filter.Types.StringFilter
//                    {
//                        MatchType = Filter.Types.StringFilter.Types.MatchType.FullRegexp,
//                        Value = "(session_start|add_to_cart|begin_checkout|view_item|purchase)"
//                    }
//                }
//            };
//        }

//        private static FilterExpression CombineAnd(params FilterExpression?[] expressions)
//        {
//            var list = new List<FilterExpression>();
//            foreach (var e in expressions)
//            {
//                if (e is not null) list.Add(e);
//            }

//            if (list.Count == 0)
//            {
//                // Return empty expression; caller should treat as no filter by ignoring null return
//                return new FilterExpression();
//            }

//            if (list.Count == 1)
//            {
//                return list[0];
//            }

//            var fel = new FilterExpressionList();
//            fel.Expressions.Add(list);
//            return new FilterExpression { AndGroup = fel };
//        }

//        private async Task<GaReportResult> RunReportAllPagesAsync(
//            IEnumerable<string> dimensionNames,
//            IEnumerable<string> metricNames,
//            string startDate,
//            string endDate,
//            FilterExpression? dimensionFilter,
//            int pageSize,
//            CancellationToken cancellationToken)
//        {
//            var rows = new List<Row>();
//            var offset = 0;
//            IReadOnlyList<DimensionHeader>? dimHeaders = null;
//            IReadOnlyList<MetricHeader>? metHeaders = null;

//            while (true)
//            {
//                var request = new RunReportRequest
//                {
//                    Property = $"properties/{_propertyId}",
//                    Limit = pageSize,
//                    Offset = offset,
//                };
//                request.DateRanges.Add(new DateRange { StartDate = startDate, EndDate = endDate });
//                foreach (var d in dimensionNames) request.Dimensions.Add(BuildDimension(d));
//                foreach (var m in metricNames) request.Metrics.Add(BuildMetric(m));
//                if (dimensionFilter is not null && !IsEmptyFilterExpression(dimensionFilter))
//                {
//                    request.DimensionFilter = dimensionFilter;
//                }

//                var response = await _analyticsDataClient.RunReportAsync(request, cancellationToken);
//                if (dimHeaders is null) dimHeaders = response.DimensionHeaders;
//                if (metHeaders is null) metHeaders = response.MetricHeaders;

//                if (response.Rows.Count == 0) break;
//                rows.AddRange(response.Rows);

//                offset += response.Rows.Count;
//                if (response.Rows.Count < pageSize) break;
//            }

//            return new GaReportResult(dimHeaders ?? Array.Empty<DimensionHeader>(), metHeaders ?? Array.Empty<MetricHeader>(), rows);
//        }

//        // Overload using dynamic propertyId and per-client analytics client
//        private static async Task<GaReportResult> RunReportAllPagesAsync(
//            BetaAnalyticsDataClient client,
//            string propertyId,
//            IEnumerable<string> dimensionNames,
//            IEnumerable<string> metricNames,
//            string startDate,
//            string endDate,
//            FilterExpression? dimensionFilter,
//            int pageSize,
//            CancellationToken cancellationToken)
//        {
//            var rows = new List<Row>();
//            var offset = 0;
//            IReadOnlyList<DimensionHeader>? dimHeaders = null;
//            IReadOnlyList<MetricHeader>? metHeaders = null;

//            while (true)
//            {
//                var request = new RunReportRequest
//                {
//                    Property = $"properties/{propertyId}",
//                    Limit = pageSize,
//                    Offset = offset,
//                };
//                request.DateRanges.Add(new DateRange { StartDate = startDate, EndDate = endDate });
//                foreach (var d in dimensionNames) request.Dimensions.Add(new Dimension { Name = d });
//                foreach (var m in metricNames) request.Metrics.Add(new Metric { Name = m });
//                if (dimensionFilter is not null && !(dimensionFilter.Filter is null && dimensionFilter.AndGroup is null && dimensionFilter.OrGroup is null && dimensionFilter.NotExpression is null))
//                {
//                    request.DimensionFilter = dimensionFilter;
//                }

//                var response = await client.RunReportAsync(request, cancellationToken);
//                if (dimHeaders is null) dimHeaders = response.DimensionHeaders;
//                if (metHeaders is null) metHeaders = response.MetricHeaders;

//                if (response.Rows.Count == 0) break;
//                rows.AddRange(response.Rows);

//                offset += response.Rows.Count;
//                if (response.Rows.Count < pageSize) break;
//            }

//            return new GaReportResult(dimHeaders ?? Array.Empty<DimensionHeader>(), metHeaders ?? Array.Empty<MetricHeader>(), rows);
//        }

//        private static bool IsEmptyFilterExpression(FilterExpression expr)
//        {
//            // Heuristic: when created as default new FilterExpression() with no Filter/And/Or, treat as empty
//            return expr.Filter is null && expr.AndGroup is null && expr.OrGroup is null && expr.NotExpression is null;
//        }

//        // ===================== Report methods mirroring Apps Script =====================

//        public Task<GaReportResult> GetByDeviceAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "deviceCategory" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        // Per-client variants that load credentials from DB
//        public async Task<GaReportResult> GetByDeviceForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (company, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "deviceCategory" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByChannelAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "sessionDefaultChannelGroup" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByChannelForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "sessionDefaultChannelGroup" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByUserTypeAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "newVsReturning" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByUserTypeForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "newVsReturning" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByCountryAsync(string startDate, string endDate, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "country" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, null, pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByCountryForClientAsync(Guid clientId, string startDate, string endDate, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "country" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, null, pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByDateAsync(string startDate, string endDate, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "date" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, null, pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByDateForClientAsync(Guid clientId, string startDate, string endDate, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "date" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, null, pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByDateDeviceAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "date", "deviceCategory" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByDateDeviceForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "date", "deviceCategory" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetFunnelByDeviceAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "engagedSessions",
//                "purchaseRevenue"
//            };
//            var dims = new[] { "eventName", "month", "year", "sessionDefaultChannelGroup", "deviceCategory" };
//            var combinedFilter = CombineAnd(BuildCountryEqualsFilter(country), BuildEcommerceEventRegexFilter());
//            var filterToUse = string.IsNullOrWhiteSpace(country) ? BuildEcommerceEventRegexFilter() : combinedFilter;
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, filterToUse, pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetFunnelByDeviceForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "engagedSessions", "purchaseRevenue" };
//            var dims = new[] { "eventName", "month", "year", "sessionDefaultChannelGroup", "deviceCategory" };
//            var combinedFilter = CombineAnd(BuildCountryEqualsFilter(country), BuildEcommerceEventRegexFilter());
//            var filterToUse = string.IsNullOrWhiteSpace(country) ? BuildEcommerceEventRegexFilter() : combinedFilter;
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, filterToUse, pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetFunnelByUserAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "engagedSessions",
//                "purchaseRevenue"
//            };
//            var dims = new[] { "eventName", "month", "year", "sessionDefaultChannelGroup", "newVsReturning" };
//            var combinedFilter = CombineAnd(BuildCountryEqualsFilter(country), BuildEcommerceEventRegexFilter());
//            var filterToUse = string.IsNullOrWhiteSpace(country) ? BuildEcommerceEventRegexFilter() : combinedFilter;
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, filterToUse, pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetFunnelByUserForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "engagedSessions", "purchaseRevenue" };
//            var dims = new[] { "eventName", "month", "year", "sessionDefaultChannelGroup", "newVsReturning" };
//            var combinedFilter = CombineAnd(BuildCountryEqualsFilter(country), BuildEcommerceEventRegexFilter());
//            var filterToUse = string.IsNullOrWhiteSpace(country) ? BuildEcommerceEventRegexFilter() : combinedFilter;
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, filterToUse, pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByLandingPagesAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "bounceRate",
//                "purchaseRevenue"
//            };
//            var dims = new[] { "landingPagePlusQueryString" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByLandingPagesForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "bounceRate", "purchaseRevenue" };
//            var dims = new[] { "landingPagePlusQueryString" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByEventsAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[] { "sessions" };
//            var dims = new[] { "eventName" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByEventsForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions" };
//            var dims = new[] { "eventName" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByWeekdaysAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "dayOfWeekName" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByWeekdaysForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "dayOfWeekName" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public Task<GaReportResult> GetByGenderAsync(string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var metrics = new[]
//            {
//                "sessions",
//                "transactions",
//                "purchaseRevenue",
//                "engagementRate",
//                "bounceRate",
//                "totalUsers",
//                "engagedSessions",
//                "averagePurchaseRevenue"
//            };
//            var dims = new[] { "userGender" };
//            return RunReportAllPagesAsync(dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        public async Task<GaReportResult> GetByGenderForClientAsync(Guid clientId, string startDate, string endDate, string? country = null, int pageSize = 1000, CancellationToken cancellationToken = default)
//        {
//            var (client, propertyId) = await BuildClientFromDbAsync(clientId, cancellationToken);
//            var metrics = new[] { "sessions", "transactions", "purchaseRevenue", "engagementRate", "bounceRate", "totalUsers", "engagedSessions", "averagePurchaseRevenue" };
//            var dims = new[] { "userGender" };
//            return await RunReportAllPagesAsync(client, propertyId, dims, metrics, startDate, endDate, BuildCountryEqualsFilter(country), pageSize, cancellationToken);
//        }

//        // ===================== Utility helpers similar to Apps Script =====================

//        public static string FormatGa4Date(string gaDate)
//        {
//            if (string.IsNullOrWhiteSpace(gaDate) || gaDate.Length != 8) return gaDate;
//            return $"{gaDate.Substring(0, 4)}-{gaDate.Substring(4, 2)}-{gaDate.Substring(6, 2)}";
//        }
//    }
//}
