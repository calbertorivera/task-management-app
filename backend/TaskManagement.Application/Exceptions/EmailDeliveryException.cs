namespace TaskManagement.Application.Exceptions;

public sealed class EmailDeliveryException : Exception
{
    public EmailDeliveryException()
        : base("We couldn't send the verification email. Please try again later.")
    {
    }

    public EmailDeliveryException(Exception innerException)
        : base(
            "We couldn't send the verification email. Please try again later.",
            innerException)
    {
    }
}