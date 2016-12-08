namespace Griffin.WebServer.Modules
{
    /// <summary>
    /// Used to route requests..
    /// </summary>
    /// <remarks>Invoked after <see cref="IAuthenticationModule"/> but before <see cref="IAuthorizationModule"/>.</remarks>
    public interface IRoutingModule : IHttpModule
    {
        /// <summary>
        /// Route the request.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns><see cref="ModuleResult.Stop"/> will stop all processing including <see cref="IHttpModule.EndRequest"/>.</returns>
        /// <remarks>Simply change the request URI to something else.</remarks>
        ModuleResult Route(IHttpContext context);
    }
}