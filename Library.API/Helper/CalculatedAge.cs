using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Library.API.Helper
{
    public static class CalculatedAge
    {
        public static int GetCurrentAge(this DateTimeOffset dateTimeOffset,DateTimeOffset? dateOfDeath)
        {
            var current = DateTimeOffset.UtcNow;
            if (dateOfDeath != null)
                current = dateOfDeath.Value.UtcDateTime;
            var age = current.Year - dateTimeOffset.Year;
            if (current < dateTimeOffset.AddYears(age))
                age--;
            return age;
        }
    }
}
