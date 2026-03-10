using APIServer.AuthAttributes;
using Business.Model;
using Microsoft.AspNetCore.Mvc.Filters;

namespace APIServer.Services
{
    public class ServiceProviderFilterFactory : IFilterFactory
    {
        private readonly Roles _role;
        private readonly int _limit;
        private readonly bool _requireCompanyContext;

        public ServiceProviderFilterFactory(Roles role, int limit, bool requireCompanyContext = true)
        {
            _role = role;
            _limit = limit;
            _requireCompanyContext = requireCompanyContext;
        }
        public bool IsReusable => false;

        public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
        {
            return new AuthorizationAttribute(serviceProvider, _role, _limit, _requireCompanyContext);
        }
    }
}
