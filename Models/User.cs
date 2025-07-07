using System.ComponentModel.DataAnnotations;

namespace UserManagementAPI.Models
{
    // User model
    class User
    {
        public int UserId { get; set; }

        [Required, MinLength(2)]
        public string UserName { get; set; }

        [Range(0, 150)]
        public int UserAge { get; set; }

        [Required, EmailAddress]
        public string UserEmail { get; set; }

        public User(int userId, string userName, int userAge, string userEmail)
        {
            UserId = userId;
            UserName = userName;
            UserAge = userAge;
            UserEmail = userEmail;
        }
        // Helper: static method to validate user input
         public static bool IsValid(User user, out List<ValidationResult> results)
        {
            var context = new ValidationContext(user, serviceProvider: null, items: null);
            results = new List<ValidationResult>();
            return Validator.TryValidateObject(user, context, results, validateAllProperties: true);
        }
    }
}
