// For ease of discovery, resource types should be placed in
// the Aspire.Hosting.ApplicationModel namespace. If there is
// likelihood of a conflict on the resource name consider using
// an alternative namespace.
namespace Aspire.Hosting.ApplicationModel;

public class IdentityServerNovaResource(string name) 
    : ContainerResource(name)
{
    // Constants used to refer to well known-endpoint names, this is specific
    // for each resource type. MailDev exposes an SMTP endpoint and a HTTP
    // endpoint.
    internal const string HttpsEndpointName = "https";
    internal const string HttpEndpointName = "http";

    // An EndpointReference is a core .NET Aspire type used for keeping
    // track of endpoint details in expressions. Simple literal values cannot
    // be used because endpoints are not known until containers are launched.
    private EndpointReference? _httpsReference;

    public EndpointReference HttpsEndpoint =>
        _httpsReference ??= new(this, HttpsEndpointName);

    //public ReferenceExpression ConnectionStringExpression 
    //    => ReferenceExpression.Create(
    //        $"https://{HttpsEndpoint.Property(EndpointProperty.Host)}:{HttpsEndpoint.Property(EndpointProperty.Port)}"
    //    );
}
