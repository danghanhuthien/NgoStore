using System.ComponentModel.DataAnnotations;

namespace Project_Essay_Course.ViewModels
{
    public class Account_ViewModel
    {
        // Register ViewModel
        public class RegisterViewModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; }
        }

        // Login ViewModel
        public class LoginViewModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [Display(Name = "Ghi nhớ đăng nhập?")]
            public bool RememberMe { get; set; }
        }

        // Profile ViewModel
        public class ProfileViewModel
        {
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Tên người dùng")]
            public string UserName { get; set; }

            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }
        }

        // Edit Profile ViewModel
        public class EditProfileViewModel
        {
            [Required(ErrorMessage = "Email là bắt buộc")]
            [EmailAddress(ErrorMessage = "Email không hợp lệ")]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Tên người dùng")]
            public string UserName { get; set; }

            [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }
        }

        // Change Password ViewModel
        public class ChangePasswordViewModel
        {
            [Required(ErrorMessage = "Mật khẩu hiện tại là bắt buộc")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu hiện tại")]
            public string CurrentPassword { get; set; }

            [Required(ErrorMessage = "Mật khẩu mới là bắt buộc")]
            [StringLength(100, ErrorMessage = "{0} phải có ít nhất {2} ký tự", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu mới")]
            public string NewPassword { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu mới")]
            [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không khớp")]
            public string ConfirmPassword { get; set; }
        }

        // Delete Account ViewModel
        public class DeleteAccountViewModel
        {
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập mật khẩu để xác nhận")]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }
        }

        // User Details ViewModel (Admin)
        public class UserDetailsViewModel
        {
            public string Id { get; set; }

            [Display(Name = "Email")]
            public string Email { get; set; }

            [Display(Name = "Tên người dùng")]
            public string UserName { get; set; }

            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Email đã xác nhận")]
            public bool EmailConfirmed { get; set; }

            [Display(Name = "Vai trò")]
            public List<string> Roles { get; set; }
        }
        public class ManageRolesViewModel
        {
            public string UserId { get; set; } = string.Empty;
            public string UserName { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;

            /// <summary>Các role user đang có</summary>
            public List<string> UserRoles { get; set; } = new();

            /// <summary>Tất cả role trong hệ thống</summary>
            public List<string> AllRoles { get; set; } = new();

            /// <summary>Role user CHƯA có (để hiện nút gán)</summary>
            public List<string> AvailableRoles =>
                AllRoles.Except(UserRoles).ToList();
        }
    }
}
