using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;

namespace BlogEngine.NET.AppCode.Api
{
    /// <summary>
    /// Reproduces the ASP.NET Web API 2 routing the admin SPA was written against:
    ///   api/{controller}/{id}           - action selected by HTTP verb, then by parameters
    ///   api/{controller}/{action}/{id}  - action selected by method name
    /// HTTP verbs come from [HttpX] attributes, else from the method name prefix, else POST.
    /// Simple parameters bind from route or query string, like Web API's [FromUri].
    /// </summary>
    /// <remarks>
    /// Runs after DefaultApplicationModelProvider builds the model and before
    /// ApiBehaviorApplicationModelProvider, so [ApiController] sees attribute-routed actions.
    /// Applies to controllers routed as "api/[controller]" whose actions have no route template of their own.
    /// </remarks>
    public sealed class WebApiRoutingProvider : IApplicationModelProvider
    {
        static readonly string[] VerbPrefixes = { "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS" };

        public int Order => -1000 + 50;

        public void OnProvidersExecuting(ApplicationModelProviderContext context)
        {
            foreach (var controller in context.Result.Controllers.Where(IsWebApiController))
            {
                foreach (var action in controller.Actions.Where(a => a.Selectors.All(s => s.AttributeRouteModel == null)))
                {
                    var verbs = GetHttpMethods(action);
                    action.Selectors.Clear();
                    action.Selectors.Add(CreateSelector("{id?}", verbs));
                    action.Selectors.Add(CreateSelector(action.ActionName + "/{id}", verbs));

                    foreach (var parameter in action.Parameters.Where(IsUriParameter))
                    {
                        // "action"/"controller" are always route values in ASP.NET Core; Web API 2 read them from the query string
                        parameter.BindingInfo ??= new BindingInfo();
                        parameter.BindingInfo.BindingSource = IsReservedRouteKey(parameter.ParameterName) ? BindingSource.Query : BindingSource.ModelBinding;
                    }
                }
            }
        }

        public void OnProvidersExecuted(ApplicationModelProviderContext context)
        {
        }

        static bool IsWebApiController(ControllerModel controller) =>
            controller.Selectors.Any(s => string.Equals(s.AttributeRouteModel?.Template, "api/[controller]", StringComparison.OrdinalIgnoreCase));

        static string[] GetHttpMethods(ActionModel action)
        {
            var declared = action.Attributes.OfType<IActionHttpMethodProvider>().SelectMany(a => a.HttpMethods).Distinct().ToArray();
            if (declared.Length > 0)
                return declared;

            var byName = VerbPrefixes.FirstOrDefault(v => action.ActionMethod.Name.StartsWith(v, StringComparison.OrdinalIgnoreCase));
            return new[] { byName ?? "POST" };
        }

        static SelectorModel CreateSelector(string template, string[] verbs)
        {
            var selector = new SelectorModel { AttributeRouteModel = new AttributeRouteModel { Template = template } };
            selector.ActionConstraints.Add(new HttpMethodActionConstraint(verbs));
            selector.ActionConstraints.Add(new WebApiParameterConstraint());
            selector.EndpointMetadata.Add(new HttpMethodMetadata(verbs));
            return selector;
        }

        internal static bool IsReservedRouteKey(string name) =>
            string.Equals(name, "action", StringComparison.OrdinalIgnoreCase) || string.Equals(name, "controller", StringComparison.OrdinalIgnoreCase);

        static bool IsUriParameter(ParameterModel parameter) =>
            parameter.BindingInfo?.BindingSource == null && WebApiParameterConstraint.IsSimpleType(parameter.ParameterType);
    }

    /// <summary>
    /// Web API 2 overload selection: a candidate needs all its required URI parameters present in
    /// route values or query string; among those, the ones matching the most parameters win.
    /// </summary>
    public sealed class WebApiParameterConstraint : IActionConstraint
    {
        public int Order => 0;

        public bool Accept(ActionConstraintContext context)
        {
            var available = GetAvailableNames(context.RouteContext);
            var score = Score(context.CurrentCandidate.Action, available);
            if (score < 0)
                return false;

            return context.Candidates
                .Where(c => c.Constraints?.Any(x => x is WebApiParameterConstraint) == true)
                .All(c => Score(c.Action, available) <= score);
        }

        internal static bool IsSimpleType(Type type) =>
            TypeDescriptor.GetConverter(Nullable.GetUnderlyingType(type) ?? type).CanConvertFrom(typeof(string));

        static HashSet<string> GetAvailableNames(RouteContext routeContext)
        {
            var names = new HashSet<string>(routeContext.HttpContext.Request.Query.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (var value in routeContext.RouteData.Values)
            {
                if (value.Value != null && !WebApiRoutingProvider.IsReservedRouteKey(value.Key))
                    names.Add(value.Key);
            }
            return names;
        }

        /// <returns>-1 if a required URI parameter is missing, otherwise the number of URI parameters supplied.</returns>
        static int Score(ActionDescriptor action, HashSet<string> available)
        {
            var matched = 0;
            foreach (var parameter in action.Parameters.OfType<ControllerParameterDescriptor>())
            {
                var source = parameter.BindingInfo?.BindingSource;
                if (source != BindingSource.ModelBinding && source != BindingSource.Query)
                    continue;

                if (available.Contains(parameter.Name))
                    matched++;
                else if (!parameter.ParameterInfo.HasDefaultValue && !parameter.ParameterInfo.IsOptional)
                    return -1;
            }
            return matched;
        }
    }
}
