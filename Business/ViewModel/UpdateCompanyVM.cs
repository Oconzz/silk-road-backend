using System;

namespace Business.ViewModel
{
    public class UpdateCompanyVM
    {
        public Guid id { get; set; }
        public string name { get; set; }
        public string? category { get; set; }
        public string? city { get; set; }
        public string? state { get; set; }
        public string? zipcode { get; set; }
        public string? country { get; set; }
        public string? logoUrl { get; set; }
    }
}
