using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Business.ViewModel
{
    public class LoginVM
    {
        public string email { get; set; }
        public string password { get; set; }
    }

    public class ChangePasswordVM
    {
        public string newPassword { get; set; } = string.Empty;
    }
}
