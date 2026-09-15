namespace DigitalAllianceTogo.Application.Common.Exceptions
{
    /// <summary>
    /// Levée pour une validation échouée.
    /// À mapper vers un 400 dans le middleware d'erreurs de l'API.
    /// </summary>
    public class ValidationException : Exception
    {
        public IDictionary<string, string[]> Errors { get; }

        public ValidationException()
            : base("Une ou plusieurs erreurs de validation se sont produites.")
        {
            Errors = new Dictionary<string, string[]>();
        }

        public ValidationException(IEnumerable<FluentValidation.Results.ValidationFailure> failures) : this()
        {
            Errors = failures
                .GroupBy(f => f.PropertyName, f => f.ErrorMessage)
                .ToDictionary(g => g.Key, g => g.ToArray());
        }
    }
}
