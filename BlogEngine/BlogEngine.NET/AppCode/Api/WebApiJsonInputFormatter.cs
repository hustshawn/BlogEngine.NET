using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Formatters;
using Newtonsoft.Json;

namespace BlogEngine.NET.AppCode.Api
{
    /// <summary>
    /// JSON body reader with ASP.NET Web API 2 semantics: a member that fails to deserialize
    /// (e.g. the admin SPA sends "Id": "" for new items) is recorded in ModelState and skipped,
    /// instead of discarding the whole model as NewtonsoftJsonInputFormatter does.
    /// </summary>
    public sealed class WebApiJsonInputFormatter : TextInputFormatter
    {
        readonly JsonSerializerSettings _settings;

        public WebApiJsonInputFormatter(JsonSerializerSettings settings)
        {
            _settings = settings;
            SupportedEncodings.Add(UTF8EncodingWithoutBOM);
            SupportedEncodings.Add(UTF16EncodingLittleEndian);
            SupportedMediaTypes.Add("application/json");
            SupportedMediaTypes.Add("text/json");
            SupportedMediaTypes.Add("application/*+json");
        }

        public override async Task<InputFormatterResult> ReadRequestBodyAsync(InputFormatterContext context, Encoding encoding)
        {
            // Kestrel disallows synchronous reads, so buffer the body before handing it to Json.NET
            string content;
            using (var reader = context.ReaderFactory(context.HttpContext.Request.Body, encoding))
                content = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(content))
                return await InputFormatterResult.NoValueAsync();

            var serializer = JsonSerializer.Create(_settings);
            serializer.Error += (sender, e) =>
            {
                context.ModelState.TryAddModelError(e.ErrorContext.Path ?? string.Empty, e.ErrorContext.Error.Message);
                e.ErrorContext.Handled = true;
            };

            using var jsonReader = new JsonTextReader(new StringReader(content));
            return await InputFormatterResult.SuccessAsync(serializer.Deserialize(jsonReader, context.ModelType));
        }
    }
}
