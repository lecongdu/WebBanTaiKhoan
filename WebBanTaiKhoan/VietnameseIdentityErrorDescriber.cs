using Microsoft.AspNetCore.Identity;

namespace WebBanTaiKhoan // Đảm bảo namespace này giống với dự án của bạn
{
    public class VietnameseIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateUserName(string userName) => new IdentityError { Code = nameof(DuplicateUserName), Description = $"Tên đăng nhập '{userName}' đã được sử dụng." };
        public override IdentityError DuplicateEmail(string email) => new IdentityError { Code = nameof(DuplicateEmail), Description = $"Email '{email}' đã được đăng ký bởi một tài khoản khác." };
        public override IdentityError InvalidEmail(string email) => new IdentityError { Code = nameof(InvalidEmail), Description = $"Định dạng Email '{email}' không hợp lệ." };
        public override IdentityError PasswordTooShort(int length) => new IdentityError { Code = nameof(PasswordTooShort), Description = $"Mật khẩu phải có ít nhất {length} ký tự." };
        public override IdentityError PasswordRequiresNonAlphanumeric() => new IdentityError { Code = nameof(PasswordRequiresNonAlphanumeric), Description = "Mật khẩu phải có ít nhất một ký tự đặc biệt (!@#...)." };
        public override IdentityError PasswordRequiresDigit() => new IdentityError { Code = nameof(PasswordRequiresDigit), Description = "Mật khẩu phải có ít nhất một chữ số (0-9)." };
        public override IdentityError PasswordRequiresUpper() => new IdentityError { Code = nameof(PasswordRequiresUpper), Description = "Mật khẩu phải có ít nhất một chữ cái viết hoa (A-Z)." };
        public override IdentityError PasswordRequiresLower() => new IdentityError { Code = nameof(PasswordRequiresLower), Description = "Mật khẩu phải có ít nhất một chữ cái viết thường (a-z)." };
        public override IdentityError DefaultError() => new IdentityError { Code = nameof(DefaultError), Description = "Đã xảy ra lỗi không xác định." };
    }
}