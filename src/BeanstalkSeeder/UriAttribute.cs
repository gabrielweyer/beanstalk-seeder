using System;
using System.ComponentModel.DataAnnotations;

namespace BeanstalkSeeder
{
    class UriAttribute : ValidationAttribute
    {
        public UriAttribute()
            : base("The value for {0} must be a valid URI")
        {
        }

        protected override ValidationResult IsValid(object value, ValidationContext context)
        {
            return value is string uri && Uri.TryCreate(uri, UriKind.Absolute, out _)
                ? ValidationResult.Success
                : new ValidationResult(FormatErrorMessage(context.DisplayName));
        }
    }
}