using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;

namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    public class ApplicationValidationException : Exception
    {
        public IList<ValidationFailure> Errors { get; }

        public ApplicationValidationException(IEnumerable<ValidationFailure> failures)
            : base("One or more validation failures have occurred.")
        {
            Errors = failures?.ToList() ?? new List<ValidationFailure>();
        }
    }
}
